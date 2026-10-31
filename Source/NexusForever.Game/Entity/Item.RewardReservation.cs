using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;

namespace NexusForever.Game.Entity
{
    internal enum RewardItemReservationState
    {
        Prepared, Staged, Uncertain, ReadyToRetry, Committed, Published, Canceled, Quarantined
    }

    public partial class Item
    {
        private RewardMutationReservation rewardMutationReservation;
        private bool requiresAuthoritativeRewardReload;

        internal bool HasRewardMutationReservation
        {
            get { lock (persistenceGate) return rewardMutationReservation != null || requiresAuthoritativeRewardReload; }
        }

        /// <summary>
        /// Reserve the scalars determining a reward merge. SQL enrollment requires
        /// this capability's explicit Stage call and the aggregate's receipt guard.
        /// Retain ownership through outcome resolution and publication.
        /// </summary>
        // Only Bag calls this while holding its mutation gate. Using the same gate
        // prevents acquisition between Bag's preflight and scalar/array mutations.
        internal bool TryReserveRewardMutationFromBag(out RewardMutationReservation reservation)
        {
            lock (persistenceGate)
            {
                reservation = null;
                if (rewardMutationReservation != null || requiresAuthoritativeRewardReload || activeSave != null || uncertainStates != null
                    || uncertainBankStates != null || realmBankBaseline != null || deleteRequested
                    || Info?.Entry == null || !CharacterId.HasValue || Guid == 0
                    || Location is not (InventoryLocation.Inventory or InventoryLocation.PlayerBank))
                    return false;
                rewardMutationReservation = reservation = new RewardMutationReservation(this);
                return true;
            }
        }

        // Called under persistenceGate before assignment, not after MarkDirty.
        private void EnsureRewardMutationAllowed()
        {
            if (rewardMutationReservation != null || requiresAuthoritativeRewardReload)
                throw new InvalidOperationException("Item is reserved for a pending reward delivery.");
        }

        /// <summary>
        /// Construct an unadvertised new bound Item from the aggregate's frozen
        /// template and allocated ID. Validate frozen sockets without allocating an
        /// ID or resampling; retain the table owner for the eventual wire payload.
        /// </summary>
        internal static RewardMutationReservation PrepareNewRewardItem(ItemModel snapshot, IItemInfo info, IGameTableManager gameTableManager = null)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(info);
            uint maximum = info.IsStackable() ? Math.Max(1u, info.Entry?.MaxStackCount ?? 0u) : 1u;
            if (info.Entry == null || snapshot.Id == 0 || !snapshot.OwnerId.HasValue || snapshot.ItemId != info.Id
                || !snapshot.Soulbound || snapshot.StackCount == 0 || snapshot.StackCount > maximum
                || snapshot.Charges > info.Entry.MaxCharges || (info.Entry.MaxCharges != 0 && snapshot.Charges == 0)
                || !float.IsFinite(snapshot.Durability)
                || snapshot.Durability < 0 || snapshot.Durability > 1 || snapshot.MicrochipIds == null || snapshot.RuneSlots == null
                || (InventoryLocation)snapshot.Location is not (InventoryLocation.Inventory or InventoryLocation.PlayerBank))
                throw new ArgumentException("Invalid frozen reward Item template.", nameof(snapshot));
            ItemModel captured = CopyModel(snapshot);
            var item = new Item(captured, info, gameTableManager, true);
            ItemRuneSlotInitializer.ApplyDefaultSockets(item, gameTableManager);
            if (ItemMicrochipIdsCodec.Serialize(item.MicrochipIds) != captured.MicrochipIds
                || ItemRuneSlotsCodec.Serialize(item.RuneSlots) != captured.RuneSlots)
                throw new ArgumentException("Reward Item codecs must be canonical and lossless.", nameof(snapshot));
            return item.rewardMutationReservation = new RewardMutationReservation(item, captured);
        }

        private Item(ItemModel snapshot, IItemInfo info, IGameTableManager gameTableManager, bool rewardCreation)
        {
            Guid = snapshot.Id;
            Info = info;
            this.gameTableManager = gameTableManager;
            characterId = snapshot.OwnerId;
            location = InventoryLocation.None;
            stackCount = snapshot.StackCount;
            charges = snapshot.Charges;
            durability = snapshot.Durability;
            expirationTimeLeft = snapshot.ExpirationTimeLeft;
            soulbound = snapshot.Soulbound;
            ItemMicrochipIdsCodec.DeserializeInto(snapshot.MicrochipIds, MicrochipIds);
            ItemRuneSlotsCodec.DeserializeInto(snapshot.RuneSlots, RuneSlots);
            saveMask = ItemSaveMask.Create;
        }

        internal sealed class RewardMutationReservation : IDisposable
        {
            private readonly Item item;
            private readonly ItemModel initial;
            private readonly ItemModel beforeDatabase;
            private readonly uint maxStackCount;
            private readonly ItemModel newDestination;
            private RewardItemReservationState state = RewardItemReservationState.Prepared;
            private CharacterContext attemptContext;
            private SaveUnit attemptUnit;
            private SaveUnit leaseUnit;
            private string databaseKey;
            private uint? awardedCount;
            private uint finalCount;
            private ItemModel after;
            private ItemModel trackedOriginal;
            private Dictionary<ItemSaveMask, long> capturedRevisions;
            private Action<CharacterSaveValidationPhase> deliveryGuard;
            private bool stageSucceeded;
            private bool contextTerminal;
            private bool contextCommitted;
            private bool itemTerminal;
            private bool itemCommitted;

            internal RewardItemReservationState State { get { lock (item.persistenceGate) return state; } }
            internal ItemModel Before { get { lock (item.persistenceGate) return CopyModel(beforeDatabase); } }
            internal ItemModel PreparedValues { get { lock (item.persistenceGate) return CopyModel(initial); } }
            internal ItemModel After { get { lock (item.persistenceGate) return CopyModel(after); } }

            internal RewardMutationReservation(Item item, ItemModel newDestination = null)
            {
                this.item = item;
                initial = item.CaptureModel();
                beforeDatabase = CopyModel(item.persistenceBaseline);
                maxStackCount = item.Info.IsStackable() ? Math.Max(1u, item.Info.Entry.MaxStackCount) : 1u;
                this.newDestination = CopyModel(newDestination);
            }

            /// <summary>
            /// Enroll a frozen quantity override without changing the live stack.
            /// The aggregate repository must supply its receipt/claimed-state guard;
            /// this leaf does not decide claim eligibility or manufacture a receipt.
            /// Any failure permanently rejects the whole supplied context.
            /// </summary>
            internal ItemModel Stage(CharacterContext context, uint award, Action<CharacterSaveValidationPhase> validateDelivery)
            {
                ArgumentNullException.ThrowIfNull(context);
                lock (item.persistenceGate)
                {
                    try
                    {
                        ArgumentNullException.ThrowIfNull(validateDelivery);
                        if (context.IsSaveRejected || context.HasSavepointRollback)
                            throw new InvalidOperationException("Reward staging requires a healthy save unit.");
                        if (state == RewardItemReservationState.Staged && ReferenceEquals(attemptContext, context)
                            && awardedCount == award && ReferenceEquals(deliveryGuard, validateDelivery) && stageSucceeded)
                            return CopyModel(after);
                        if (state is not (RewardItemReservationState.Prepared or RewardItemReservationState.ReadyToRetry)
                            || !ReferenceEquals(item.rewardMutationReservation, this))
                            throw new InvalidOperationException("Reward Item is not ready for a new enrollment.");
                        if (!initial.Soulbound || (newDestination == null ? award == 0 : award != 0) || initial.StackCount == 0
                            || (ulong)initial.StackCount + award > maxStackCount || !LiveCriticalValuesMatch())
                            throw new InvalidOperationException("Reward merge requires a bound, unchanged, valid whole stack.");
                        if (awardedCount.HasValue && awardedCount.Value != award)
                            throw new InvalidOperationException("A retained reward cannot change its quantity on retry.");
                        string key = SaveDatabaseKey(context);
                        if (databaseKey != null && databaseKey != key)
                            throw new InvalidOperationException("Reward reservation belongs to another configured database.");
                        databaseKey = key;
                        awardedCount = award;
                        finalCount = initial.StackCount + award;
                        attemptContext = context;
                        attemptUnit = null;
                        after = null;
                        trackedOriginal = null;
                        capturedRevisions = null;
                        stageSucceeded = contextTerminal = contextCommitted = itemTerminal = itemCommitted = false;
                        deliveryGuard = validateDelivery;
                        state = RewardItemReservationState.Staged;
                        context.OnSaveCompleted(committed => OnContextCompleted(context, committed));
                        // Enroll before any query/attach can trigger a reentrant save.
                        context.OnSaveValidation(phase => ValidateEnrollment(context, phase));
                        item.SavePersistence(context, false, this);
                        return CopyModel(after);
                    }
                    catch (Exception error)
                    {
                        context.RejectSave(error);
                        throw;
                    }
                }
            }

            internal bool OwnsLease(SaveUnit unit) => ReferenceEquals(leaseUnit, unit);

            internal void AttachUnit(SaveUnit unit) => attemptUnit = leaseUnit = unit;

            internal ItemModel BuildStagedState(ItemModel captured)
            {
                if (!LiveCriticalValuesMatch()) throw new InvalidOperationException("Reserved Item values changed.");
                captured.StackCount = finalCount;
                if (newDestination != null)
                {
                    captured.Location = newDestination.Location;
                    captured.BagIndex = newDestination.BagIndex;
                }
                return captured;
            }

            internal void RecordStage(SaveUnit unit, ItemModel staged)
            {
                if (!CriticalValuesMatch(staged, finalCount, true))
                    throw new InvalidOperationException("Another owner changed the reward Item's tracked identity or merge values.");
                after = CopyModel(staged);
                trackedOriginal = CopyModel((ItemModel)unit.Context.Entry(unit.Model).OriginalValues.ToObject());
                capturedRevisions = new Dictionary<ItemSaveMask, long>(unit.Revisions);
                stageSucceeded = true;
            }

            internal bool CanReuseEnrollment(CharacterContext context) => state == RewardItemReservationState.Staged
                && stageSucceeded && ReferenceEquals(attemptContext, context) && ReferenceEquals(item.activeSave, attemptUnit)
                && !context.IsSaveRejected && !context.HasSavepointRollback;

            private void ValidateEnrollment(CharacterContext context, CharacterSaveValidationPhase phase)
            {
                lock (item.persistenceGate)
                {
                    if (!CanReuseEnrollment(context))
                        throw new InvalidOperationException("Reward Item enrollment is incomplete or changed.");
                    var entry = context.Entry(attemptUnit.Model);
                    ItemModel original = (ItemModel)entry.OriginalValues.ToObject();
                    bool firstWriteReady = beforeDatabase == null ? entry.State == EntityState.Added
                        : entry.State == EntityState.Modified && entry.Property(row => row.StackCount).IsModified;
                    if (!LiveCriticalValuesMatch() || !SameState(after, attemptUnit.Model)
                        || entry.State is EntityState.Detached or EntityState.Deleted
                        || (!attemptUnit.FlushObserved && !firstWriteReady)
                        || (!SameState(original, trackedOriginal) && !(attemptUnit.FlushObserved && SameState(original, after)))
                        || (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && !attemptUnit.FlushObserved))
                        throw new InvalidOperationException("Reward Item enrollment is incomplete, unflushed, or changed.");
                    deliveryGuard(phase);
                }
            }

            private bool CriticalValuesMatch(ItemModel model, uint count, bool delivery = false)
            {
                ItemModel expected = delivery ? newDestination ?? initial : initial;
                return model != null && model.Id == expected.Id && model.OwnerId == expected.OwnerId
                    && model.ItemId == expected.ItemId && model.Location == expected.Location && model.BagIndex == expected.BagIndex
                    && model.StackCount == count && model.Charges == expected.Charges
                    && model.ExpirationTimeLeft == expected.ExpirationTimeLeft && model.Soulbound == expected.Soulbound;
            }

            private bool LiveCriticalValuesMatch() => CriticalValuesMatch(item.CaptureModel(), initial.StackCount);

            private void OnContextCompleted(CharacterContext context, bool committed)
            {
                lock (item.persistenceGate)
                {
                    if (!ReferenceEquals(attemptContext, context)) return;
                    contextTerminal = true;
                    contextCommitted = committed;
                    UpdateTerminalState();
                }
            }

            internal void OnItemCompleted(SaveUnit unit, bool committed)
            {
                if (!ReferenceEquals(attemptUnit, unit)) return;
                itemTerminal = true;
                itemCommitted = committed && unit.FlushObserved && stageSucceeded && SameState(after, unit.FlushedState);
                UpdateTerminalState();
            }

            private void UpdateTerminalState()
            {
                if (!contextTerminal) return;
                state = contextCommitted && itemTerminal && itemCommitted
                    ? RewardItemReservationState.Committed : RewardItemReservationState.Uncertain;
            }

            /// <summary>
            /// After terminal completion, resolve through one fresh serializable transaction.
            /// The repository must verify the exact durable receipt in this same
            /// snapshot: true=committed, false=absent, null=conflict. No row replay is
            /// permitted when a committed receipt's Item has since changed/vanished.
            /// </summary>
            internal RewardItemReservationState ResolveOutcome(CharacterContext fresh, Func<CharacterContext, bool?> verifyReceipt)
            {
                ArgumentNullException.ThrowIfNull(fresh);
                ArgumentNullException.ThrowIfNull(verifyReceipt);
                lock (item.persistenceGate)
                {
                    if (state != RewardItemReservationState.Uncertain || !contextTerminal || item.activeSave != null)
                        return state;
                    if (ReferenceEquals(fresh, attemptContext) || fresh.Database.CurrentTransaction == null
                        || fresh.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable
                        || fresh.IsSaveRejected || fresh.ChangeTracker.Entries().Any() || SaveDatabaseKey(fresh) != databaseKey)
                        throw new InvalidOperationException("Reward resolution requires a fresh serializable transaction in the same database.");
                    bool? receipt = verifyReceipt(fresh);
                    if (fresh.ChangeTracker.Entries().Any())
                        throw new InvalidOperationException("Receipt resolution must use untracked reads.");
                    ItemModel actual = fresh.Item.AsNoTracking().SingleOrDefault(row => row.Id == initial.Id);
                    bool bankOwned = fresh.RealmBankItem.AsNoTracking().Any(row => row.Id == initial.Id);
                    if (!bankOwned && receipt == true && stageSucceeded && SameState(after, actual) && LiveCriticalValuesMatch())
                        state = RewardItemReservationState.Committed;
                    else if (!bankOwned && receipt == false && SameState(beforeDatabase, actual) && LiveCriticalValuesMatch())
                        state = RewardItemReservationState.ReadyToRetry;
                    else
                        state = RewardItemReservationState.Quarantined;
                    return state;
                }
            }

            internal bool CanPublishCommitted
            {
                get
                {
                    lock (item.persistenceGate)
                        return state == RewardItemReservationState.Committed && contextTerminal && item.activeSave == null
                            && ReferenceEquals(item.rewardMutationReservation, this) && LiveCriticalValuesMatch();
                }
            }

            /// <summary>Adopt only committed quantity/baseline; retain later unrelated live mutations.</summary>
            internal NexusForever.Network.World.Message.Model.Shared.Item BuildPublicationPayload()
            {
                lock (item.persistenceGate)
                {
                    if (!CanPublishCommitted) throw new InvalidOperationException("Reward Item is not ready for publication.");
                    var result = item.Build();
                    result.StackCount = finalCount;
                    result.LocationData.Location = (InventoryLocation)after.Location;
                    result.LocationData.BagIndex = after.BagIndex;
                    return result;
                }
            }

            internal Item PublishCommitted(bool retainOwnership = false)
            {
                lock (item.persistenceGate)
                {
                    if (!CanPublishCommitted) return null;
                    item.persistenceBaseline = CopyModel(after);
                    item.uncertainStates = null;
                    foreach ((ItemSaveMask field, long revision) in capturedRevisions)
                        if (item.fieldRevisions.GetValueOrDefault(field) == revision)
                            item.saveMask &= ~field;
                    item.saveMask &= ~(ItemSaveMask.Create | ItemSaveMask.Delete);
                    item.stackCount = finalCount;
                    if (newDestination != null)
                    {
                        item.location = (InventoryLocation)after.Location;
                        item.bagIndex = after.BagIndex;
                    }
                    state = RewardItemReservationState.Published;
                    if (!retainOwnership) ReleasePublishedOwnership();
                    return item;
                }
            }

            internal bool ReleasePublishedOwnership()
            {
                lock (item.persistenceGate)
                {
                    if (state != RewardItemReservationState.Published || !ReferenceEquals(item.rewardMutationReservation, this)) return false;
                    item.rewardMutationReservation = null;
                    ReleaseSaveLease(leaseUnit);
                    return true;
                }
            }

            internal bool Cancel()
            {
                lock (item.persistenceGate)
                {
                    if (state is not (RewardItemReservationState.Prepared or RewardItemReservationState.ReadyToRetry)
                        || item.activeSave != null || !ReferenceEquals(item.rewardMutationReservation, this))
                        return false;
                    state = RewardItemReservationState.Canceled;
                    item.rewardMutationReservation = null;
                    ReleaseSaveLease(leaseUnit);
                    return true;
                }
            }

            /// <summary>
            /// Permit authoritative relog after a terminal unresolved/stale outcome.
            /// The abandoned live Item is permanently unwritable; no old quantity
            /// can be saved after releasing its process lease.
            /// </summary>
            internal bool AbandonForAuthoritativeReload(bool outerUnitTerminal = false)
            {
                lock (item.persistenceGate)
                {
                    bool preparedInTerminalOuterUnit = outerUnitTerminal && state is
                        (RewardItemReservationState.Prepared or RewardItemReservationState.ReadyToRetry);
                    bool retainedPublication = outerUnitTerminal && state == RewardItemReservationState.Published
                        && ReferenceEquals(item.rewardMutationReservation, this);
                    if ((!contextTerminal && !preparedInTerminalOuterUnit) || item.activeSave != null
                        || (!preparedInTerminalOuterUnit && !retainedPublication && state is not
                            (RewardItemReservationState.Uncertain or RewardItemReservationState.Quarantined or RewardItemReservationState.Committed)))
                        return false;
                    item.requiresAuthoritativeRewardReload = true;
                    item.rewardMutationReservation = null;
                    state = RewardItemReservationState.Quarantined;
                    ReleaseSaveLease(leaseUnit);
                    return true;
                }
            }

            // Disposing a staged/uncertain operation must not masquerade as rollback.
            public void Dispose() => Cancel();
        }
    }
}
