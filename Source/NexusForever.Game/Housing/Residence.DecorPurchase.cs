using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private readonly Queue<DecorPurchase> decorPurchases = new();
        private DecorPurchase CurrentDecorPurchase => decorPurchases.TryPeek(out var value) ? value : null;
        internal bool DecorPurchaseSavePending { get { lock (MutationGate) return CurrentDecorPurchase?.Prepared == true; } }
        internal bool OwnsDecorPurchase(object value) => DecorPurchaseSavePending && ReferenceEquals(CurrentDecorPurchase, value);
        internal void RequireDecorPurchaseSave(object value)
        { if (DecorPurchaseSavePending && !OwnsDecorPurchase(value)) throw new InvalidOperationException("Residence belongs to its retained decor purchase."); }

        internal void QueueDecorPurchase(ResidenceMapInstance map, Player player, HousingDecorInfoEntry entry, DecorInfo update,
            CurrencyType? currency, ulong cost)
        {
            if (globalResidenceManager is not GlobalResidenceManager registry) throw new InvalidPacketValueException();
            var command = new DecorPurchase(this, map, player, entry, update, currency ?? CurrencyType.Credits, cost);
            lock (command.AuthorizationGate) lock (MutationGate)
            {
                RequirePersistenceEditable();
                if (persistence == null || !command.CurrentRoot || !command.CurrentPlayer || !CanModifyResidence(player))
                    throw new InvalidPacketValueException();
                decorPurchases.Enqueue(command); registry.QueueDecorPurchase(command);
            }
        }

        internal sealed class DecorPurchase : IHousingFeeOperation
        {
            private static readonly Logger log = LogManager.GetCurrentClassLogger();
            private readonly Residence owner;
            private readonly ResidenceMapInstance map;
            private readonly Player player;
            private readonly IGameSession session;
            private readonly IGuildManager membership;
            private readonly ICommunity community;
            private readonly HousingDecorInfoEntry entry;
            private readonly CurrencyType currency;
            private readonly ulong cost;
            private readonly ulong? originalOwner, originalGuild;
            private ResidencePersistence.Decoration decoration;
            private ResidencePersistence.Snapshot target;
            private Player.HousingNameActor actor;
            private CharacterContext enrolled;
            private Task attempt;
            private List<Action> publications;
            private int cursor;
            private bool reporting;
            private bool applied;
            private IReadOnlyList<Action> committedEffects = [];
            private int effectCursor;
            internal bool Prepared { get; private set; }
            internal bool Finished { get; private set; }
            internal ulong Id => owner.Id;
            internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal object AuthorizationGate => (community as GuildBase)?.MutationGate ?? owner.MutationGate;
            internal bool CurrentRoot => ReferenceEquals(owner.globalResidenceManager.GetResidence(owner.Id), owner)
                && owner.OwnerId == originalOwner && owner.GuildOwnerId == originalGuild && ReferenceEquals(owner.Map, map)
                && !map.UnloadStatus.HasValue && map.ContainsResidence(owner)
                && (community == null || community.Id == originalGuild && ReferenceEquals(community.Residence, owner)
                    && community is not GuildBase { PendingDelete: true });
            internal bool CurrentPlayer => ReferenceEquals(player.Session, session) && session is IPlayerSession original
                && ReferenceEquals(original.Player, player) && session is not NetworkSession { IsDisconnecting: true }
                && ReferenceEquals(player.Map, map) && ReferenceEquals(player.GuildManager, membership)
                && (community == null || ReferenceEquals(membership.GetGuild<ICommunity>(GuildType.Community), community));

            internal DecorPurchase(Residence owner, ResidenceMapInstance map, Player player, HousingDecorInfoEntry entry,
                DecorInfo update, CurrencyType currency, ulong cost)
            {
                this.owner = owner; this.map = map; this.player = player; this.entry = entry; this.currency = currency; this.cost = cost;
                session = player.Session; membership = player.GuildManager; originalOwner = owner.OwnerId; originalGuild = owner.GuildOwnerId;
                community = owner.Type == ResidenceType.Community ? membership.GetGuild<ICommunity>(GuildType.Community) : null;
                bool crate = update.DecorType == DecorType.Crate;
                decoration = new(0, entry.Id, (uint)update.DecorType, update.DecorData, update.HookBagIndex, update.HookIndex,
                    update.PlotIndex, crate ? 0 : update.Scale, crate ? 0 : update.Position.X, crate ? 0 : update.Position.Y,
                    crate ? 0 : update.Position.Z, crate ? 0 : update.Rotation.X, crate ? 0 : update.Rotation.Y,
                    crate ? 0 : update.Rotation.Z, crate ? 1 : update.Rotation.W, update.ActivePropUnitId, update.ParentDecorId, update.ColourShiftId);
            }
            internal void Advance()
            {
                lock (AuthorizationGate) lock (owner.MutationGate)
                {
                    if (Finished || !ReferenceEquals(owner.CurrentDecorPurchase, this)) return;
                    if (!Prepared)
                    {
                        if (!CurrentRoot || !CurrentPlayer || !owner.CanModifyResidence(player)) { Cancel(HousingResult.InvalidPermissions); return; }
                        if (owner.NeedsSave || owner.stagedSaveUnits.Count != 0 || owner.communitySaveOwner != null || owner.AssociationSavePending
                            || owner.donation != null || owner.paidName != null || owner.CurrentRemodel != null || !player.HousingNameCanBegin
                            || community is GuildBase { NeedsSave: true }) return;
                        if (cost != 0 && !player.CurrencyManager.CanAfford(currency, cost)) { Cancel(HousingResult.Decor_CannotAfford); return; }
                        HousingResult valid = map.ValidateDecorPurchase(owner, decoration);
                        if (valid != HousingResult.Success) { Cancel(valid); return; }
                        decoration = decoration with { Id = owner.GetGlobalResidenceManager().NextDecorId };
                        var captured = owner.CapturePersistence();
                        target = captured with { Decor = captured.Decor.Append(decoration).OrderBy(row => row.Id).ToArray() };
                        Prepared = true;
                        try
                        {
                            actor = player.BeginHousingName(this, currency, cost);
                            if (actor == null) { Prepared = false; return; }
                        }
                        catch
                        {
                            Prepared = false;
                            (player.QuestManager as QuestManager)?.RequireNormalSaveReload();
                            if (CurrentPlayer) session.ForceDisconnect();
                            Cancel(HousingResult.Failed); throw;
                        }
                    }
                    if (attempt != null)
                    {
                        if (!attempt.IsCompleted) return;
                        if (attempt.IsFaulted)
                        {
                            if (!reporting) { log.Error(attempt.Exception, "Housing decor fee remains owned after an unresolved save."); reporting = true; }
                            return;
                        }
                        attempt = null;
                    }
                    if (publications == null) actor.RefreshPublicationReadiness();
                    if (!actor.ReadyToPublish)
                    {
                        attempt = actor.Attempt(); ((GlobalResidenceManager)owner.globalResidenceManager).ObserveDecorPurchaseAttempt(this, attempt); return;
                    }
                    publications ??= PreparePublication();
                    while (cursor < publications.Count) { publications[cursor](); cursor++; }
                    actor.End(); Finish();
                }
            }
            CharacterSaveReconciliation IHousingFeeOperation.CreateReceipt(bool primary)
                => new HousingDecorPurchaseSaveReconciliation(Guid.NewGuid().ToString("N"), player.CharacterId, owner.Id,
                    originalOwner, originalGuild, (byte)GuildType.Community, decoration.Id,
                    context =>
                    {
                        if (!Prepared || !owner.OwnsDecorPurchase(this) || actor?.Owns() != true)
                            throw new InvalidOperationException("Housing decor lost its frozen actor or residence.");
                        CharacterDatabaseIdentity.Require(context, actor.Database);
                        if (!primary && !ResidencePersistence.Equal(owner.persistence.Read(context), target))
                            throw new InvalidOperationException("Housing decor lost its committed target graph.");
                    },
                    context =>
                    {
                        if (primary && !ReferenceEquals(context, enrolled)) throw new InvalidOperationException("Housing decor did not enroll its coupled graph.");
                    }, entry => ReferenceEquals(entry.Context, enrolled) && owner.persistence.OwnsRow(entry));
            void IHousingFeeOperation.Stage(CharacterContext context)
            {
                lock (owner.MutationGate)
                {
                    if (!Prepared || actor?.Owns() != true) throw new InvalidOperationException("Housing decor lost its primary save owner.");
                    owner.RegisterSaveUnit(context); owner.persistence.Save(context, this, target); enrolled = context;
                }
            }
            void IHousingFeeOperation.PrepareEffects()
            {
                if (cost == 0) return;
                if (player.AchievementManager is Achievement.CharacterAchievementManager achievements)
                    committedEffects = achievements.PrepareHousingDecorPurchase();
                else player.AchievementManager.CheckAchievements(player, AchievementType.HousingDecorPurchase, 0);
            }
            void IHousingFeeOperation.ApplyCommittedEffects()
            { while (effectCursor < committedEffects.Count) { committedEffects[effectCursor](); effectCursor++; } }
            private List<Action> PreparePublication()
            {
                if (!applied)
                {
                    if (ReferenceEquals(owner.globalResidenceManager.GetResidence(owner.Id), owner))
                        owner.decors.Add(decoration.Id, new Decor(owner, owner.persistence.Model(decoration), entry));
                    applied = true;
                }
                var prepared = new List<Action>();
                foreach (var packet in actor.BeginPublication())
                    prepared.Add(() => { if (actor.CanPublish && CurrentPlayer) session.EnqueueMessageEncrypted(packet()); });
                var recipients = CurrentRoot ? map.CaptureHousingRecipients().Select(recipient => (Player: recipient, Session: recipient.Session)).ToArray() : [];
                foreach (var recipient in recipients)
                    prepared.Add(() =>
                    {
                        if (CurrentRoot && map.ContainsHousingRecipient(recipient.Player) && ReferenceEquals(recipient.Player.Map, map)
                            && ReferenceEquals(recipient.Player.Session, recipient.Session) && recipient.Session is IPlayerSession current
                            && ReferenceEquals(current.Player, recipient.Player) && recipient.Session is not NetworkSession { IsDisconnecting: true })
                        {
                            var packet = new ServerHousingResidenceDecor { Operation = 0 };
                            packet.DecorData.Add(new Decor(owner, owner.persistence.Model(decoration), entry).Build());
                            recipient.Session.EnqueueMessageEncrypted(packet);
                        }
                    });
                return prepared;
            }
            private void Cancel(HousingResult result)
            {
                if (CurrentPlayer) session.EnqueueMessageEncrypted(new ServerHousingResult { ResidenceId = owner.Id, RealmId = owner.RealmId, Result = result });
                Finish();
            }
            private void Finish()
            {
                if (!ReferenceEquals(owner.CurrentDecorPurchase, this)) throw new InvalidOperationException("Housing decor cannot release another purchase.");
                owner.decorPurchases.Dequeue(); Finished = true; Completion.TrySetResult();
            }
        }
    }
}
