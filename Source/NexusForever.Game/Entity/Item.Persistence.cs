using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data.Common;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        private readonly object persistenceGate = new();
        private readonly Dictionary<ItemSaveMask, long> fieldRevisions = [];
        private long persistenceRevision;
        private bool deleteRequested;
        private ItemModel persistenceBaseline;
        private SaveUnit activeSave;
        private List<ItemModel> uncertainStates;
        private ItemModel realmBankBaseline;
        private List<ItemModel> uncertainBankStates;

        // The lease covers live save units, including two Item objects for one GUID.
        // It is process-local ordering, not a database concurrency token or claim receipt.
        private static readonly object saveLeaseGate = new();
        private static readonly Dictionary<(string Database, ulong Id), SaveUnit> saveLeases = [];

        internal sealed class SaveUnit
        {
            public CharacterContext Context;
            public (string Database, ulong Id) Key;
            public ItemModel Model;
            public ItemModel LastState;
            public List<ItemModel> PossibleStates = [];
            public Dictionary<ItemSaveMask, long> Revisions;
            public bool FlushObserved;
            public ItemModel FlushedState;
            public Dictionary<ItemSaveMask, long> FlushedRevisions;
            public EventHandler<SavedChangesEventArgs> OnFlushed;
            public bool BankTransition;
            public ItemModel BankAfter;
            public ItemModel FlushedBankState;
            public List<ItemModel> PossibleBankStates = [];
            public RewardMutationReservation Reward;
        }

        internal bool HasPendingSave
        {
            get { lock (persistenceGate) return activeSave != null; }
        }

        internal bool HasUnsettledDeletion
        {
            get
            {
                lock (persistenceGate)
                    return deleteRequested && (activeSave != null || uncertainStates != null || persistenceBaseline != null
                        || realmBankBaseline != null || uncertainBankStates != null);
            }
        }

        private void MarkDirty(ItemSaveMask field)
        {
            lock (persistenceGate)
            {
                saveMask |= field;
                fieldRevisions[field] = ++persistenceRevision;
            }
        }

        // Realm-bank writes stage the character-row removal in the same context as
        // their bank row. Completion transfers the baseline only after the commit.
        internal ItemModel SaveToRealmBank(CharacterContext context)
        {
            lock (persistenceGate)
            {
                if (Location != InventoryLocation.RealmBank) throw new InvalidOperationException("Item left its bank deposit before capture.");
                SavePersistence(context, true);
                return CopyModel(activeSave.BankAfter);
            }
        }

        internal ItemModel SaveFromRealmBank(CharacterContext context)
        {
            lock (persistenceGate)
            {
                if (Location is InventoryLocation.RealmBank or InventoryLocation.None) throw new InvalidOperationException("Item left its withdrawal destination before capture.");
                SavePersistence(context, false);
                return CopyModel(activeSave?.LastState);
            }
        }

        internal ItemModel SaveConsumedRealmBankItem(CharacterContext context)
        {
            lock (persistenceGate)
            {
                if (Location != InventoryLocation.None || !deleteRequested)
                    throw new InvalidOperationException("Bank stack consumption requires a removed Item and retained deletion intent.");
                var captured = CaptureModel();
                SavePersistence(context, false);
                if (activeSave == null || activeSave.LastState != null)
                    throw new InvalidOperationException("Bank stack consumption lost its deletion owner.");
                return captured;
            }
        }

        internal void ValidateSplitCreation(CharacterContext context)
        {
            lock (persistenceGate)
            {
                if (!PendingCreate || activeSave != null)
                    throw new InvalidOperationException("A split requires its own unfinished new Item identity.");
                bool bankExists = context.RealmBankItem.AsNoTracking().Any(row => row.Id == Guid);
                bool uncertain = uncertainStates != null || uncertainBankStates != null;
                // A retry may observe this same creation after a lost acknowledgement;
                // SavePersistence reconciles only its retained possible states.
                if ((!uncertain && (persistenceBaseline != null || realmBankBaseline != null || bankExists
                    || context.Item.AsNoTracking().Any(row => row.Id == Guid)))
                    || Location != InventoryLocation.RealmBank && bankExists)
                    throw new InvalidOperationException("The split allocator reused an existing storage identity.");
            }
        }

        private void SavePersistence(CharacterContext context, bool toRealmBank, RewardMutationReservation reward = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            lock (persistenceGate)
            {
                if (requiresAuthoritativeRewardReload || (rewardMutationReservation != null
                    && !ReferenceEquals(rewardMutationReservation, reward)))
                {
                    if (!requiresAuthoritativeRewardReload && reward == null && !toRealmBank
                        && rewardMutationReservation.CanReuseEnrollment(context))
                        return;
                    var error = new InvalidOperationException($"Item {Guid} belongs to a reward reservation; ordinary save cannot enroll it.");
                    context.RejectSave(error);
                    throw error;
                }
                if (reward != null && (!ReferenceEquals(rewardMutationReservation, reward) || toRealmBank))
                    throw new InvalidOperationException("Reward enrollment requires its current Item capability.");
                if (activeSave != null && !ReferenceEquals(activeSave.Context, context))
                    throw new InvalidOperationException($"Item {Guid} already belongs to an unfinished save unit.");
                if (activeSave != null && context.HasSavepointRollback)
                    throw new InvalidOperationException($"Item {Guid} requires a fresh save unit after savepoint rollback.");
                if (!toRealmBank && Location == InventoryLocation.RealmBank)
                    return;
                if (activeSave == null && saveMask == ItemSaveMask.None && !toRealmBank && reward == null)
                    return;

                SaveUnit unit = activeSave;
                if (unit == null)
                {
                    unit = new SaveUnit
                    {
                        Context = context,
                        Key = (SaveDatabaseKey(context), Guid),
                        Reward = reward
                    };
                    lock (saveLeaseGate)
                    {
                        if (saveLeases.TryGetValue(unit.Key, out SaveUnit owner) && reward?.OwnsLease(owner) != true)
                            throw new InvalidOperationException($"Item {Guid} is staged by another live owner.");
                        saveLeases[unit.Key] = unit;
                    }
                    activeSave = unit;
                    reward?.AttachUnit(unit);
                    unit.OnFlushed = (_, result) =>
                    {
                        lock (persistenceGate)
                        {
                            if (!ReferenceEquals(activeSave, unit) || unit.Revisions == null)
                                return;
                            if (unit.Reward != null && result.EntitiesSavedCount == 0)
                                return;
                            unit.FlushObserved = true;
                            unit.FlushedState = unit.LastState == null ? null : CopyModel(unit.Model);
                            unit.FlushedRevisions = new Dictionary<ItemSaveMask, long>(unit.Revisions);
                            unit.PossibleStates.Add(CopyModel(unit.FlushedState));
                            unit.FlushedBankState = CopyModel(unit.BankAfter);
                        }
                    };
                    context.SavedChanges += unit.OnFlushed;
                    // Register before any read/attach can fail. Context disposal still
                    // releases a failed staging attempt without consuming its intent.
                    context.OnSaveCompleted(committed => CompleteItemSave(unit, committed));
                    try
                    {
                        ReconcileBaseline(context, toRealmBank);
                        unit.LastState = CopyModel(persistenceBaseline);
                        unit.PossibleStates.Add(CopyModel(persistenceBaseline));
                        unit.BankTransition = toRealmBank || realmBankBaseline != null || uncertainBankStates != null;
                        unit.PossibleBankStates.Add(CopyModel(realmBankBaseline));
                    }
                    catch
                    {
                        // A reward owner retains the failed enrollment until this
                        // rejected context is terminal and a fresh receipt read resolves it.
                        if (reward == null) ReleaseSave(unit);
                        throw;
                    }
                }

                ItemModel state = deleteRequested || toRealmBank ? null : CaptureModel();
                if (reward != null) state = reward.BuildStagedState(state);
                EntityEntry<ItemModel> tracked = context.ChangeTracker.Entries<ItemModel>()
                    .SingleOrDefault(entry => entry.Entity.Id == Guid);
                if (unit.Model == null && tracked != null && (persistenceBaseline == null
                    || !SameState(persistenceBaseline, (ItemModel)tracked.OriginalValues.ToObject())))
                    throw new InvalidOperationException($"Item {Guid} requires its complete loaded model in a shared context.");
                if (unit.Model != null && tracked != null && !ReferenceEquals(tracked.Entity, unit.Model))
                    throw new InvalidOperationException($"Item {Guid} has conflicting tracked ownership.");

                if (state == null)
                {
                    if (tracked != null)
                    {
                        if (tracked.State == EntityState.Added)
                            tracked.State = EntityState.Detached;
                        else
                            tracked.State = EntityState.Deleted;
                        unit.Model = tracked.Entity;
                    }
                    else if (unit.LastState != null)
                    {
                        unit.Model = CopyModel(unit.LastState);
                        context.Attach(unit.Model).State = EntityState.Deleted;
                    }
                }
                else if (unit.LastState == null)
                {
                    if (tracked?.State == EntityState.Deleted && ReferenceEquals(tracked.Entity, unit.Model))
                    {
                        // Cancel a deletion which has not reached SQL yet (buyback).
                        tracked.State = EntityState.Unchanged;
                        tracked.CurrentValues.SetValues(state);
                        state = CopyModel(tracked.Entity);
                    }
                    else
                    {
                        if (tracked != null && tracked.State != EntityState.Detached)
                            throw new InvalidOperationException($"Item {Guid} is already tracked by another create operation.");
                        unit.Model = CopyModel(state);
                        context.Add(unit.Model);
                    }
                }
                else
                {
                    if (tracked == null)
                    {
                        unit.Model = CopyModel(unit.LastState);
                        tracked = context.Attach(unit.Model);
                    }
                    else
                    {
                        if (tracked.State == EntityState.Added && unit.Model == null)
                            throw new InvalidOperationException($"Item {Guid} has a conflicting tracked create.");
                        unit.Model = tracked.Entity;
                        if (tracked.State == EntityState.Deleted)
                            tracked.State = EntityState.Unchanged;
                    }
                    // Preserve unrelated changes made by a mail/auction owner in this
                    // context. Only item-owned dirty columns and the existing codecs
                    // participate in an ordinary update.
                    ApplyDirtyFields(tracked, state, reward == null ? ItemSaveMask.None : ItemSaveMask.StackCount);
                    state = CopyModel(unit.Model);
                }

                unit.LastState = CopyModel(state);
                unit.PossibleStates.Add(CopyModel(state));
                unit.BankAfter = toRealmBank ? CaptureModel() : null;
                unit.PossibleBankStates.Add(CopyModel(unit.BankAfter));
                unit.Revisions = new Dictionary<ItemSaveMask, long>(fieldRevisions);
                reward?.RecordStage(unit, state);
            }
        }

        private void ReconcileBaseline(CharacterContext context, bool toRealmBank)
        {
            if (realmBankBaseline != null || uncertainBankStates != null || toRealmBank)
            {
                RealmBankItemModel bank = context.RealmBankItem.AsNoTracking().SingleOrDefault(model => model.Id == Guid);
                ItemModel actualBank = bank == null ? null : new ItemModel
                {
                    Id = bank.Id, ItemId = bank.ItemId, BagIndex = bank.BagIndex,
                    StackCount = bank.StackCount, Charges = bank.Charges, Durability = bank.Durability,
                    ExpirationTimeLeft = bank.ExpirationTimeLeft, Soulbound = bank.Soulbound,
                    MicrochipIds = bank.MicrochipIds, RuneSlots = bank.RuneSlots
                };
                if (uncertainBankStates != null)
                {
                    if (!uncertainBankStates.Any(state => SameBankState(state, actualBank)))
                        throw new InvalidOperationException($"Bank item {Guid} changed outside its pending save unit.");
                    realmBankBaseline = CopyModel(actualBank);
                    uncertainBankStates = null;
                }
                else if (!SameBankState(realmBankBaseline, actualBank))
                    throw new InvalidOperationException($"Bank item {Guid} no longer matches its loaded values.");

                if (actualBank != null && !toRealmBank && !context.ChangeTracker.Entries<RealmBankItemModel>()
                    .Any(entry => entry.Entity.Id == Guid && entry.State == EntityState.Deleted))
                    throw new InvalidOperationException($"Bank item {Guid} requires an enrolled withdrawal.");
            }
            if (persistenceBaseline == null && uncertainStates == null)
                return; // New IDs use the existing allocator and database primary key.

            ItemModel actual = context.Item.AsNoTracking().SingleOrDefault(model => model.Id == Guid);
            if (uncertainStates != null)
            {
                if (!uncertainStates.Any(state => SameState(state, actual)))
                    throw new InvalidOperationException($"Item {Guid} changed outside its pending save unit.");
                persistenceBaseline = CopyModel(actual);
                uncertainStates = null;
            }
            else if (!SameState(persistenceBaseline, actual))
                throw new InvalidOperationException($"Item {Guid} no longer matches its loaded owner and values.");

            // A rolled-back/ambiguous create must never resurrect a row which has
            // subsequently moved into the shared bank.
            if (actual == null && Location != InventoryLocation.RealmBank
                && context.RealmBankItem.AsNoTracking().Any(model => model.Id == Guid)
                && !context.ChangeTracker.Entries<RealmBankItemModel>().Any(entry =>
                    entry.Entity.Id == Guid && entry.State == EntityState.Deleted))
                throw new InvalidOperationException($"Item {Guid} is owned by the realm bank.");
        }

        private void CompleteItemSave(SaveUnit unit, bool committed)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(activeSave, unit))
                    return;
                try
                {
                    if (unit.Reward != null)
                    {
                        unit.Reward.OnItemCompleted(unit, committed);
                        return;
                    }
                    if (committed && unit.FlushObserved)
                    {
                        persistenceBaseline = CopyModel(unit.FlushedState);
                        if (unit.BankTransition)
                            realmBankBaseline = CopyModel(unit.FlushedBankState);
                        foreach ((ItemSaveMask field, long revision) in unit.FlushedRevisions)
                            if (fieldRevisions.GetValueOrDefault(field) == revision)
                                saveMask &= ~field;
                        saveMask &= ~(ItemSaveMask.Create | ItemSaveMask.Delete);
                        if (deleteRequested && persistenceBaseline != null)
                            saveMask |= ItemSaveMask.Delete;
                        else if (!deleteRequested && persistenceBaseline == null && Location != InventoryLocation.RealmBank)
                            saveMask |= ItemSaveMask.Create;
                    }
                    else if (unit.Revisions != null)
                    {
                        // False includes savepoint rollback and lost acknowledgement.
                        // A later fresh context reads the row and accepts only a state
                        // belonging to this live operation before retrying local intent.
                        uncertainStates = unit.PossibleStates;
                        if (unit.BankTransition)
                            uncertainBankStates = unit.PossibleBankStates;
                    }
                }
                finally { ReleaseSave(unit, unit.Reward == null); }
            }
        }

        private void ReleaseSave(SaveUnit unit, bool releaseLease = true)
        {
            if (unit.OnFlushed != null)
                unit.Context.SavedChanges -= unit.OnFlushed;
            if (ReferenceEquals(activeSave, unit))
                activeSave = null;
            if (releaseLease) ReleaseSaveLease(unit);
        }

        private static void ReleaseSaveLease(SaveUnit unit)
        {
            if (unit == null) return;
            lock (saveLeaseGate)
                if (saveLeases.TryGetValue(unit.Key, out SaveUnit current) && ReferenceEquals(current, unit))
                    saveLeases.Remove(unit.Key);
        }

        private static string SaveDatabaseKey(CharacterContext context)
        {
            // MySQL removes credentials from ConnectionString after opening when
            // PersistSecurityInfo is false. The lease must survive that transition
            // and must not retain a password as an ownership key.
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = context.Database.GetDbConnection().ConnectionString
            };
            builder.Remove("Password");
            builder.Remove("Pwd");
            var normalized = new DbConnectionStringBuilder();
            foreach (string key in builder.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                normalized[key.ToLowerInvariant()] = builder[key];
            return context.Database.ProviderName + ":" + normalized.ConnectionString;
        }

        internal static string GetRewardDatabaseKey(CharacterContext context) => SaveDatabaseKey(context);

        private void ApplyDirtyFields(EntityEntry<ItemModel> entry, ItemModel state, ItemSaveMask forced = ItemSaveMask.None)
        {
            Set(ItemSaveMask.CharacterId, nameof(ItemModel.OwnerId), state.OwnerId);
            Set(ItemSaveMask.Location, nameof(ItemModel.Location), state.Location);
            Set(ItemSaveMask.BagIndex, nameof(ItemModel.BagIndex), state.BagIndex);
            Set(ItemSaveMask.StackCount, nameof(ItemModel.StackCount), state.StackCount);
            Set(ItemSaveMask.Charges, nameof(ItemModel.Charges), state.Charges);
            Set(ItemSaveMask.Durability, nameof(ItemModel.Durability), state.Durability);
            Set(ItemSaveMask.ExpirationTimeLeft, nameof(ItemModel.ExpirationTimeLeft), state.ExpirationTimeLeft);
            Set(ItemSaveMask.Soulbound, nameof(ItemModel.Soulbound), state.Soulbound);
            // Preserve the established update codecs, including values changed via
            // the public list APIs followed by an ordinary scalar mutation.
            entry.Property(model => model.MicrochipIds).CurrentValue = state.MicrochipIds;
            entry.Property(model => model.MicrochipIds).IsModified = true;
            entry.Property(model => model.RuneSlots).CurrentValue = state.RuneSlots;
            entry.Property(model => model.RuneSlots).IsModified = true;

            void Set(ItemSaveMask field, string property, object value)
            {
                if (((saveMask | forced) & field) == 0)
                    return;
                entry.Property(property).CurrentValue = value;
                entry.Property(property).IsModified = true;
            }
        }

        private ItemModel CaptureModel() => new()
        {
            Id = Guid, OwnerId = CharacterId, ItemId = Id, Location = (ushort)Location,
            BagIndex = BagIndex, StackCount = StackCount, Charges = Charges,
            Durability = Durability, ExpirationTimeLeft = ExpirationTimeLeft, Soulbound = Soulbound,
            MicrochipIds = ItemMicrochipIdsCodec.Serialize(MicrochipIds),
            RuneSlots = ItemRuneSlotsCodec.Serialize(RuneSlots)
        };

        internal ItemModel CaptureRewardModel()
        {
            lock (persistenceGate) return CaptureModel();
        }

        private static ItemModel CopyModel(ItemModel model) => model == null ? null : new()
        {
            Id = model.Id, OwnerId = model.OwnerId, ItemId = model.ItemId, Location = model.Location,
            BagIndex = model.BagIndex, StackCount = model.StackCount, Charges = model.Charges,
            Durability = model.Durability, ExpirationTimeLeft = model.ExpirationTimeLeft,
            Soulbound = model.Soulbound, MicrochipIds = model.MicrochipIds, RuneSlots = model.RuneSlots
        };

        private static bool SameState(ItemModel left, ItemModel right)
        {
            if (left == null || right == null)
                return left == right;
            return left.Id == right.Id && left.OwnerId == right.OwnerId && left.ItemId == right.ItemId
                && left.Location == right.Location && left.BagIndex == right.BagIndex
                && left.StackCount == right.StackCount && left.Charges == right.Charges
                && left.Durability.Equals(right.Durability) && left.ExpirationTimeLeft == right.ExpirationTimeLeft
                && left.Soulbound == right.Soulbound && left.MicrochipIds == right.MicrochipIds && left.RuneSlots == right.RuneSlots;
        }

        private static bool SameBankState(ItemModel left, ItemModel right)
        {
            if (left == null || right == null)
                return left == right;
            return left.Id == right.Id && left.ItemId == right.ItemId && left.BagIndex == right.BagIndex
                && left.StackCount == right.StackCount && left.Charges == right.Charges
                && left.Durability.Equals(right.Durability) && left.ExpirationTimeLeft == right.ExpirationTimeLeft
                && left.Soulbound == right.Soulbound
                && (left.MicrochipIds ?? string.Empty) == (right.MicrochipIds ?? string.Empty)
                && (left.RuneSlots ?? string.Empty) == (right.RuneSlots ?? string.Empty);
        }
    }
}
