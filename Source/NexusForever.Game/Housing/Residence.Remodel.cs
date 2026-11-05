using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private readonly Queue<Remodel> remodels = new();
        private Remodel CurrentRemodel => remodels.TryPeek(out var current) ? current : null;
        internal bool RemodelSavePending { get { lock (MutationGate) return CurrentRemodel?.Prepared == true; } }
        internal bool OwnsRemodel(object reservation) => reservation != null && ReferenceEquals(CurrentRemodel, reservation);
        internal void RequireRemodelSave(object reservation)
        {
            if (CurrentRemodel?.Prepared == true && !OwnsRemodel(reservation))
                throw new InvalidOperationException("Residence belongs to its retained remodel.");
        }
        private void RequireRemodelFieldEditable(ResidenceSaveMask field)
        {
            if (remodels.Any(change => (change.Fields & field) != 0))
                throw new InvalidOperationException("Residence field belongs to an accepted remodel.");
        }
        private ushort ValidateDecorValue(ushort value)
        {
            if (gameTableManager.HousingDecorInfo?.GetEntry(value) == null) throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }
        private ushort ValidateWallpaperValue(ushort value, uint flag = 0)
        {
            var entry = gameTableManager.HousingWallpaperInfo?.GetEntry(value);
            if (entry == null || flag != 0 && (entry.Flags & flag) == 0) throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }
        internal void QueueRemodel(ResidenceMapInstance map, IPlayer player, ClientHousingRemodel request)
        {
            if (globalResidenceManager is not GlobalResidenceManager registry) throw new InvalidPacketValueException();
            var command = new Remodel(this, map, player, request);
            command.Accept(); registry.QueueRemodel(command);
        }

        internal sealed class Remodel
        {
            private readonly Residence owner;
            private readonly ResidenceMapInstance map;
            private readonly IPlayer player;
            private readonly IGameSession session;
            private readonly IGuildManager membership;
            private readonly ICommunity community;
            private readonly ulong? originalOwner, originalGuild;
            private readonly ushort? roof, wallpaper, entry, door, sky, music, ground;
            private ResidencePersistence.Snapshot target;
            private (IPlayer Player, IGameSession Session)[] recipients;
            private int cursor;
            private bool applied;
            private volatile bool ready;
            internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private object AuthorizationGate => (community as GuildBase)?.MutationGate ?? owner.MutationGate;
            internal ulong Id => owner.Id;
            internal bool Prepared { get; private set; }
            internal bool Finished { get; private set; }
            internal bool ReadyToPublish => ready;
            internal ResidenceSaveMask Fields { get; }

            internal Remodel(Residence owner, ResidenceMapInstance map, IPlayer player, ClientHousingRemodel request)
            {
                this.owner = owner; this.map = map; this.player = player; session = player.Session; membership = player.GuildManager;
                originalOwner = owner.OwnerId; originalGuild = owner.GuildOwnerId;
                community = owner.Type == ResidenceType.Community ? membership.GetGuild<ICommunity>(GuildType.Community) : null;
                ushort? Value(uint value) => value == 0 ? null : (ushort)value;
                roof = Value(request.RoofDecorInfoId); wallpaper = Value(request.WallpaperId); entry = Value(request.EntrywayDecorInfoId);
                door = Value(request.DoorDecorInfoId); sky = Value(request.SkyWallpaperId); music = Value(request.MusicId); ground = Value(request.GroundWallpaperId);
                Fields = (roof.HasValue ? ResidenceSaveMask.Roof : 0) | (wallpaper.HasValue ? ResidenceSaveMask.Wallpaper : 0)
                    | (entry.HasValue ? ResidenceSaveMask.Entryway : 0) | (door.HasValue ? ResidenceSaveMask.Door : 0)
                    | (sky.HasValue ? ResidenceSaveMask.Sky : 0) | (music.HasValue ? ResidenceSaveMask.Music : 0)
                    | (ground.HasValue ? ResidenceSaveMask.Ground : 0);
            }
            private bool CurrentRoot => ReferenceEquals(owner.globalResidenceManager.GetResidence(owner.Id), owner)
                && owner.OwnerId == originalOwner && owner.GuildOwnerId == originalGuild
                && ReferenceEquals(owner.Map, map) && !map.UnloadStatus.HasValue && map.ContainsResidence(owner)
                && (community == null || community is not GuildBase { PendingDelete: true } && ReferenceEquals(community.Residence, owner)
                    && community.Id == originalGuild && (community is not GuildBase original || ReferenceEquals(original.NameRegistry.GetGuild(original.Id), original)));
            private bool CurrentPlayer => ReferenceEquals(player.Session, session) && session is IPlayerSession connection && ReferenceEquals(connection.Player, player)
                && session is not NetworkSession { IsDisconnecting: true } && ReferenceEquals(player.Map, map)
                && ReferenceEquals(player.GuildManager, membership)
                && (community == null || ReferenceEquals(membership.GetGuild<ICommunity>(GuildType.Community), community));
            internal void Accept()
            {
                lock (AuthorizationGate) lock (owner.MutationGate)
                {
                    owner.RequirePersistenceEditable();
                    if (owner.persistence == null || !CurrentRoot || !CurrentPlayer || !owner.CanModifyResidence(player)) throw new InvalidPacketValueException();
                    if (roof.HasValue) owner.ValidateDecorValue(roof.Value);
                    if (wallpaper.HasValue) owner.ValidateWallpaperValue(wallpaper.Value);
                    if (entry.HasValue) owner.ValidateDecorValue(entry.Value);
                    if (door.HasValue) owner.ValidateDecorValue(door.Value);
                    if (sky.HasValue) owner.ValidateWallpaperValue(sky.Value, 0x40);
                    if (music.HasValue) owner.ValidateWallpaperValue(music.Value, 0x100);
                    if (ground.HasValue) owner.ValidateWallpaperValue(ground.Value, 0x200);
                    owner.remodels.Enqueue(this);
                }
            }
            internal bool Prepare()
            {
                lock (AuthorizationGate) lock (owner.MutationGate)
                {
                    if (Finished || !ReferenceEquals(owner.CurrentRemodel, this)) return false;
                    if (Prepared) return true;
                    if (!CurrentRoot || !CurrentPlayer || !owner.CanModifyResidence(player)) { Finish(); return false; }
                    if (owner.stagedSaveUnits.Count != 0 || owner.communitySaveOwner != null || owner.AssociationSavePending || owner.donation != null || owner.paidName != null || owner.DecorPurchaseSavePending) return false;
                    var captured = owner.CapturePersistence();
                    target = captured with { Header = captured.Header with { Roof = roof ?? captured.Header.Roof, Wallpaper = wallpaper ?? captured.Header.Wallpaper,
                        Entry = entry ?? captured.Header.Entry, Door = door ?? captured.Header.Door, Sky = sky ?? captured.Header.Sky,
                        Music = music ?? captured.Header.Music, Ground = ground ?? captured.Header.Ground } };
                    Prepared = true; return true;
                }
            }
            internal void Stage(CharacterContext context)
            {
                lock (owner.MutationGate)
                {
                    if (!Prepared || Finished || !ReferenceEquals(owner.CurrentRemodel, this)) throw new InvalidOperationException("Remodel lost its original save reservation.");
                    owner.RegisterSaveUnit(context); owner.persistence.Save(context, this, target);
                    context.OnSaveSettled(success => { if (success) ready = true; });
                }
            }
            internal void PublishOnWorldUpdate()
            {
                lock (AuthorizationGate) lock (owner.MutationGate)
                {
                    if (!ready || Finished || !ReferenceEquals(owner.CurrentRemodel, this)) throw new InvalidOperationException("Remodel has no confirmed owned save.");
                    if (!applied)
                    {
                        if (roof.HasValue) owner.roofDecorInfoId = roof.Value;
                        if (wallpaper.HasValue) owner.wallpaperId = wallpaper.Value;
                        if (entry.HasValue) owner.entrywayDecorInfoId = entry.Value;
                        if (door.HasValue) owner.doorDecorInfoId = door.Value;
                        if (sky.HasValue) owner.skyWallpaperId = sky.Value;
                        if (music.HasValue) owner.musicId = music.Value;
                        if (ground.HasValue) owner.groundWallpaperId = ground.Value;
                        if (owner.CaptureHeader() == target.Header) owner.saveMask = ResidenceSaveMask.None;
                        applied = true;
                    }
                    recipients ??= CurrentRoot ? map.CaptureHousingRecipients().Select(recipient => (recipient, recipient.Session)).ToArray() : [];
                    while (cursor < recipients.Length)
                    {
                        var recipient = recipients[cursor];
                        if (CurrentRoot && map.ContainsHousingRecipient(recipient.Player) && ReferenceEquals(recipient.Player.Map, map)
                            && ReferenceEquals(recipient.Player.Session, recipient.Session) && recipient.Session is IPlayerSession current
                            && ReferenceEquals(current.Player, recipient.Player) && recipient.Session is not NetworkSession { IsDisconnecting: true })
                            recipient.Session.EnqueueMessageEncrypted(map.BuildProperties());
                        cursor++;
                    }
                    Finish();
                }
            }
            private void Finish()
            {
                if (!ReferenceEquals(owner.CurrentRemodel, this)) throw new InvalidOperationException("Remodel cannot release another request.");
                owner.remodels.Dequeue(); Finished = true; Completion.TrySetResult();
            }
        }
    }
}
