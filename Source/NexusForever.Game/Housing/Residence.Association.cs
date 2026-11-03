using System.Data;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Guild;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private Association association;
        internal bool AssociationSavePending { get { lock (MutationGate) return association?.Prepared == true; } }
        internal bool OwnsAssociation(object token) => token != null && ReferenceEquals(association, token);
        internal void RequireAssociationSave(object token)
        {
            if (AssociationSavePending && !OwnsAssociation(token)) throw new InvalidOperationException("Residence belongs to its prepared community association.");
        }
        private void RequireAssociationAssignment()
        {
            if (AssociationSavePending && !association.Applying && communitySaveOwner == null)
                throw new InvalidOperationException("Residence association fields belong to their accepted save unit.");
        }
        internal void RequireAssociationPlotInfoAssignment()
        {
            if (AssociationSavePending && association.HoldsChild(this)) RequireAssociationAssignment();
        }
        public bool TryQueueCommunityPlacement(IPlayer player, ICommunity community, uint index, IResidenceEntrance entrance, IMapLockManager locks)
            => TryQueueAssociation(player, community, index, false, entrance, locks);
        public bool TryQueueCommunityRemoval(IPlayer player, ICommunity community, IResidenceEntrance entrance, IMapLockManager locks)
            => TryQueueAssociation(player, community, 0, true, entrance, locks);
        private bool TryQueueAssociation(IPlayer player, ICommunity community, uint index, bool remove, IResidenceEntrance entrance, IMapLockManager locks)
        {
            if (persistence == null) return false;
            if (community is not Community original || original.Residence is not Residence parent
                || parent.persistence == null || globalResidenceManager is not GlobalResidenceManager registry
                || !ReferenceEquals(globalResidenceManager, parent.globalResidenceManager)) throw new InvalidPacketValueException();
            var command = new Association(this, parent, original, player, index, remove, entrance, locks);
            command.Accept(); registry.QueueAssociation(command); return true;
        }

        internal sealed class Association
        {
            private readonly Residence child, parent, first, second;
            private readonly Community community;
            private readonly IPlayer player;
            private readonly IGameSession session;
            private readonly IGuildManager membership;
            private readonly IResidenceManager housing;
            private readonly IResidenceMapInstance initialPlayerMap;
            private readonly bool remove, isOwner;
            private readonly uint index;
            private readonly PropertyInfoId property;
            private readonly WorldEntry entry;
            private readonly Vector3 position, rotation;
            private readonly IMapLock destination;
            private readonly IMapPosition travelPosition;
            private ResidencePersistence.Snapshot targetChild, targetParent, beforeChild, beforeParent;
            private GuildMutationPersistence.Snapshot targetGuild, beforeGuild;
            private string name;
            private IResidence beforeLiveParent;
            private IResidenceMapInstance sourceMap, destinationMap;
            private bool witnessed, committed, worldStarted;
            private volatile bool ready;
            private int stage, cursor;
            private List<Action> publication;
            private HousingPlotInfoEntry[] geometry;
            internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal bool Prepared { get; private set; }
            internal bool Applying { get; private set; }
            internal bool Finished { get; private set; }
            internal bool ReadyToPublish => ready;
            internal ulong ChildId => child.Id;
            internal ulong ParentId => parent.Id;
            internal Residence AssociatedChild => child;
            internal bool HoldsChild(Residence value) => ReferenceEquals(value, child);
            internal bool Conflicts(Association other) => !remove && !other.remove && property == other.property && !ReferenceEquals(child, other.child);

            internal Association(Residence child, Residence parent, Community community, IPlayer player, uint index, bool remove, IResidenceEntrance entrance, IMapLockManager locks)
            {
                if (index > byte.MaxValue - 100) throw new InvalidPacketValueException();
                this.child = child; this.parent = parent; this.community = community; this.player = player; this.index = index; this.remove = remove;
                session = player.Session; membership = player.GuildManager; housing = player.ResidenceManager; initialPlayerMap = player.Map as IResidenceMapInstance;
                isOwner = child.OwnerId == player.CharacterId; property = remove ? PropertyInfoId.Residence : (PropertyInfoId)(index + 100);
                entry = entrance.Entry; position = entrance.Position; rotation = entrance.Rotation.ToEuler();
                destination = locks.GetResidenceLock(remove ? child : parent);
                travelPosition = new MapPosition { Info = new MapInfo { Entry = entry, MapLock = destination }, Position = position };
                first = child.Id < parent.Id ? child : parent; second = ReferenceEquals(first, child) ? parent : child;
            }
            private bool Registered => ReferenceEquals(child.globalResidenceManager.GetResidence(child.Id), child)
                && ReferenceEquals(parent.globalResidenceManager.GetResidence(parent.Id), parent)
                && ReferenceEquals(community.NameRegistry.GetGuild(community.Id), community) && ReferenceEquals(community.Residence, parent)
                && parent.GuildOwnerId == community.Id && parent.OwnerId == null && child.OwnerId.HasValue;
            private bool CurrentAssociation => Registered && !community.PendingDelete && child.communitySaveOwner == null
                && (remove || community.GetMember(child.OwnerId.Value) is { PendingDelete: false })
                && child.GuildOwnerId == targetChild.Header.Guild && child.PropertyInfoId == property
                && (remove ? child.Parent == null : ReferenceEquals(child.Parent, parent)
                    && ReferenceEquals(parent.GetChild(child.OwnerId.Value)?.Residence, child));
            private ResidenceMapInstance.AssociationDetachment detachment;
            private ResidenceMapInstance.AssociationAttachment attachment;
            private bool CurrentPlayer => ReferenceEquals(player.Session, session) && session is IPlayerSession original && ReferenceEquals(original.Player, player)
                && session is not NetworkSession { IsDisconnecting: true } && ReferenceEquals(player.GuildManager, membership)
                && ReferenceEquals(membership.GetGuild<ICommunity>(GuildType.Community), community)
                && ReferenceEquals(player.ResidenceManager, housing);
            private bool Authorized()
            {
                if (!Registered || community.PendingDelete || !CurrentPlayer || !ReferenceEquals(player.Map, initialPlayerMap) || initialPlayerMap == null) return false;
                if (remove)
                    return ReferenceEquals(child.Parent, parent) && ReferenceEquals(parent.GetChild(child.OwnerId.Value)?.Residence, child)
                        && (isOwner || community.GetMember(player.CharacterId)?.Rank.HasPermission(GuildRankPermission.RemoveCommunityPlotReservation) == true);
                return isOwner && ReferenceEquals(housing.Residence, child) && community.GetMember(player.CharacterId) is { PendingDelete: false }
                    && (child.Parent == null || ReferenceEquals(child.Parent, parent) && ReferenceEquals(parent.GetChild(player.CharacterId)?.Residence, child))
                    && (parent.GetChild(property) is not {} occupied || ReferenceEquals(occupied.Residence, child));
            }
            internal void Accept()
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    child.RequirePersistenceEditable(); parent.RequirePersistenceEditable();
                    if (!Authorized()) throw new InvalidPacketValueException();
                    community.QueueAssociation(this);
                }
            }
            private bool Prepare()
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    if (Finished || !community.OwnsAssociation(this)) return false;
                    if (Prepared) return true;
                    if (!Authorized()) { Finish(); return false; }
                    if (!community.AssociationPrerequisitesClear || child.association != null || parent.association != null
                        || child.stagedSaveUnits.Count != 0 || parent.stagedSaveUnits.Count != 0 || child.communitySaveOwner != null || parent.communitySaveOwner != null
                        || child.donation != null || parent.donation != null || child.paidName != null || parent.paidName != null
                        || child.CurrentRemodel != null || parent.CurrentRemodel != null) return false;
                    var captured = child.CapturePersistence();
                    var plots = captured.Plots.ToDictionary(row => row.Index);
                    geometry = child.gameTableManager.HousingPlotInfo.Entries.Where(row => row.HousingPropertyInfoId == (uint)property).ToArray();
                    foreach (var info in geometry)
                    {
                        byte slot = checked((byte)info.HousingPropertyPlotIndex);
                        if (!plots.TryGetValue(slot, out var row)) throw new InvalidOperationException("Association lost an original housing plot.");
                        plots[slot] = row with { Info = checked((ushort)info.Id) };
                    }
                    targetChild = captured with { Header = captured.Header with { Guild = remove ? null : community.Id, Property = (byte)property }, Plots = plots.Values.OrderBy(row => row.Index).ToArray() };
                    targetParent = parent.CapturePersistence(); targetGuild = community.CaptureAssociationGraph(this);
                    if (remove) targetGuild = targetGuild with { Members = targetGuild.Members.Select(row => row.Character == child.OwnerId ? row with { Plot = -1 } : row).ToArray() };
                    name = community.Name; beforeLiveParent = child.Parent; sourceMap = child.Map;
                    child.association = parent.association = this; Prepared = true; return true;
                }
            }
            internal async Task Run(Func<CharacterContext> contexts)
            {
                if (!Prepare()) return;
                if (!committed)
                {
                    await using var context = contexts(); bool entered = false;
                    lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                    {
                        community.RequireAssociationDatabase(context, this);
                        child.persistence.RequireOwner(context, this); parent.persistence.RequireOwner(context, this);
                    }
                    Task saving = context.Database.CreateExecutionStrategy().Execute(() =>
                    {
                        if (entered) throw new InvalidOperationException("Association enrollment cannot be replayed by a provider strategy.");
                        entered = true; var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
                        try
                        {
                            bool mysql = context.Database.IsMySql();
                            if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported association provider.");
                            _ = (mysql ? context.Guild.FromSqlInterpolated($"SELECT * FROM guild WHERE id={community.Id} FOR UPDATE")
                                : context.Guild.Where(row => row.Id == community.Id)).AsNoTracking().AsEnumerable().Single();
                            foreach (ulong id in new[] { first.Id, second.Id })
                                _ = (mysql ? context.Residence.FromSqlInterpolated($"SELECT * FROM residence WHERE id={id} FOR UPDATE")
                                    : context.Residence.Where(row => row.Id == id)).AsNoTracking().AsEnumerable().Single();
                            lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                            {
                                var sqlChild = child.persistence.Read(context); var sqlParent = parent.persistence.Read(context);
                                var sqlGuild = GuildMutationPersistence.Read(context, community.Id, (byte)GuildType.Community);
                                if (context.Guild.AsNoTracking().Single(row => row.Id == community.Id).Name != name)
                                    throw new InvalidOperationException("Association lost its original confirmed community name.");
                                if (!witnessed) { beforeChild = sqlChild; beforeParent = sqlParent; beforeGuild = sqlGuild; witnessed = true; }
                                bool before = ResidencePersistence.Equal(sqlChild, beforeChild) && ResidencePersistence.Equal(sqlParent, beforeParent) && GuildMutationPersistence.Equal(sqlGuild, beforeGuild);
                                bool after = ResidencePersistence.Equal(sqlChild, targetChild) && ResidencePersistence.Equal(sqlParent, targetParent) && GuildMutationPersistence.Equal(sqlGuild, targetGuild);
                                if (!before && !after) throw new InvalidOperationException("Association differs from its complete retained guild/residence unit.");
                                child.RegisterSaveUnit(context); parent.RegisterSaveUnit(context);
                                community.SaveAssociationGraph(context, this, targetGuild);
                                child.persistence.Save(context, this, targetChild); parent.persistence.Save(context, this, targetParent);
                                context.OnSaveSettled(success => { if (success) committed = true; });
                            }
                            return Complete();
                        }
                        catch { transaction.Dispose(); throw; }
                        async Task Complete()
                        {
                            await using (transaction) { await context.SaveChangesAsync(); await transaction.CommitAsync(); }
                        }
                    });
                    try { await saving; } catch when (committed) { }
                }
                ready = true;
            }
            internal void PublishOnWorldUpdate()
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    if (!ready || !community.OwnsAssociation(this) || !child.OwnsAssociation(this) || !parent.OwnsAssociation(this))
                        throw new InvalidOperationException("Association has no confirmed original save unit.");
                    if (!worldStarted)
                    {
                        if (!Registered || community.PendingDelete || child.communitySaveOwner != null || !ReferenceEquals(child.Parent, beforeLiveParent)) { Finish(); return; }
                        worldStarted = true;
                    }
                    if (!Registered || community.PendingDelete || child.communitySaveOwner != null) { Finish(); return; }
                    Applying = true;
                    try
                    {
                        if (stage == 0)
                        {
                            if (sourceMap != null && ReferenceEquals(child.Map, sourceMap))
                            {
                                if (sourceMap is ResidenceMapInstance old)
                                {
                                    detachment ??= new(old, child);
                                    detachment.Apply();
                                }
                                if (beforeLiveParent == null)
                                {
                                    if (!sourceMap.UnloadStatus.HasValue) sourceMap.Unload(new MapPosition { Info = new MapInfo { Entry = entry, MapLock = destination }, Position = position });
                                }
                            }
                            stage++;
                        }
                        if (stage == 1)
                        {
                            if (ReferenceEquals(child.Parent, parent)) parent.RemoveChild(child);
                            child.guildOwnerId = targetChild.Header.Guild; child.propertyInfoId = property;
                            foreach (var info in geometry) child.GetPlot(checked((byte)info.HousingPropertyPlotIndex)).PlotInfoEntry = info;
                            if (!remove) parent.AddChild(child, true);
                            if (remove && community.GetMember(child.OwnerId.Value) is GuildMember member) member.AdoptAssociationReservation(-1);
                            if (child.CaptureHeader() == targetChild.Header) child.saveMask = ResidenceSaveMask.None;
                            destinationMap = remove ? null : parent.Map;
                            stage++;
                        }
                        if (stage == 2)
                        {
                            if (CurrentAssociation && destinationMap is ResidenceMapInstance map && !map.UnloadStatus.HasValue)
                            {
                                attachment ??= new(map, child);
                                attachment.Apply();
                            }
                            stage++;
                        }
                        if (stage == 3)
                        {
                            bool currentMap = ReferenceEquals(player.Map, initialPlayerMap) || player is Entity.Player retry && retry.OwnsHousingTeleport(this);
                            if (CurrentPlayer && currentMap && CurrentAssociation && (remove ? isOwner : beforeLiveParent != null || !ReferenceEquals(sourceMap, initialPlayerMap)))
                            {
                                player.Rotation = rotation;
                                if (player is Entity.Player original) { original.TeleportHousing(this, travelPosition); original.FinishHousingTeleport(this); }
                                else player.TeleportTo(entry, position, destination);
                            }
                            stage++;
                        }
                        publication ??= CapturePublication();
                        while (cursor < publication.Count) { publication[cursor](); cursor++; }
                        Finish();
                    }
                    finally { Applying = false; }
                }
            }
            private List<Action> CapturePublication()
            {
                var actions = new List<Action>();
                void MapPackets(IResidenceMapInstance candidate, bool deleted)
                {
                    if (candidate is not ResidenceMapInstance map) return;
                    var factories = map.AssociationPackets(child, deleted);
                    foreach (IPlayer recipient in map.CaptureHousingRecipients())
                    {
                        var connection = recipient.Session;
                        foreach (var factory in factories) actions.Add(() =>
                        {
                            if (!CurrentAssociation || map.UnloadStatus.HasValue || !map.ContainsHousingRecipient(recipient) || !ReferenceEquals(recipient.Map, map)
                                || !ReferenceEquals(recipient.Session, connection) || connection is not IPlayerSession current || !ReferenceEquals(current.Player, recipient)
                                || connection is NetworkSession { IsDisconnecting: true }) return;
                            connection.EnqueueMessageEncrypted(factory());
                        });
                    }
                }
                if (beforeLiveParent != null) MapPackets(sourceMap, true);
                if (!remove) MapPackets(destinationMap, false);
                if (!remove) actions.Add(() =>
                {
                    if (!CurrentPlayer || !CurrentAssociation) return;
                    var message = new ServerHousingCommunityPlacement { PlacedResidenceId = child.Id, PropertyIndex = index };
                    message.TargetResidence.RealmId = parent.RealmId; message.TargetResidence.ResidenceId = parent.Id; session.EnqueueMessageEncrypted(message);
                });
                return actions;
            }
            private void Finish()
            {
                if (Prepared)
                {
                    if (!child.OwnsAssociation(this) || !parent.OwnsAssociation(this)) throw new InvalidOperationException("Association cannot release another residence owner.");
                    child.association = parent.association = null;
                }
                community.FinishAssociation(this); Finished = true; Completion.TrySetResult();
            }
        }
    }
}
