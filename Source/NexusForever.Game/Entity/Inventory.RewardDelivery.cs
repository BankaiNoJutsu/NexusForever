using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public bool TryReserveSoulboundCreation(InventoryLocation location, IItemInfo info, uint count,
            out IInventoryRewardReservation reservation, out GenericError error,
            ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            lock (MutationGate)
            {
                reservation = null;
                error = GenericError.ItemNotValidForSlot;
                if (rewardOwnerInvalidated || RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || count == 0 || characterId == 0
                    || (info?.Entry != null && charges > info.Entry.MaxCharges)
                    || inventorySaveLeases != 0 || inventoryMutationLeases != 0 || inventoryMutationDepth != 0
                    || rewardNotificationDepth != 0 || HasPendingRewardDelivery
                    || bags.Values.SelectMany(bag => bag).OfType<Item>().Any(item => item.HasPendingSave)
                    || deletedItems.OfType<Item>().Any(item => item.HasPendingSave))
                    return false;
                if (!TryPlanSoulboundCreation(location, info, count, charges, out var plan, out error)) return false;
                if (GetBag(location) is not Bag bag || plan.StackUpdates.Any(line => line.Item is not Item))
                {
                    error = GenericError.ItemNotValidForSlot;
                    return false;
                }
                var expected = plan.StackUpdates.ToDictionary(line => line.Before.BagIndex, line => line.Item);
                foreach (var line in plan.NewStacks) expected.Add(line.BagIndex, null);
                if (!bag.TryReserveRewardPlan(expected, out var slots, out var items))
                {
                    error = GenericError.ItemNotValidForSlot;
                    return false;
                }
                try
                {
                    var lines = new List<RewardDeliveryLine>();
                    foreach (var line in plan.StackUpdates)
                    {
                        var item = (Item)line.Item;
                        var token = items[item];
                        if (!RewardCriticalEqual(line.Before, token.PreparedValues))
                            throw new InvalidOperationException("Reward merge changed during preparation.");
                        lines.Add(new RewardDeliveryLine(item, token, line.StackCount - line.Before.StackCount, null));
                    }
                    foreach (var line in plan.NewStacks)
                    {
                        var template = new Item(characterId, info, line.StackCount, charges, gameTableManager: gameTableManager);
                        template.MakeSoulbound();
                        ItemModel frozen = template.CaptureRewardModel();
                        frozen.Location = (ushort)location;
                        frozen.BagIndex = line.BagIndex;
                        lines.Add(new RewardDeliveryLine(null, null, line.StackCount, frozen));
                    }
                    var held = new RewardDeliveryReservation(this, bag, slots, info, count, reason, lines);
                    if (!held.IsCurrentActor)
                    {
                        held.Cancel();
                        error = GenericError.ItemNotValidForSlot;
                        return false;
                    }
                    RewardDeliveries.Add(held);
                    reservation = held;
                    error = GenericError.Ok;
                    return true;
                }
                catch
                {
                    foreach (var token in items.Values) token.Dispose();
                    slots.Dispose();
                    error = GenericError.ItemNotValidForSlot;
                    return false;
                }
            }
        }

        private static bool RewardCriticalEqual(ItemModel first, ItemModel second) => first != null && second != null
            && first.Id == second.Id && first.OwnerId == second.OwnerId && first.ItemId == second.ItemId
            && first.Location == second.Location && first.BagIndex == second.BagIndex && first.StackCount == second.StackCount
            && first.Charges == second.Charges && first.ExpirationTimeLeft == second.ExpirationTimeLeft && first.Soulbound == second.Soulbound;

        private sealed class RewardDeliveryLine(Item existing, Item.RewardMutationReservation token, uint award, ItemModel template)
        {
            public readonly Item Existing = existing;
            public Item.RewardMutationReservation Token = token;
            public readonly uint Award = award;
            public readonly ItemModel Template = template;
            public ItemModel Before => Token?.Before;
            public ItemModel After => Token?.After;
            public ulong Guid => Existing?.Guid ?? Template.Id;
        }

        private sealed partial class RewardDeliveryReservation : IInventoryRewardReservation
        {
            private readonly Inventory owner;
            private readonly Bag bag;
            private readonly Bag.SlotReservation slots;
            private readonly IItemInfo info;
            private readonly uint itemId;
            private readonly uint count;
            private readonly uint maximum;
            private readonly uint charges;
            private readonly uint expiration;
            private readonly uint runeInstance;
            private readonly bool stackable;
            private readonly ItemUpdateReason reason;
            private readonly List<RewardDeliveryLine> lines;
            private readonly IGameSession session;
            private readonly IAccount account;
            private readonly uint? accountId;
            private InventoryRewardReservationState state = InventoryRewardReservationState.Prepared;
            private InventoryRewardDeliveryPlan plan;
            private CharacterContext context;
            private string database;
            private RewardClaimEnrollment enrollment;
            private ClaimIdentity identity;
            private string payload;
            private volatile bool enrolled;
            private bool flushed;
            private bool terminal;
            private volatile bool abandonRequested;
            private int quarantineNotification;
            private EventHandler<SavedChangesEventArgs> onFlushed;

            internal RewardDeliveryReservation(Inventory owner, Bag bag, Bag.SlotReservation slots, IItemInfo info,
                uint count, ItemUpdateReason reason, List<RewardDeliveryLine> lines)
            {
                this.owner = owner; this.bag = bag; this.slots = slots; this.info = info;
                this.count = count; this.reason = reason; this.lines = lines;
                itemId = info.Id; maximum = info.Entry.MaxStackCount; charges = info.Entry.MaxCharges;
                expiration = info.Entry.ExpirationTimeMinutes; runeInstance = info.Entry.ItemRuneInstanceId;
                stackable = info.IsStackable();
                session = owner.player?.Session; account = owner.player?.Account;
                accountId = account?.Id;
            }

            public InventoryRewardReservationState State { get { lock (owner.MutationGate) return state; } }
            public InventoryRewardDeliveryPlan Plan { get { lock (owner.MutationGate) return plan; } }
            public ulong CharacterId => owner.characterId;
            public uint ItemId => itemId;
            public uint Count => count;
            internal bool IsCurrentActor => CurrentActor();

            private bool CurrentActor() => !abandonRequested && !owner.rewardOwnerInvalidated && (owner.player == null
                || (session != null && account != null && ReferenceEquals(owner.player.Inventory, owner)
                    && owner.player.CharacterId == owner.characterId && ReferenceEquals(owner.player.Session, session)
                    && ReferenceEquals(owner.player.Account, account) && ReferenceEquals(account.Session, session)
                    && session is not NetworkSession { IsDisconnecting: true }
                    && owner.player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)));

            public InventoryRewardDeliveryPlan Stage(CharacterContext next, CharacterChallengeRewardClaimModel claim)
                => StageCore(next, claim == null ? null : new ChallengeClaimEnrollment(claim));

            public InventoryRewardDeliveryPlan StageContract(CharacterContext next, CharacterContractRewardClaimModel claim)
                => StageCore(next, claim == null ? null : new ContractClaimEnrollment(claim));

            private InventoryRewardDeliveryPlan StageCore(CharacterContext next, RewardClaimEnrollment proposed)
            {
                ArgumentNullException.ThrowIfNull(next);
                lock (owner.MutationGate)
                {
                    try
                    {
                        ArgumentNullException.ThrowIfNull(proposed);
                        if (state is not (InventoryRewardReservationState.Prepared or InventoryRewardReservationState.ReadyToRetry)
                            || !slots.IsCurrent || owner.RequiresAuthoritativeReload || !CurrentActor())
                            throw new InvalidOperationException("Reward delivery is not ready for enrollment.");
                        if (next.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable
                            || next.IsSaveRejected || next.HasSavepointRollback)
                            throw new InvalidOperationException("A healthy serializable unit is required.");
                        string key = Item.GetRewardDatabaseKey(next);
                        if (database != null && database != key) throw new InvalidOperationException("Delivery changed its configured database.");
                        var candidate = proposed.Identity;
                        if (candidate.Contract && owner.player != null && candidate.Account != accountId)
                            throw new InvalidOperationException("Contract receipt belongs to another prepared account.");
                        if (identity != null && candidate with { Created = identity.Created } != identity)
                            throw new InvalidOperationException("Retained claim identity or reward owner changed.");
                        // MySQL datetime(6) cannot retain sub-microsecond ticks.
                        // Bind the same representable timestamp on both providers.
                        identity ??= candidate with { Created = new DateTime(candidate.Created.Ticks - candidate.Created.Ticks % 10, candidate.Created.Kind) };
                        proposed.SetCreated(identity.Created);
                        database = key; context = next; enrollment = proposed; enrolled = false; flushed = terminal = false;
                        state = InventoryRewardReservationState.Staged;
                        next.OnSaveSettled(committed => OnTerminal(next, committed));
                        next.OnSaveValidation(ValidateUnit);
                        onFlushed = (_, result) => { if (result.EntitiesSavedCount > 0) flushed = true; };
                        next.SavedChanges += onFlushed;
                        enrollment.Bind(next, owner.characterId);
                        if (info.Id != itemId || info.IsStackable() != stackable || info.Entry.MaxStackCount != maximum || info.Entry.MaxCharges != charges
                            || info.Entry.ExpirationTimeMinutes != expiration || info.Entry.ItemRuneInstanceId != runeInstance)
                            throw new InvalidOperationException("Reward definition changed after preparation.");

                        foreach (var line in lines)
                        {
                            if (line.Token == null)
                            {
                                if (line.Template.Id == 0)
                                    line.Template.Id = owner.itemManager?.NextItemId ?? throw new InvalidOperationException("Reward delivery requires the real item allocator.");
                                line.Token = Item.PrepareNewRewardItem(line.Template, info, owner.gameTableManager);
                            }
                            line.Token.Stage(next, line.Existing == null ? 0 : line.Award, ValidateUnit);
                        }
                        plan = new(owner.characterId, itemId, count, lines.Select(line => new InventoryRewardDeliveryLine(
                            line.Existing != null, line.Award, InventoryRewardItemSnapshot.FromModel(line.Before), InventoryRewardItemSnapshot.FromModel(line.After))));
                        payload = plan.ToJson();
                        enrollment.SetDelivery(payload);
                        var stagedPlan = plan;
                        next.OnSaveValidation(phase =>
                        {
                            if (phase != CharacterSaveValidationPhase.BeforeExplicitCommit) return;
                            // This context callback is separate from Item guards:
                            // query SQL without holding their persistence gates.
                            foreach (var line in stagedPlan.Lines)
                            {
                                var actual = next.Item.AsNoTracking().SingleOrDefault(row => row.Id == line.After.Guid);
                                if (InventoryRewardItemSnapshot.FromModel(actual) != line.After
                                    || next.RealmBankItem.AsNoTracking().Any(row => row.Id == line.After.Guid))
                                    throw new InvalidOperationException("Reward Item SQL differs from its frozen delivery target.");
                            }
                        });
                        enrolled = true;
                        return plan;
                    }
                    catch (Exception error)
                    {
                        next.RejectSave(error);
                        throw;
                    }
                }
            }

            // Called under individual Item gates: no inventory gate, packets or SQL.
            private void ValidateUnit(CharacterSaveValidationPhase phase)
            {
                if (!enrolled || context.IsSaveRejected || context.HasSavepointRollback || enrollment.Identity != identity)
                    throw new InvalidOperationException("Reward receipt enrollment changed or is incomplete.");
                enrollment.Validate(context, flushed, payload);
            }

            private void OnTerminal(CharacterContext unit, bool committed)
            {
                lock (owner.MutationGate)
                {
                    if (!ReferenceEquals(context, unit)) return;
                    unit.SavedChanges -= onFlushed;
                    terminal = true;
                    state = committed && enrolled && flushed && lines.All(line => line.Token?.State == RewardItemReservationState.Committed)
                        ? InventoryRewardReservationState.Committed : InventoryRewardReservationState.Uncertain;
                    if (abandonRequested)
                    {
                        if (state == InventoryRewardReservationState.Committed) { if (!PublishCommitted()) Quarantine(); }
                        else Quarantine();
                    }
                }
            }

            public InventoryRewardReservationState ResolveOutcome(CharacterContext fresh)
            {
                ArgumentNullException.ThrowIfNull(fresh);
                try
                {
                    lock (owner.MutationGate)
                    {
                        if (state != InventoryRewardReservationState.Uncertain || !terminal) return state;
                        if (ReferenceEquals(fresh, context) || fresh.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable
                            || fresh.IsSaveRejected || fresh.ChangeTracker.Entries().Any() || Item.GetRewardDatabaseKey(fresh) != database)
                            throw new InvalidOperationException("Delivery resolution requires a fresh serializable transaction in the same database.");
                        var (committed, absent) = enrollment.ReadOutcome(fresh, identity, payload);
                        if (!committed && !absent) { Quarantine(); return state; }
    
                        // Validate every row before changing any leaf's recovery state.
                        foreach (var line in lines)
                        {
                            if (line.Guid == 0) { if (committed) { Quarantine(); return state; } continue; }
                            var actual = fresh.Item.AsNoTracking().SingleOrDefault(row => row.Id == line.Guid);
                            var expected = committed ? line.After : line.Before;
                            if (fresh.RealmBankItem.AsNoTracking().Any(row => row.Id == line.Guid)
                                || InventoryRewardItemSnapshot.FromModel(actual) != InventoryRewardItemSnapshot.FromModel(expected)
                                || (committed && expected == null))
                            { Quarantine(); return state; }
                        }
                        foreach (var line in lines)
                        {
                            if (line.Token?.State == RewardItemReservationState.Uncertain)
                                line.Token.ResolveOutcome(fresh, _ => committed);
                            if (committed && line.Token?.State != RewardItemReservationState.Committed)
                            { Quarantine(); return state; }
                            if (absent && line.Token != null && line.Token.State is not (RewardItemReservationState.Prepared or RewardItemReservationState.ReadyToRetry))
                            { Quarantine(); return state; }
                        }
                        state = committed ? InventoryRewardReservationState.Committed : InventoryRewardReservationState.ReadyToRetry;
                        return state;
                    }
                }
                finally
                {
                    if (state == InventoryRewardReservationState.Quarantined && CurrentActor()
                        && Interlocked.Exchange(ref quarantineNotification, 1) == 0)
                        try { session?.ForceDisconnect(); } catch { }
                }
            }

            public bool PublishCommitted()
            {
                var messages = new List<IWritable>();
                bool refresh = false;
                bool notifying = false;
                long generation = 0;
                try
                {
                    lock (owner.MutationGate)
                    {
                        if (state != InventoryRewardReservationState.Committed || !terminal || !slots.IsCurrent
                            || lines.Any(line => line.Token?.CanPublishCommitted != true)) return false;
                        bool notify = CurrentActor() && owner.player?.IsLoading == false;
                        // Build every potentially throwing payload before changing any live quantity.
                        if (notify)
                            foreach (var line in lines)
                                messages.Add(line.Existing == null
                                    ? new ServerItemAdd { InventoryItem = new InventoryItem { Item = line.Token.BuildPublicationPayload(), Reason = reason } }
                                    : new ServerItemStackCountUpdate { Guid = line.Guid, StackCount = line.After.StackCount, Reason = reason });
                        foreach (var line in lines)
                        {
                            Item item = line.Token.PublishCommitted(true) ?? throw new InvalidOperationException("Preflighted Item publication failed.");
                            if (line.Existing == null) slots.PublishItem(item, item.BagIndex);
                        }
                        // All quantities/slots are adopted before any mutation capability is released.
                        foreach (var line in lines)
                            if (!line.Token.ReleasePublishedOwnership()) throw new InvalidOperationException("Published Item ownership changed.");
                        slots.Dispose();
                        foreach (var line in lines.Where(line => line.Existing != null))
                        {
                            var expired = owner.ApplyDeferredRewardExpiration(line.Existing);
                            if (notify && expired != null) messages.Add(expired);
                        }
                        state = InventoryRewardReservationState.Published;
                        owner.RewardDeliveries.Remove(this);
                        generation = ++owner.rewardPublicationGeneration;
                        if (notify) { ++owner.rewardNotificationDepth; notifying = true; }
                        refresh = notify && bag.Location == InventoryLocation.Inventory;
                    }
                }
                catch
                {
                    lock (owner.MutationGate) Quarantine();
                    try { session?.ForceDisconnect(); } catch { }
                    throw;
                }
                try
                {
                    try
                    {
                        foreach (var message in messages)
                        {
                            if (!CurrentActor() || Volatile.Read(ref owner.rewardPublicationGeneration) != generation) break;
                            session.EnqueueMessageEncrypted(message);
                        }
                    }
                    finally { if (notifying) lock (owner.MutationGate) --owner.rewardNotificationDepth; }
                    if (refresh && CurrentActor() && Volatile.Read(ref owner.rewardPublicationGeneration) == generation)
                        InventoryQuestObjectiveUpdater.RefreshCollectItemForItem(owner.player, itemId);
                }
                catch
                {
                    // The durable award and adopted inventory are already authoritative.
                    // A notification failure must not re-open the claim or add it again.
                    try { session?.ForceDisconnect(); } catch { }
                    throw;
                }
                return true;
            }

            public bool Cancel()
            {
                var expirations = new List<ServerItemDelete>();
                lock (owner.MutationGate)
                {
                    if (state is not (InventoryRewardReservationState.Prepared or InventoryRewardReservationState.ReadyToRetry)) return false;
                    foreach (var line in lines) if (line.Token != null && !line.Token.Cancel()) return false;
                    slots.Dispose();
                    foreach (var line in lines.Where(line => line.Existing != null))
                    {
                        var expired = owner.ApplyDeferredRewardExpiration(line.Existing);
                        if (expired != null) expirations.Add(expired);
                    }
                    state = InventoryRewardReservationState.Canceled;
                    owner.RewardDeliveries.Remove(this);
                }
                try
                {
                    if (CurrentActor()) foreach (var expired in expirations) session?.EnqueueMessageEncrypted(expired);
                }
                catch { try { session?.ForceDisconnect(); } catch { } }
                return true;
            }

            private void Quarantine()
            {
                owner.rewardRequiresReload = true;
                foreach (var line in lines) line.Token?.AbandonForAuthoritativeReload(true);
                slots.Dispose();
                state = InventoryRewardReservationState.Quarantined;
                owner.RewardDeliveries.Remove(this);
            }

            internal void OnOwnerInvalidated()
            {
                abandonRequested = true;
                if (state is InventoryRewardReservationState.Prepared or InventoryRewardReservationState.ReadyToRetry) Cancel();
                else if (terminal)
                {
                    if (state == InventoryRewardReservationState.Committed) { if (!PublishCommitted()) Quarantine(); }
                    else if (state == InventoryRewardReservationState.Uncertain) Quarantine();
                }
            }

            public void InvalidateOwner()
            {
                lock (owner.MutationGate) OnOwnerInvalidated();
            }

            public void Dispose() => Cancel();
        }
    }
}
