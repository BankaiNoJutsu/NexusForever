using System.Collections;
using System.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class Inventory : IInventory
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static ulong ItemLocationToDragDropData(InventoryLocation location, ushort slot)
        {
            return ItemLocation.ToDragDropData(location, slot);
        }

        private readonly ulong characterId;
        private readonly IPlayer player;
        private readonly RealmBankManager realmBankManager;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IItemManager itemManager;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<InventoryLocation, IBag> bags = new();
        private readonly List<IItem> deletedItems = new();
        private double itemExpirationAccumulator;

        /// <summary>
        /// Create a new <see cref="IInventory"/> from <see cref="IPlayer"/> database model.
        /// </summary>
        public Inventory(
            IPlayer owner,
            CharacterModel model,
            RealmBankManager realmBankManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            characterId           = owner?.CharacterId ?? 0ul;
            player                = owner;
            this.realmBankManager = realmBankManager;
            this.prerequisiteManager = prerequisiteManager;
            this.itemManager      = itemManager;
            this.gameTableManager = gameTableManager;

            foreach ((InventoryLocation location, uint defaultCapacity) in AssetManager.InventoryLocationCapacities)
                bags.Add(location, new Bag(location, defaultCapacity, itemManager));

            foreach (ItemModel itemModel in model.Item
                .Select(i => i)
                .OrderBy(i => i.Location))
            {
                var item = new Item(itemModel, itemManager, gameTableManager);
                AddItem(item, (InventoryLocation)itemModel.Location, itemModel.BagIndex);
            }
        }

        /// <summary>
        /// Create a new <see cref="IInventory"/> from supplied <see cref="CharacterCreationEntry"/>.
        /// </summary>
        public Inventory(
            ulong owner,
            CharacterCreationEntry creationEntry,
            IItemManager itemManager = null)
        {
            characterId = owner;
            this.itemManager = itemManager;

            foreach ((InventoryLocation location, uint defaultCapacity) in AssetManager.InventoryLocationCapacities)
                bags.Add(location, new Bag(location, defaultCapacity, itemManager));

            foreach (uint itemId in creationEntry.ItemIds.Where(i => i != 0u))
            {
                IItemInfo info = itemManager?.GetItemInfo(itemId);
                if (info == null)
                    throw new ArgumentNullException();

                ItemCreate(info.IsEquippable() ? InventoryLocation.Equipped : InventoryLocation.Inventory, info, 1u);
            }
        }

        public void Update(double lastTick)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (bankPublications != null)
                    foreach (var bank in bankPublications.ToArray()) if (!bank.CurrentActor()) bank.Abandon();
                UpdateItemExpirations(lastTick);
            }
        }

        public void Save(CharacterContext context)
        {
            lock (MutationGate)
            {
                if (realmBankPublishing || RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || realmBankNotifications != null || HasPendingRewardDelivery)
                {
                    var error = new InvalidOperationException("Ordinary inventory save overlaps a pending reward delivery.");
                    context.RejectSave(error);
                    throw error;
                }
                using var mutation = new InventoryMutationOperation(this);
                IItem[] tombstones = deletedItems.ToArray();
                foreach (IItem item in tombstones)
                    item.Save(context);
                context.OnSaveCompleted(committed =>
                {
                    if (!committed)
                        return;
                    lock (MutationGate)
                        foreach (IItem item in tombstones)
                            if (item is Item concrete ? !concrete.HasUnsettledDeletion : !item.PendingDelete)
                                deletedItems.RemoveAll(deleted => ReferenceEquals(deleted, item));
                });

                foreach (IBag bag in bags.Values.Where(b => b.Location != InventoryLocation.Ability
                    && b.Location != InventoryLocation.RealmBank))
                    bag.Save(context);

                EnrollPeriodicRealmBankSave(context);
            }
        }

        /// <summary>
        /// Returns if <see cref="InventoryLocation"/> and bag index is a visible item slot.
        /// </summary>
        public bool IsVisualItemSlot(InventoryLocation location, uint bagIndex)
        {
            return location == InventoryLocation.Equipped
                && (EquippedItem)bagIndex is EquippedItem.Chest or EquippedItem.Head or EquippedItem.Legs or EquippedItem.Hands
                    or EquippedItem.WeaponPrimary or EquippedItem.Shoulder or EquippedItem.Feet;
        }

        /// <summary>
        /// Returns if <see cref="InventoryLocation"/> and bag index is a bag slot.
        /// </summary>
        public bool IsEquippableBagSlot(InventoryLocation location, uint bagIndex)
        {
            return location == InventoryLocation.Equipped
                   && (EquippedItem)bagIndex is EquippedItem.Bag0 or EquippedItem.Bag1 or EquippedItem.Bag2 or EquippedItem.Bag3;
        }

        /// <summary>
        /// Returns if <see cref="InventoryLocation"/> and bag index is a bank bag slot.
        /// </summary>
        public bool IsEquippableBankBagSlot(InventoryLocation location, uint bagIndex)
        {
            return location == InventoryLocation.Equipped
                && (EquippedItem)bagIndex is EquippedItem.BankBag0 or EquippedItem.BankBag1 or EquippedItem.BankBag2
                    or EquippedItem.BankBag3 or EquippedItem.BankBag4 or EquippedItem.BankBag5 or EquippedItem.BankBag6
                    or EquippedItem.BankBag7 or EquippedItem.BankBag8 or EquippedItem.BankBag9;
        }

        /// <summary>
        /// Return if <see cref="InventoryLocation"/> has no free bag indexes remaining.
        /// </summary>
        public bool IsInventoryFull(InventoryLocation location)
        {
            IBag bag = GetBag(location) ?? throw new ArgumentException();
            return RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated || bag.AvailableSlots == 0u;
        }

        /// <summary>
        /// Return the amount of free bag indexes in <see cref="InventoryLocation.Inventory"/>.
        /// </summary>
        public uint GetInventorySlotsRemaining(InventoryLocation location)
        {
            IBag bag = GetBag(location);
            if (bag == null)
                throw new ArgumentException();

            return bag.SlotsRemaining;
        }

        /// <summary>
        /// Returns if the count of items with id exists in <see cref="InventoryLocation.Inventory"/>.
        /// </summary>
        public bool HasItemCount(uint itemId, uint count)
        {
            IBag bag = GetBag(InventoryLocation.Inventory);
            if (bag == null)
                throw new ArgumentException();

            foreach (IItem item in bag.Where(i => i.Id == itemId))
            {
                count -= Math.Min(count, item.StackCount);
                if (count == 0)
                    return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public uint GetItemCount(uint itemId)
        {
            IBag bag = GetBag(InventoryLocation.Inventory);
            if (bag == null)
                throw new ArgumentException();

            ulong total = 0ul;
            foreach (IItem item in bag.Where(i => i.Id == itemId))
                total += item.StackCount;

            return total > uint.MaxValue ? uint.MaxValue : (uint)total;
        }

        /// <summary>
        /// Return <see cref="IItem"/> at supplied <see cref="ItemLocation"/>.
        /// </summary>
        public IItem GetItem(ItemLocation itemLocation)
        {
            return GetItem(itemLocation.Location, itemLocation.BagIndex);
        }

        /// <summary>
        /// Return <see cref="IItem"/> at supplied <see cref="InventoryLocation"/> and bag index.
        /// </summary>
        public IItem GetItem(InventoryLocation location, uint bagIndex)
        {
            IBag bag = GetBag(location);
            if (bag == null)
                throw new ArgumentException();

            IItem item = bag.GetItem(bagIndex);
            if (item == null)
                throw new ArgumentException();

            return item;
        }

        /// <summary>
        /// Return <see cref="IItem"/> with supplied guid.
        /// </summary>
        public IItem GetItem(ulong guid)
        {
            return bags.Values
                .SelectMany(b => b)
                .FirstOrDefault(i => i.Guid == guid);
        }

        /// <summary>
        /// Returns <see cref="IItemVisual"/> for any visible items.
        /// </summary>
        public IEnumerable<IItemVisual> GetItemVisuals()
        {
            IBag bag = GetBag(InventoryLocation.Equipped);
            Debug.Assert(bag != null);

            foreach (IItem item in bag)
            {
                if (!IsVisualItemSlot(item.Location, item.BagIndex))
                    continue;

                yield return new ItemVisual
                {
                    Slot      = (ItemSlot)item.Info.TypeEntry.ItemSlotId,
                    DisplayId = item.Info.GetDisplayId()
                };
            }
        }

        /// <summary>
        /// Create a new <see cref="IItem"/> from supplied <see cref="Spell4BaseEntry"/> in the first available <see cref="InventoryLocation.Ability"/> bag slot.
        /// </summary>
        public IItem SpellCreate(Spell4BaseEntry spell4BaseEntry, ItemUpdateReason reason = ItemUpdateReason.NoReason)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                if (spell4BaseEntry == null)
                    throw new ArgumentNullException();

                var spell = new Item(characterId, spell4BaseEntry, itemManager: itemManager, gameTableManager: gameTableManager);
                AddItem(spell, InventoryLocation.Ability, reason);

                return spell;
            }
        }

        /// <summary>
        /// Returns if a new item can be created in the supplied <see cref="InventoryLocation"/>.
        /// </summary>
        public bool CanCreateItem(InventoryLocation location, uint itemId, uint count)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this, readOnly: true);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) return false;
                IItemInfo info = itemManager?.GetItemInfo(itemId);
                if (info == null)
                    throw new ArgumentNullException();

                return CanCreateItem(location, info, count);
            }
        }

        /// <summary>
        /// Returns if a new item can be created in the supplied <see cref="InventoryLocation"/>.
        /// </summary>
        public bool CanCreateItem(InventoryLocation location, IItemInfo info, uint count)
            => CanCreateItem(location, info, count, location == InventoryLocation.Inventory ? player?.SupplySatchelManager : null);

        private bool CanCreateItem(InventoryLocation location, IItemInfo info, uint count, ISupplySatchelManager satchel)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this, readOnly: true);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) return false;
                if (satchel?.TryGetAutoStoreCapacity(info, out uint capacity) == true)
                    count -= Math.Min(count, capacity);

                return CanCreateItemInBag(location, info, count);
            }
        }

        private bool CanCreateItemInBag(InventoryLocation location, IItemInfo info, uint count)
        {
            if (info == null)
                throw new ArgumentNullException();

            IBag bag = GetBag(location);
            if (bag == null)
                throw new ArgumentException();

            ulong slotsNeeded = CountRequiredSlots(bag, info, count);
            if (slotsNeeded == 0)
                return true;
            if (location == InventoryLocation.Equipped)
                return slotsNeeded == 1 && bag.GetFirstAvailableBagIndex((ItemSlot)info.SlotEntry.Id).HasValue;

            return bag.AvailableSlots >= slotsNeeded;
        }

        internal bool CanCreateQuestRewardItems(IEnumerable<(uint ItemId, uint Count)> rewards,
            IEnumerable<(uint ItemId, uint Count)> reclaimedItems)
            => CanCreateItemsAfterReclaim(rewards.Select(reward => (Info: itemManager?.GetItemInfo(reward.ItemId)
                ?? throw new ArgumentNullException(nameof(rewards)), reward.Count)), reclaimedItems, player?.SupplySatchelManager,
                requireFullReclaim: false);

        internal bool CanCreateVendorPurchase(IItemInfo info, uint count, ISupplySatchelManager satchel, IReadOnlyDictionary<uint, uint> itemCosts)
            => itemCosts.Count == 0
                ? CanCreateItem(InventoryLocation.Inventory, info, count, satchel)
                : CanCreateItemsAfterReclaim([(info, count)], itemCosts.Select(cost => (cost.Key, cost.Value)), satchel, requireFullReclaim: true);

        public bool TrySaveResourceConversion(GameSession session, Func<bool> convert)
        {
            if (player is not Player owner || owner.IsNormalSaveInProgress)
                return false;

            var account = owner.Account;
            var satchel = owner.SupplySatchelManager;
            ulong? epoch = account?.WorldSessionEpoch;
            var events = session.Events;
            bool OwnsActor() => ReferenceEquals(owner.Session, session) && ReferenceEquals(session.Events, events)
                && ReferenceEquals(owner.Inventory, this) && owner.CharacterId == characterId
                && ReferenceEquals(owner.SupplySatchelManager, satchel)
                && ReferenceEquals(owner.Account, account) && account?.WorldSessionEpoch == epoch
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, owner)
                && !session.IsDisconnecting && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            if (!OwnsActor() || !InventoryMutationScope.TryAcquire(this, out var lease))
                return false;

            try
            {
                bool converted = false;
                Action publish;
                using (lease)
                    publish = session.CaptureOutgoingPackets(() => converted = convert());

                if (converted)
                {
                    if (!OwnsActor())
                        throw new InvalidOperationException("Resource conversion lost its original actor before saving.");

                    session.CanProcessIncomingPackets = false;
                    owner.SaveResourceConversion(Publish);
                }
                else Publish();
                return converted;

                void Publish()
                {
                    if (OwnsActor()) publish();
                }
            }
            catch
            {
                rewardRequiresReload = true;
                if (OwnsActor()) session.ForceDisconnect();
                throw;
            }
        }

        /// <summary>Exchange backpack items only when the complete result fits after consumption.</summary>
        public bool TryConvertResourceItems(uint sourceId, uint sourceCount, uint targetId, uint targetCount)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (sourceCount == 0 || targetCount == 0 || RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked
                    || rewardOwnerInvalidated || commodityDelivery != null || !HasItemCount(sourceId, sourceCount))
                    return false;

                IItemInfo target = itemManager?.GetItemInfo(targetId);
                if (target?.Entry == null)
                    return false;
                if (!CanCreateItemsAfterReclaim([(target, targetCount)], [(sourceId, sourceCount)], satchel: null, requireFullReclaim: true))
                {
                    EnqueueItemMessage(new ServerItemError { ErrorCode = GenericError.ItemInventoryFull });
                    return false;
                }

                ItemDelete(sourceId, sourceCount, ItemUpdateReason.ResourceConversion);
                ItemCreate(InventoryLocation.Inventory, target, targetCount, ItemUpdateReason.ResourceConversion);
                return true;
            }
        }

        private bool CanCreateItemsAfterReclaim(IEnumerable<(IItemInfo Info, uint Count)> rewards,
            IEnumerable<(uint ItemId, uint Count)> reclaimedItems, ISupplySatchelManager satchel, bool requireFullReclaim)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this, readOnly: true);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) return false;
                IBag bag = GetBag(InventoryLocation.Inventory) ?? throw new ArgumentException();
                ulong available = bag.AvailableSlots;
                var afterReclaim = new Dictionary<IItem, uint>(ReferenceEqualityComparer.Instance);
                foreach (var (itemId, count) in reclaimedItems)
                {
                    uint remaining = count;
                    foreach (IItem stack in bag.Where(item => item.Id == itemId))
                    {
                        if (remaining == 0) break;
                        uint stackCount = afterReclaim.GetValueOrDefault(stack, stack.StackCount);
                        if (stackCount == 0) continue;
                        if (bag.IsSlotReserved(stack.BagIndex) || IsBankSavePending(stack)) return false;

                        uint consumed = Math.Min(remaining, stackCount);
                        remaining -= consumed;
                        afterReclaim[stack] = stackCount - consumed;
                        if (consumed == stackCount) available++;
                    }
                    if (requireFullReclaim && remaining != 0u) return false;
                }

                foreach (var item in rewards.GroupBy(reward => reward.Info.Id))
                {
                    IItemInfo info = item.First().Info;
                    ulong count = item.Aggregate(0ul, (total, reward) => total + reward.Count);
                    if (satchel?.TryGetAutoStoreCapacity(info, out uint capacity) == true)
                        count -= Math.Min(count, capacity);
                    ulong needed = CountRequiredSlots(bag, info, count, afterReclaim);
                    if (needed > available) return false;
                    available -= needed;
                }
                return true;
            }
        }

        private static ulong CountRequiredSlots(IBag bag, IItemInfo info, ulong count,
            IReadOnlyDictionary<IItem, uint> afterReclaim = null)
        {
            if (count == 0u)
                return 0;

            if (!info.IsStackable())
                return count;

            uint maxStackCount = Math.Max(1u, info.Entry.MaxStackCount);
            count = CountRemainingAfterStacking(bag, info, count, maxStackCount, afterReclaim);
            return (count + maxStackCount - 1) / maxStackCount;
        }

        private static ulong CountRemainingAfterStacking(IBag bag, IItemInfo info, ulong count, uint maxStackCount,
            IReadOnlyDictionary<IItem, uint> afterReclaim = null)
        {
            uint expirationTimeLeft = GetInitialExpirationTimeLeft(info);
            foreach (IItem item in bag.Where(i => !bag.IsSlotReserved(i.BagIndex) && i.Info.Id == info.Id && i.ExpirationTimeLeft == expirationTimeLeft))
            {
                if (count == 0u)
                    break;

                uint stackCount = item.StackCount;
                if (afterReclaim != null && afterReclaim.TryGetValue(item, out uint after))
                {
                    if (after == 0) continue;
                    stackCount = after;
                }

                if (stackCount < maxStackCount)
                    count -= Math.Min(count, maxStackCount - stackCount);
            }
            return count;
        }

        internal bool WithdrawSupplySatchelItem(SupplySatchelManager satchel, ITradeskillMaterial material, uint count)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (!ReferenceEquals(player?.SupplySatchelManager, satchel) || RequiresAuthoritativeReload
                    || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated || commodityDelivery != null)
                    return false;

                IItemInfo info = itemManager?.GetItemInfo(material.Entry.Item2IdStatRevolution);
                IBag bag = GetBag(InventoryLocation.Inventory);
                if (info?.Entry == null || bag == null)
                    return false;

                bool stackable = info.IsStackable();
                uint perSlot = stackable ? info.Entry.MaxStackCount : 1u;
                if (perSlot == 0u)
                    return false;

                ulong remaining = stackable ? CountRemainingAfterStacking(bag, info, count, perSlot) : count;
                ulong unplaced = remaining - Math.Min(remaining, (ulong)bag.AvailableSlots * perSlot);
                uint withdrawn = count - (uint)unplaced;
                if (withdrawn == 0u)
                    return false;

                satchel.RemoveAmount(material.MaterialId, withdrawn);
                ItemCreate(InventoryLocation.Inventory, info, withdrawn, ItemUpdateReason.ResourceConversion);
                return true;
            }
        }

        /// <inheritdoc />
        public bool SupportsSoulboundItemCreation => true;

        /// <inheritdoc />
        public bool CanCreateSoulboundItem(InventoryLocation location, IItemInfo info, uint count, out GenericError error, uint charges = 0)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                return TryPlanSoulboundCreation(location, info, count, charges, out _, out error);
            }
        }

        /// <inheritdoc />
        public bool TryCreateSoulboundItem(InventoryLocation location, IItemInfo info, uint count, out GenericError error,
            ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (!TryPlanSoulboundCreation(location, info, count, charges, out SoulboundCreationPlan plan, out error))
                    return false;

                using var notifications = BeginRealmBankNotifications(location == InventoryLocation.RealmBank);

                // The complete physical destination is known before creating any item
                // (which allocates its ID), changing a stack, or emitting a packet.
                var newItems = new List<(Item Item, uint BagIndex)>();
                foreach ((uint bagIndex, uint stackCount) in plan.NewStacks)
                {
                    var item = new Item(characterId, info, stackCount, charges, itemManager, gameTableManager);
                    item.MakeSoulbound();
                    newItems.Add((item, bagIndex));
                }

                foreach (var (item, stackCount, _) in plan.StackUpdates)
                {
                    if (player == null)
                        item.StackCount = stackCount;
                    else
                        ItemStackCountUpdate(item, stackCount, reason);
                }

                foreach ((Item item, uint bagIndex) in newItems)
                {
                    AddItem(item, location, bagIndex);
                    if (player?.IsLoading == false)
                    {
                        EnqueueItemMessage(new ServerItemAdd
                        {
                            InventoryItem = new InventoryItem { Item = item.Build(), Reason = reason }
                        });
                    }
                }

                if (count != 0u && location == InventoryLocation.Inventory && player?.IsLoading == false)
                    RefreshCollectedItem(info.Id);
                notifications?.Complete();
                return true;
            }
        }

        private bool TryPlanSoulboundCreation(InventoryLocation location, IItemInfo info, uint count, uint charges,
            out SoulboundCreationPlan plan, out GenericError error)
        {
            plan = null;
            error = GenericError.ItemNotValidForSlot;
            if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) return false;
            error = GenericError.ItemBadId;
            if (info?.Entry == null || info.Id == 0u)
                return false;

            // These are character-owned physical bags. Shared storage and direct
            // equip paths must retain their separate entitlement/slot validation.
            error = GenericError.ItemNotValidForSlot;
            if (location is not (InventoryLocation.Inventory or InventoryLocation.PlayerBank))
                return false;
            IBag bag = GetBag(location);
            error = GenericError.ItemCannotFindBag;
            if (bag == null)
                return false;

            plan = new SoulboundCreationPlan();
            uint remaining = count;
            uint maxStackCount = info.IsStackable() ? Math.Max(1u, info.Entry.MaxStackCount) : 1u;
            if (remaining != 0u && info.IsStackable())
            {
                uint expiration = GetInitialExpirationTimeLeft(info);
                uint effectiveCharges = charges != 0u ? charges : info.Entry.MaxCharges;
                foreach (IItem item in bag.Where(item => !bag.IsSlotReserved(item.BagIndex)))
                {
                    ItemModel before = item is Item concrete ? concrete.CaptureRewardModel() : new ItemModel
                    {
                        Id = item.Guid, OwnerId = item.CharacterId, ItemId = item.Info?.Id ?? 0u,
                        Location = (ushort)item.Location, BagIndex = item.BagIndex, StackCount = item.StackCount,
                        Charges = item.Charges, ExpirationTimeLeft = item.ExpirationTimeLeft, Soulbound = item.Soulbound
                    };
                    if (!before.Soulbound || before.ItemId != info.Id || before.ExpirationTimeLeft != expiration
                        || before.Charges != effectiveCharges || before.StackCount >= maxStackCount)
                        continue;
                    uint added = Math.Min(remaining, maxStackCount - before.StackCount);
                    plan.StackUpdates.Add((item, before.StackCount + added, before));
                    remaining -= added;
                    if (remaining == 0u)
                        break;
                }
            }

            error = GenericError.ItemInventoryFull;
            ulong slotsNeeded = ((ulong)remaining + maxStackCount - 1ul) / maxStackCount;
            if (slotsNeeded > bag.AvailableSlots)
                return false;
            for (uint index = 0; remaining != 0u && index < bag.Slots; index++)
            {
                if (bag.GetItem(index) != null || bag.IsSlotReserved(index))
                    continue;
                uint stackCount = Math.Min(remaining, maxStackCount);
                plan.NewStacks.Add((index, stackCount));
                remaining -= stackCount;
            }
            if (remaining != 0u)
                return false;

            error = GenericError.Ok;
            return true;
        }

        private sealed class SoulboundCreationPlan
        {
            public List<(IItem Item, uint StackCount, ItemModel Before)> StackUpdates { get; } = new();
            public List<(uint BagIndex, uint StackCount)> NewStacks { get; } = new();
        }

        /// <summary>
        /// Create a new <see cref="IItem"/> in the first available inventory bag index or stack.
        /// </summary>
        public void ItemCreate(InventoryLocation location, uint itemId, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IItemInfo info = itemManager?.GetItemInfo(itemId);
                if (info == null)
                    throw new ArgumentNullException();

                ItemCreate(location, info, count, reason, charges);
            }
        }

        /// <summary>
        /// Create a new <see cref="IItem"/> in the first available inventory bag index or stack.
        /// </summary>
        public void ItemCreate(InventoryLocation location, IItemInfo info, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
            => ItemCreate(location, info, count, reason, charges, location == InventoryLocation.Inventory ? player?.SupplySatchelManager : null);

        internal void CreateVendorPurchase(IItemInfo info, uint count, ISupplySatchelManager satchel)
            => ItemCreate(InventoryLocation.Inventory, info, count, ItemUpdateReason.NoReason, 0, satchel);

        private void ItemCreate(InventoryLocation location, IItemInfo info, uint count, ItemUpdateReason reason, uint charges,
            ISupplySatchelManager satchel)
        {
            lock (MutationGate)
            {
                if (commodityDelivery?.DeferCreation(location, info, count, reason, charges) == true) return;
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                if (info == null)
                    throw new ArgumentNullException();

                IBag bag = GetBag(location);
                if (bag == null)
                    throw new ArgumentException();

                if (count > 0u && reason != ItemUpdateReason.ResourceConversion
                    && satchel?.TryGetAutoStoreCapacity(info, out uint capacity, charges) == true)
                {
                    uint toSatchel = Math.Min(count, capacity);
                    // Validate both destinations before sending a satchel update or modifying
                    // any bag stack. A full satchel naturally falls back to the bag.
                    if (!CanCreateItemInBag(location, info, count - toSatchel))
                    {
                        EnqueueItemMessage(new ServerItemError { ErrorCode = GenericError.ItemInventoryFull });
                        return;
                    }

                    if (toSatchel > 0u)
                    {
                        var material = new Item(characterId, info, toSatchel, charges, itemManager, gameTableManager);
                        uint remainder = satchel.AddAmount(material, toSatchel);
                        uint stored = toSatchel - remainder;
                        if (remainder != 0u && !CanCreateItemInBag(location, info, count - stored))
                        {
                            // Capacity should be stable on the owning map thread. Retain
                            // quantities even if a custom manager changes it during this call.
                            if (stored != 0u)
                                satchel.RemoveAmount(material, stored);
                            EnqueueItemMessage(new ServerItemError { ErrorCode = GenericError.ItemInventoryFull });
                            return;
                        }
                        count -= stored;
                    }
                }

                using var notifications = BeginRealmBankNotifications(location == InventoryLocation.RealmBank);
                // update any existing stacks before creating new items
                if (info.IsStackable())
                {
                    uint expirationTimeLeft = GetInitialExpirationTimeLeft(info);
                    foreach (IItem item in bag.Where(i => !bag.IsSlotReserved(i.BagIndex) && i.Info.Id == info.Id && i.ExpirationTimeLeft == expirationTimeLeft))
                    {
                        if (count == 0u)
                            break;

                        if (item.StackCount >= info.Entry.MaxStackCount)
                            continue;

                        uint newStackCount = Math.Min(item.StackCount + count, info.Entry.MaxStackCount);
                        count -= newStackCount - item.StackCount;
                        ItemStackCountUpdate(item, newStackCount, reason);
                    }
                }

                // create new stacks for the remaining count
                while (count > 0)
                {
                    uint? bagIndex = location == InventoryLocation.Equipped ? bag.GetFirstAvailableBagIndex((ItemSlot)info.SlotEntry.Id) : bag.GetFirstAvailableBagIndex();
                    if (!bagIndex.HasValue)
                    {
                        // If there is remaining count left, and this was created by SupplySatchelManager, then return the rest to the client.
                        if (count > 0 && reason == ItemUpdateReason.ResourceConversion)
                            player.SupplySatchelManager.AddAmount(new Item(characterId, info, count, charges, itemManager, gameTableManager), count);
                        else
                        {
                            EnqueueItemMessage(new ServerItemError
                            {
                                ErrorCode = GenericError.ItemInventoryFull
                            });
                        }

                        notifications?.Complete();
                        return;
                    }

                    var item = new Item(characterId, info, Math.Min(count, info.IsStackable() ? info.Entry.MaxStackCount : 1), charges, itemManager, gameTableManager);
                    AddItem(item, location, bagIndex.Value);

                    if (!player?.IsLoading ?? false)
                    {
                        EnqueueItemMessage(new ServerItemAdd
                        {
                            InventoryItem = new InventoryItem
                            {
                                Item   = item.Build(),
                                Reason = reason
                            }
                        });
                    }

                    count -= item.StackCount;
                }

                if (location == InventoryLocation.Inventory && player != null && !player.IsLoading)
                    RefreshCollectedItem(info.Id);
                notifications?.Complete();
            }
        }

        /// <summary>
        /// Returns if <see cref="IItem"/> can be moved to supplied <see cref="ItemLocation"/>.
        /// </summary>
        public GenericError? CanMoveItem(IItem item, ItemLocation location)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this, readOnly: true);
                return CanMoveItem(item, location.Location, location.BagIndex);
            }
        }

        /// <summary>
        /// Returns if <see cref="IItem"/> can be moved to supplied <see cref="InventoryLocation"/> and bag index.
        /// </summary>
        public GenericError? CanMoveItem(IItem item, InventoryLocation location, uint bagIndex)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this, readOnly: true);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) return GenericError.ItemNotValidForSlot;
                // Move rollback snapshots cover every bag, including unrelated slots.
                if (bags.Values.Any(bag => bag.HasReservedSlots))
                    return GenericError.ItemNotValidForSlot;
                // ItemInventoryFull
                // ItemUnique
                // ItemNotValidForSlot
                // ItemBagMustBeEmpty
                // ItemWrongRace
                // ItemWrongClass
                // ItemLevelToLow
                // PlayerCannotWhileInCombat
                // ItemCannotBeSalvaged
                // ItemWrongFaction
                // ItemCannotBeDeleted

                if (item == null)
                    return GenericError.ItemBadId;
                if (item.Info == null)
                    return GenericError.ItemNotValidForSlot;

                IBag dstBag = GetBag(location);
                if (dstBag == null)
                    return GenericError.ItemNotValidForSlot;

                GenericError? realmBankError = EnsureRealmBankAccessForMove(item, location, bagIndex);
                if (realmBankError.HasValue)
                    return realmBankError;

                if (bagIndex >= dstBag.Slots)
                    return GenericError.ItemNotValidForSlot;

                IItem dstItem = dstBag.GetItem(bagIndex);
                if (dstItem == null && item.Location != location && dstBag.SlotsRemaining == 0u)
                    return GenericError.ItemInventoryFull;

                if (location == InventoryLocation.Inventory)
                {
                    // when removing bag capacity, make sure there is enough room left for items in the inventory
                    GenericError? bagCapacityError = CanApplyBagCapacityChangeForMove(item, dstItem, location, bagIndex);
                    if (bagCapacityError.HasValue)
                        return bagCapacityError;
                }
                else if (location == InventoryLocation.Equipped)
                {
                    if (!item.Info.IsEquippable())
                        return GenericError.ItemNotValidForSlot;

                    if (!item.Info.IsEquippableIntoSlot((EquippedItem)bagIndex))
                        return GenericError.ItemNotValidForSlot;

                    if (item.RuneSlots.Count == 0)
                        ItemRuneSlotInitializer.ApplyDefaultSockets(item);

                    uint equipPrerequisiteId = item.Info.Entry.PrerequisiteId;
                    if (equipPrerequisiteId != 0u && player != null
                        && !PrerequisiteEvaluation.Meets(player, equipPrerequisiteId, item, prerequisiteManager))
                        return GenericError.ItemEquipPrereqFailed;

                    GenericError? bagCapacityError = CanApplyBagCapacityChangeForMove(item, dstItem, location, bagIndex);
                    if (bagCapacityError.HasValue)
                        return bagCapacityError;

                    /*if (owner.Character.Class != item.Entry.ClassRequired)
    			        return GenericError.ItemWrongClass;

    		        if (owner.Character.Race != item.Entry.RaceRequired)
    			        return GenericError.ItemWrongRace;*/
                }
                else
                {
                    GenericError? bagCapacityError = CanApplyBagCapacityChangeForMove(item, dstItem, location, bagIndex);
                    if (bagCapacityError.HasValue)
                        return bagCapacityError;
                }

                return null;
            }
        }

        /// <summary>
        /// Move <see cref="IItem"/> to supplied <see cref="ItemLocation"/>.
        /// </summary>
        public void ItemMove(IItem item, ItemLocation location)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                ItemMove(item, location.Location, location.BagIndex);
            }
        }

        /// <summary>
        /// Move <see cref="IItem"/> to supplied <see cref="InventoryLocation"/> and bag index.
        /// </summary>
        public void ItemMove(IItem item, InventoryLocation location, uint bagIndex)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                if (item == null)
                    throw new ArgumentNullException();

                IBag bag = GetBag(item.Location);
                if (bag == null)
                    throw new ArgumentException();

                IBag dstBag = GetBag(location);
                if (dstBag == null)
                    throw new ArgumentException();

                GenericError? result = CanMoveItem(item, location, bagIndex);
                if (result.HasValue)
                {
                    EnqueueItemMessage(new ServerItemError
                    {
                        ErrorCode = result.Value
                    });
                    return;
                }

                IItem dstItem = dstBag.GetItem(bagIndex);
                if (ReferenceEquals(item, dstItem))
                    return;
                using var notifications = BeginRealmBankNotifications(bag.Location == InventoryLocation.RealmBank || dstBag.Location == InventoryLocation.RealmBank);
                InventoryOperationSnapshot snapshot = CreateOperationSnapshot();
                bool wasSoulbound = item.Soulbound;
                bool dstWasSoulbound = dstItem?.Soulbound ?? false;
                Task bankPersistence = null;

                try
                {
                    if (dstItem == null)
                    {
                        // item move is between the same bag, just a simple move
                        if (bag.Location == dstBag.Location)
                            bag.MoveItem(item, bagIndex);
                        else
                        {
                            RemoveItem(item);

                            // ensure slot is still valid
                            // this can happen when removing a bag to a slot that was removed during the resize
                            if (bagIndex >= dstBag.Slots)
                            {
                                uint? newBagIndex = dstBag.GetFirstAvailableBagIndex();
                                if (!newBagIndex.HasValue)
                                    throw new InvalidOperationException();

                                bagIndex = newBagIndex.Value;
                            }

                            AddItem(item, location, bagIndex, persistRealmBank: false);
                        }

                        BindOnEquip(item);
                        bankPersistence = PersistRealmBankStorage(item);

                        EnqueueItemMessage(new ServerItemMove
                        {
                            To = new ItemDragDrop
                            {
                                Guid     = item.Guid,
                                DragDrop = ItemLocationToDragDropData(item.Location, (ushort)item.BagIndex)
                            }
                        });

                        SendBindOnEquipRefreshIfNeeded(item, wasSoulbound);
                    }
                    else
                    {
                        // item at destination with same entry, try and stack
                        if (dstItem.Info.IsStackable()
                            && item.Info.Entry.Id == dstItem.Info.Entry.Id
                            && item.ExpirationTimeLeft == dstItem.ExpirationTimeLeft)
                        {
                            uint newStackCount = Math.Min(dstItem.StackCount + item.StackCount, dstItem.Info.Entry.MaxStackCount);
                            uint oldStackCount = item.StackCount - (newStackCount - dstItem.StackCount);
                            ItemStackCountUpdate(dstItem, newStackCount, persistRealmBank: false);

                            if (oldStackCount == 0u)
                            {
                                ItemDelete(bag, item, ItemUpdateReason.Loot,
                                    persistRealmBank: bag.Location != InventoryLocation.RealmBank && dstBag.Location != InventoryLocation.RealmBank);
                            }
                            else
                                ItemStackCountUpdate(item, oldStackCount, persistRealmBank: false);
                        }
                        // item swap is between the same bag, just a simple move
                        else if (bag.Location == dstBag.Location)
                            bag.SwapItem(item, dstItem);
                        // items being swapped are 2 bags, we need to calculate the difference between the two
                        else if (item.Info.IsEquippableBag() && dstItem.Info.IsEquippableBag())
                        {
                            bag.RemoveItem(item);
                            dstBag.RemoveItem(dstItem);
                            bag.AddItem(dstItem, item.PreviousBagIndex);
                            dstBag.AddItem(item, dstItem.PreviousBagIndex);

                            int capacityChange = (int)dstItem.Info.Entry.MaxStackCount - (int)item.Info.Entry.MaxStackCount;
                            if (capacityChange != 0)
                            {
                                InventoryLocation? expandedLocation = GetExpandedBagLocation(dstItem.Location, dstItem.BagIndex);
                                if (expandedLocation.HasValue)
                                    InventoryResize(expandedLocation.Value, capacityChange);
                            }
                        }
                        else
                        {
                            RemoveItem(item);
                            RemoveItem(dstItem);
                            AddItem(item, dstItem.PreviousLocation, dstItem.PreviousBagIndex, persistRealmBank: false);
                            AddItem(dstItem, item.PreviousLocation, item.PreviousBagIndex, persistRealmBank: false);
                        }

                        BindOnEquip(item);
                        BindOnEquip(dstItem);
                        if (bag.Location == InventoryLocation.RealmBank || dstBag.Location == InventoryLocation.RealmBank)
                            bankPersistence = PersistRealmBankMove(item, dstItem);
                        else
                        {
                            PersistRealmBankStorage(item);
                            PersistRealmBankStorage(dstItem);
                        }

                        EnqueueItemMessage(new ServerItemSwap
                        {
                            To = new ItemDragDrop
                            {
                                Guid     = item.Guid,
                                DragDrop = ItemLocationToDragDropData(item.Location, (ushort)item.BagIndex)
                            },
                            From = new ItemDragDrop
                            {
                                Guid     = dstItem.Guid,
                                DragDrop = ItemLocationToDragDropData(dstItem.Location, (ushort)dstItem.BagIndex)
                            }
                        });

                        SendBindOnEquipRefreshIfNeeded(item, wasSoulbound);
                        SendBindOnEquipRefreshIfNeeded(dstItem, dstWasSoulbound);
                    }
                    notifications?.Complete();
                }
                catch (Exception exception)
                {
                    // Once SQL owns this move, a notification error cannot undo
                    // its live Items while the captured transaction may commit.
                    if (bankPersistence != null) rewardRequiresReload = true;
                    else RestoreOperationSnapshot(snapshot);
                    log.Fatal(exception);
                    player.Session.EnqueueMessageEncrypted(new ServerItemError
                    {
                        ErrorCode = GenericError.DbFailure
                    });
                }
            }
        }

        /// <summary>
        /// Split a subset of <see cref="IItem"/> to create a new <see cref="IItem"/> of split amount
        /// </summary>
        public void ItemSplit(ulong itemGuid, ItemLocation newItemLocation, uint count)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IItem item = GetItem(itemGuid);
                if (item == null)
                    throw new InvalidPacketValueException();

                if (item.Info.IsEquippableBag())
                    throw new InvalidPacketValueException();

                if (item.Info.Entry.MaxStackCount <= 1u)
                    throw new InvalidPacketValueException();

                if (count == 0u || count >= item.StackCount)
                    throw new InvalidPacketValueException();

                IBag dstBag = GetBag(newItemLocation.Location);
                if (dstBag == null)
                    throw new InvalidPacketValueException();

                GenericError? realmBankError = EnsureRealmBankAccessForMove(item, newItemLocation.Location, newItemLocation.BagIndex);
                if (realmBankError.HasValue)
                    throw new InvalidPacketValueException();

                IItem dstItem = dstBag.GetItem(newItemLocation.BagIndex);
                if (dstItem != null)
                    throw new InvalidPacketValueException();
                if (bags.Values.Any(bag => bag.HasReservedSlots))
                    throw new InvalidPacketValueException();

                bool bankSplit = item.Location == InventoryLocation.RealmBank || newItemLocation.Location == InventoryLocation.RealmBank;
                using var notifications = BeginRealmBankNotifications(bankSplit);
                InventoryOperationSnapshot snapshot = CreateOperationSnapshot();
                Task bankPersistence = null;
                try
                {
                    var newItem = new Item(characterId, item.Info, Math.Min(count, item.Info.Entry.MaxStackCount), itemManager: itemManager, gameTableManager: gameTableManager);
                    newItem.ExpirationTimeLeft = item.ExpirationTimeLeft;
                    if (item.Soulbound)
                        newItem.MakeSoulbound();

                    AddItem(newItem, newItemLocation.Location, newItemLocation.BagIndex, persistRealmBank: false);
                    item.StackCount -= newItem.StackCount;
                    // Build first so charge/socket initialization belongs to the
                    // captured SQL state. Preserve Add-before-Stack packet order.
                    ServerItemAdd added = (!player?.IsLoading ?? false) ? new ServerItemAdd
                    {
                        InventoryItem = new InventoryItem
                        {
                            Item = newItem.Build(),
                            Reason = ItemUpdateReason.NoReason
                        }
                    } : null;
                    if (bankSplit) bankPersistence = PersistRealmBankSplit(item, newItem);
                    if (added != null) EnqueueItemMessage(added);
                    SendItemStackCountUpdate(item, item.StackCount, ItemUpdateReason.NoReason);
                    notifications?.Complete();
                }
                catch (Exception error)
                {
                    if (bankPersistence != null) rewardRequiresReload = true;
                    else RestoreOperationSnapshot(snapshot);
                    log.Error(error, "Failed to split item {0}.", itemGuid);
                    player.Session.EnqueueMessageEncrypted(new ServerItemError { ErrorCode = GenericError.DbFailure });
                }
            }
        }

        /// <summary>
        /// Delete <see cref="IItem"/> at supplied <see cref="ItemLocation"/>, this is called directly from a packet hander.
        /// </summary>
        public IItem ItemDelete(ItemLocation from, ItemUpdateReason reason = ItemUpdateReason.Loot)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IBag srcBag = GetBag(from.Location);
                if (srcBag == null)
                    throw new InvalidPacketValueException();

                IItem srcItem = srcBag.GetItem(from.BagIndex);
                if (srcItem == null)
                    throw new InvalidPacketValueException();

                return ItemDelete(srcBag, srcItem, reason);
            }
        }

        /// <summary>
        /// Delete a supplied amount of <see cref="IItem"/> at supplied <see cref="ItemLocation"/>.
        /// </summary>
        public IItem ItemDelete(ItemLocation from, uint count, ItemUpdateReason reason = ItemUpdateReason.Loot)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IBag srcBag = GetBag(from.Location);
                if (srcBag == null)
                    throw new InvalidPacketValueException();

                IItem srcItem = srcBag.GetItem(from.BagIndex);
                if (srcItem == null)
                    throw new InvalidPacketValueException();

                if (IsBankSavePending(srcItem) || count == 0u || count > srcItem.StackCount)
                    throw new InvalidPacketValueException();

                if (count == srcItem.StackCount)
                    return ItemDelete(srcBag, srcItem, reason);

                IItem splitItem = new Item(characterId, srcItem.Info, count, srcItem.Charges, itemManager, gameTableManager)
                {
                    Durability          = srcItem.Durability,
                    ExpirationTimeLeft  = srcItem.ExpirationTimeLeft
                };
                if (srcItem.Soulbound)
                    splitItem.MakeSoulbound();

                ItemStackCountUpdate(srcItem, srcItem.StackCount - count, reason);
                return splitItem;
            }
        }

        private IItem ItemDelete(IBag bag, IItem item, ItemUpdateReason reason, bool notify = true, bool persistRealmBank = true)
        {
            if (IsBankSavePending(item))
                throw new InvalidPacketValueException();
            GenericError? bagCapacityError = CanRemoveItem(item);
            if (bagCapacityError.HasValue)
            {
                EnqueueItemMessage(new ServerItemError
                {
                    ErrorCode = bagCapacityError.Value
                });
                return null;
            }

            using var notifications = BeginRealmBankNotifications(persistRealmBank && item.Location == InventoryLocation.RealmBank);
            InventoryLocation deletedLocation = item.Location;
            RemoveItem(item);
            item.EnqueueDelete(true);
            if (!deletedItems.Any(deleted => ReferenceEquals(deleted, item)))
                deletedItems.Add(item);
            Task bankPersistence = deletedLocation == InventoryLocation.RealmBank && persistRealmBank ? PersistRealmBankDeletion(item) : null;
            realmBankNotifications?.Stage(item);

            try
            {
                if (notify && player != null) EnqueueItemMessage(new ServerItemDelete
                {
                    Guid   = item.Guid,
                    Reason = reason
                });
                notifications?.Complete();
            }
            catch
            {
                if (bankPersistence != null) rewardRequiresReload = true;
                throw;
            }

            return item;
        }

        /// <summary>
        /// Delete a supplied amount of an <see cref="IItem"/>.
        /// </summary>
        public void ItemDelete(uint itemId, uint count = 1u, ItemUpdateReason reason = ItemUpdateReason.Loot)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IBag bag = GetBag(InventoryLocation.Inventory);
                foreach (IItem item in bag.Where(i => i.Id == itemId))
                {
                    if (item.StackCount > count)
                    {
                        ItemStackCountUpdate(item, item.StackCount - count);
                        count = 0;
                    }
                    else
                    {
                        ItemDelete(bag, item, reason);
                        count -= item.StackCount;
                    }

                    if (count == 0)
                        break;
                }
            }
        }

        /// <summary>
        /// Remove <see cref="IItem"/> from this player's inventory without deleting the item from the DB
        /// </summary>
        public void ItemRemove(IItem item, ItemUpdateReason reason = ItemUpdateReason.NoReason)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                if (item == null)
                    throw new ArgumentNullException();

                IBag srcBag = GetBag(item.Location);
                if (srcBag == null)
                    throw new ArgumentException();

                RemoveItem(item);
                item.CharacterId = null;

                EnqueueItemMessage(new ServerItemDelete
                {
                    Guid   = item.Guid,
                    Reason = reason
                });
            }
        }

        /// <summary>
        /// Add <see cref="IItem"/> in the first available bag index for the given <see cref="InventoryLocation"/> .
        /// </summary>
        public void AddItem(IItem item, InventoryLocation location, ItemUpdateReason reason = ItemUpdateReason.NoReason)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                IBag bag = GetBag(location);
                if (bag == null)
                    throw new ArgumentException();

                uint? bagIndex = location == InventoryLocation.Equipped ? bag.GetFirstAvailableBagIndex((ItemSlot)item.Info.SlotEntry.Id) : bag.GetFirstAvailableBagIndex();
                if (!bagIndex.HasValue)
                    throw new ArgumentException();

                using var notifications = BeginRealmBankNotifications(location == InventoryLocation.RealmBank);
                // Stacks are bought back in full, so no need to worry about splitting stacks
                AddItem(item, location, bagIndex.Value);

                if (!player?.IsLoading ?? false)
                {
                    EnqueueItemMessage(new ServerItemAdd
                    {
                        InventoryItem = new InventoryItem
                        {
                            Item   = item.Build(),
                            Reason = reason
                        }
                    });
                }
                notifications?.Complete();
            }
        }

        /// <inheritdoc />
        public void LoadItem(IItem item, InventoryLocation location, uint bagIndex)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                AddItem(item, location, bagIndex, persistRealmBank: false);
            }
        }

        /// <summary>
        /// Add <see cref="IItem"/> to the supplied <see cref="InventoryLocation"/> and bag index.
        /// </summary>
        private void AddItem(IItem item, InventoryLocation location, uint bagIndex, bool persistRealmBank = true)
        {
            if (item == null)
                throw new ArgumentNullException();
            if (item.Location != InventoryLocation.None)
                throw new ArgumentException();

            IBag bag = GetBag(location);
            if (bag == null)
                throw new ArgumentException();

            bag.AddItem(item, bagIndex);
            // Vendor buyback restores the same object, including sales whose SQL
            // deletion is still pending. A former tombstone must not delete it later.
            item.EnqueueDelete(false);
            deletedItems.RemoveAll(deleted => ReferenceEquals(deleted, item));
            BindOnEquip(item);

            if (location == InventoryLocation.Equipped && player != null)
                ApplyProperties(item);

            if (IsVisualItemSlot(item.Location, item.BagIndex) && player != null)
            {
                ItemSlot slot = (ItemSlot)item.Info.TypeEntry.ItemSlotId; ushort display = item.Info.GetDisplayId();
                ApplyInventoryEffect(() => player.AddVisual(slot, display));
            }

            if (IsEquippableBagSlot(item.Location, item.BagIndex))
                InventoryResize(InventoryLocation.Inventory, (int)item.Info.Entry.MaxStackCount);
            if (IsEquippableBankBagSlot(item.Location, item.BagIndex))
                InventoryResize(InventoryLocation.PlayerBank, (int)item.Info.Entry.MaxStackCount);

            if (persistRealmBank && location == InventoryLocation.RealmBank && player != null)
                PersistRealmBankStorage(item);
            realmBankNotifications?.Stage(item, item is Item created && created.PendingCreate ? created : null);
        }

        private static void BindOnEquip(IItem item)
        {
            if (item.Location != InventoryLocation.Equipped || item.Soulbound || item.Info?.CanBindOnEquip() != true)
                return;

            item.MakeSoulbound();
        }

        private void SendBindOnEquipRefreshIfNeeded(IItem item, bool wasSoulbound)
        {
            if (wasSoulbound || item?.Soulbound != true || item.Location == InventoryLocation.None || player?.IsLoading != false)
                return;

            EnqueueItemMessage(new ServerItemAdd
            {
                InventoryItem = new InventoryItem
                {
                    Item   = item.Build(),
                    Reason = ItemUpdateReason.NoReason
                }
            });
        }

        /// <summary>
        /// Remove <see cref="IItem"/> from it's current <see cref="InventoryLocation"/> and bag slot.
        /// </summary>
        private void RemoveItem(IItem item)
        {
            if (item == null)
                throw new ArgumentNullException();
            if (item.Location == InventoryLocation.None)
                throw new ArgumentException();

            IBag bag = GetBag(item.Location);
            if (bag == null)
                throw new ArgumentException();

            if (item.Location == InventoryLocation.Equipped && player != null)
                RemoveProperties(item);

            if (IsEquippableBagSlot(item.Location, item.BagIndex) && item.Info.IsEquippableBag())
                InventoryResize(InventoryLocation.Inventory, (int)-item.Info.Entry.MaxStackCount);
            if (IsEquippableBankBagSlot(item.Location, item.BagIndex) && item.Info.IsEquippableBag())
                InventoryResize(InventoryLocation.PlayerBank, (int)-item.Info.Entry.MaxStackCount);

            bag.RemoveItem(item);

            if (IsVisualItemSlot(item.PreviousLocation, item.PreviousBagIndex) && player != null)
            {
                ItemSlot slot = (ItemSlot)item.Info.TypeEntry.ItemSlotId;
                ApplyInventoryEffect(() => player.RemoveVisual(slot));
            }
        }

        /// <summary>
        /// Resize <see cref="InventoryLocation"/> with supplied capacity change.
        /// </summary>
        private void InventoryResize(InventoryLocation location, int capacityChange)
        {
            if (bags.Values.Any(bag => bag.HasReservedSlots))
                throw new InvalidOperationException("Cannot resize inventory during a pending delivery.");
            IBag bag = GetBag(location);
            if (bag == null)
                throw new ArgumentException();

            if (capacityChange < 0)
            {
                int newSlotCount = checked((int)bag.Slots + capacityChange);
                if (newSlotCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(capacityChange));

                for (uint bagIndex = (uint)newSlotCount; bagIndex < bag.Slots; bagIndex++)
                {
                    IItem item = bag.GetItem(bagIndex);
                    if (item == null)
                        continue;

                    uint? newBagIndex = GetFirstAvailableBagIndex(bag, (uint)newSlotCount);
                    if (!newBagIndex.HasValue)
                        throw new InvalidOperationException();

                    ItemMove(item, location, newBagIndex.Value);
                    //bag.MoveItem(item, newBagIndex.Value);
                }
            }

            bag.Resize(capacityChange);
        }

        private void UpdateItemExpirations(double lastTick)
        {
            if (lastTick <= 0d)
                return;

            itemExpirationAccumulator += lastTick;
            if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || realmBankNotifications != null || HasPendingRealmBankItemSave
                || auctionDelivery?.DefersExpiration == true || commodityDelivery?.DefersExpiration == true)
                return;
            if (itemExpirationAccumulator < 1d)
                return;

            uint elapsedSeconds = (uint)Math.Min(uint.MaxValue, Math.Floor(itemExpirationAccumulator));
            bool bankExpiry = bags.Values.Where(bag => bag.Location == InventoryLocation.RealmBank)
                .SelectMany(bag => bag).Any(item => item.ExpirationTimeLeft > 0 && item.ExpirationTimeLeft <= elapsedSeconds);
            if (bankExpiry && (inventorySaveLeases != 0 || HasPendingRewardDelivery
                || bags.Values.SelectMany(bag => bag).OfType<Item>().Any(item => item.HasPendingSave))) return;
            using var notifications = BeginRealmBankNotifications(bankExpiry);
            itemExpirationAccumulator -= elapsedSeconds;
            notifications?.RetainExpirationOnAbort(elapsedSeconds);

            foreach (IItem item in bags.Values
                .Where(b => b.Location != InventoryLocation.Ability)
                .SelectMany(b => b)
                .Where(i => i.ExpirationTimeLeft > 0u)
                .ToList())
            {
                if (item.Location == InventoryLocation.None)
                    continue;

                if (item is Item reserved && reserved.HasRewardMutationReservation)
                {
                    DeferRewardExpiration(item, elapsedSeconds);
                    continue;
                }

                if (item.ExpirationTimeLeft <= elapsedSeconds)
                {
                    IBag bag = GetBag(item.Location);
                    if (bag != null)
                        ItemDelete(bag, item, ItemUpdateReason.Expired, !rewardOwnerInvalidated);
                }
                else
                {
                    item.ExpirationTimeLeft -= elapsedSeconds;
                    realmBankNotifications?.Stage(item);
                }
            }
            notifications?.Complete();
        }

        private static uint? GetFirstAvailableBagIndex(IBag bag, uint maxExclusive)
        {
            for (uint bagIndex = 0u; bagIndex < bag.Slots && bagIndex < maxExclusive; bagIndex++)
                if (bag.GetItem(bagIndex) == null && !bag.IsSlotReserved(bagIndex))
                    return bagIndex;

            return null;
        }

        private GenericError? CanRemoveItem(IItem item)
        {
            if (item.Info?.IsEquippableBag() != true)
                return null;

            InventoryLocation? expandedLocation = GetExpandedBagLocation(item.Location, item.BagIndex);
            if (!expandedLocation.HasValue)
                return null;

            return CanApplyBagCapacityChange(expandedLocation.Value, -(int)item.Info.Entry.MaxStackCount)
                ? null
                : GenericError.ItemBagMustBeEmpty;
        }

        private GenericError? CanApplyBagCapacityChangeForMove(IItem item, IItem dstItem, InventoryLocation location, uint bagIndex)
        {
            InventoryLocation? sourceExpandedLocation = item.Info?.IsEquippableBag() == true
                ? GetExpandedBagLocation(item.Location, item.BagIndex)
                : null;
            InventoryLocation? destinationExpandedLocation = item.Info?.IsEquippableBag() == true
                ? GetExpandedBagLocation(location, bagIndex)
                : null;

            if (sourceExpandedLocation.HasValue && sourceExpandedLocation != destinationExpandedLocation)
            {
                int capacityChange = -(int)item.Info.Entry.MaxStackCount;
                if (dstItem?.Info?.IsEquippableBag() == true && location == sourceExpandedLocation.Value)
                    capacityChange += (int)dstItem.Info.Entry.MaxStackCount;

                uint? reservedBagIndex = dstItem == null && location == sourceExpandedLocation.Value ? bagIndex : null;
                if (!CanApplyBagCapacityChange(sourceExpandedLocation.Value, capacityChange, reservedBagIndex))
                    return GenericError.ItemBagMustBeEmpty;
            }

            if (destinationExpandedLocation.HasValue && destinationExpandedLocation != sourceExpandedLocation)
            {
                int capacityChange = (int)item.Info.Entry.MaxStackCount;
                if (dstItem?.Info?.IsEquippableBag() == true)
                    capacityChange -= (int)dstItem.Info.Entry.MaxStackCount;

                if (!CanApplyBagCapacityChange(destinationExpandedLocation.Value, capacityChange))
                    return GenericError.ItemBagMustBeEmpty;
            }

            return null;
        }

        private GenericError? EnsureRealmBankAccessForMove(IItem item, InventoryLocation location, uint bagIndex)
        {
            if (item.Location != InventoryLocation.RealmBank && location != InventoryLocation.RealmBank)
                return null;

            if (IsRealmBankPersistenceBlocked || inventorySaveLeases != 0 || HasPendingRewardDelivery)
                return GenericError.ItemNotValidForSlot;

            if (player == null || realmBankManager == null || !realmBankManager.HasUnlock(player))
                return GenericError.ItemNotValidForSlot;

            if (item is Item source && source.HasPendingSave)
                return GenericError.ItemNotValidForSlot;
            IBag destination = GetBag(location);
            IItem destinationItem = bagIndex < (destination?.Slots ?? 0) ? destination.GetItem(bagIndex) : null;
            if (destinationItem is Item target && target.HasPendingSave)
                return GenericError.ItemNotValidForSlot;

            uint capacity = realmBankManager.GetSlotCapacity(player);
            IBag realmBank = GetBag(InventoryLocation.RealmBank);
            if (realmBank == null)
                return GenericError.ItemNotValidForSlot;

            if (realmBank.Slots < capacity)
                realmBank.Resize((int)(capacity - realmBank.Slots));

            if (location == InventoryLocation.RealmBank && bagIndex >= capacity)
                return GenericError.ItemNotValidForSlot;

            return null;
        }

        private static bool IsBankSavePending(IItem item) =>
            item?.Location == InventoryLocation.RealmBank && item is Item concrete && concrete.HasPendingSave;

        private Task PersistRealmBankStorage(IItem item)
        {
            if (item == null || item.Location == InventoryLocation.None || player == null || realmBankManager == null)
                return null;

            if (realmBankNotifications != null && (item.Location == InventoryLocation.RealmBank || item.PreviousLocation == InventoryLocation.RealmBank))
            {
                realmBankNotifications.Stage(item); return null;
            }

            Task save = item.Location == InventoryLocation.RealmBank ? realmBankManager.SaveItemAsync(item, player)
                : item.PreviousLocation == InventoryLocation.RealmBank ? realmBankManager.SaveCharacterItemAsync(item, player) : null;
            return save == null ? null : ObserveRealmBankPersistence(save, item, item);
        }

        private bool CanApplyBagCapacityChange(InventoryLocation location, int capacityChange, uint? reservedBagIndex = null)
        {
            if (capacityChange >= 0)
                return true;

            IBag bag = GetBag(location);
            if (bag == null)
                return false;

            int newSlotCount = checked((int)bag.Slots + capacityChange);
            if (newSlotCount < 0)
                return false;

            uint emptyRetainedSlots = 0u;
            uint requiredRetainedSlots = 0u;
            for (uint bagIndex = 0u; bagIndex < bag.Slots; bagIndex++)
            {
                IItem item = bag.GetItem(bagIndex);
                if (bagIndex < newSlotCount)
                {
                    if (item == null)
                        emptyRetainedSlots++;
                }
                else if (item != null)
                    requiredRetainedSlots++;
            }

            if (reservedBagIndex.HasValue)
            {
                if (reservedBagIndex.Value < newSlotCount)
                {
                    if (bag.GetItem(reservedBagIndex.Value) == null)
                    {
                        if (emptyRetainedSlots == 0u)
                            return false;

                        emptyRetainedSlots--;
                    }
                }
                else
                    requiredRetainedSlots++;
            }

            return emptyRetainedSlots >= requiredRetainedSlots;
        }

        private InventoryLocation? GetExpandedBagLocation(InventoryLocation location, uint bagIndex)
        {
            if (IsEquippableBagSlot(location, bagIndex))
                return InventoryLocation.Inventory;
            if (IsEquippableBankBagSlot(location, bagIndex))
                return InventoryLocation.PlayerBank;

            return null;
        }

        private static uint GetInitialExpirationTimeLeft(IItemInfo info)
        {
            if (info?.Entry.ExpirationTimeMinutes is null or 0u)
                return 0u;

            ulong seconds = (ulong)info.Entry.ExpirationTimeMinutes * 60ul;
            return seconds > uint.MaxValue ? uint.MaxValue : (uint)seconds;
        }

        private InventoryOperationSnapshot CreateOperationSnapshot()
        {
            var snapshot = new InventoryOperationSnapshot
            {
                DeletedItemCount = deletedItems.Count
            };

            foreach ((InventoryLocation location, IBag bag) in bags)
            {
                IItem[] bagItems = bag.CreateSnapshot();
                snapshot.Bags.Add(location, bagItems);

                foreach (IItem item in bagItems)
                {
                    if (item != null)
                        snapshot.Items.TryAdd(item.Guid, new ItemOperationSnapshot(item));
                }
            }

            return snapshot;
        }

        private void RestoreOperationSnapshot(InventoryOperationSnapshot snapshot)
        {
            // Reject before restoring any Item scalar, not just the later bag array.
            if (bags.Values.Any(bag => bag.HasReservedSlots))
                throw new InvalidOperationException("Cannot restore inventory over a pending delivery.");
            foreach (ItemOperationSnapshot itemSnapshot in snapshot.Items.Values)
                itemSnapshot.Restore();

            foreach ((InventoryLocation location, IItem[] bagSnapshot) in snapshot.Bags)
            {
                IBag bag = GetBag(location);
                bag?.RestoreSnapshot(bagSnapshot);
            }

            if (deletedItems.Count > snapshot.DeletedItemCount)
                deletedItems.RemoveRange(snapshot.DeletedItemCount, deletedItems.Count - snapshot.DeletedItemCount);
        }

        private void RestoreBankPreparation(InventoryOperationSnapshot before, InventoryOperationSnapshot prepared)
        {
            if (bags.Count != prepared.Bags.Count || bags.Any(pair => pair.Value.HasReservedSlots
                || !prepared.Bags.TryGetValue(pair.Key, out var items) || !pair.Value.CreateSnapshot().SequenceEqual(items)))
                throw new InvalidOperationException("Bank rollback cannot discard later inventory membership.");
            var restore = before.Items.Select(pair => pair.Value.PrepareRestore(prepared.Items.GetValueOrDefault(pair.Key)
                ?? throw new InvalidOperationException("Bank rollback lost its prepared Item."))).ToArray();
            foreach (var apply in restore) apply();
            foreach (var pair in before.Bags) GetBag(pair.Key).RestoreSnapshot(pair.Value);
            if (deletedItems.Count > before.DeletedItemCount) deletedItems.RemoveRange(before.DeletedItemCount, deletedItems.Count - before.DeletedItemCount);
        }

        /// <summary>
        /// Update <see cref="IItem"/> with supplied stack count.
        /// </summary>
        private void ItemStackCountUpdate(IItem item, uint stackCount, ItemUpdateReason reason = ItemUpdateReason.NoReason, bool persistRealmBank = true)
        {
            if (item == null)
                throw new ArgumentNullException();

            using var notifications = BeginRealmBankNotifications(persistRealmBank && item.Location == InventoryLocation.RealmBank);

            uint previousStackCount = item.StackCount;
            item.StackCount = stackCount;

            if (item.Location == InventoryLocation.Inventory
                && stackCount > previousStackCount
                && player != null
                && !player.IsLoading)
            {
                RefreshCollectedItem(item.Id);
            }

            SendItemStackCountUpdate(item, stackCount, reason);

            if (persistRealmBank && item.Location == InventoryLocation.RealmBank && player != null)
                PersistRealmBankStorage(item);
            realmBankNotifications?.Stage(item);
            notifications?.Complete();
        }

        private void SendItemStackCountUpdate(IItem item, uint stackCount, ItemUpdateReason reason)
        {
            EnqueueItemMessage(new ServerItemStackCountUpdate
            {
                Guid = item.Guid, StackCount = stackCount, Reason = reason
            });
        }

        internal static bool WillRemoveOnUse(IItem item)
        {
            var entry = item.Info.Entry;
            return entry.MaxStackCount > 1 ? item.StackCount == 1
                : entry.MaxStackCount == 1 && entry.MaxCharges > 0 && item.Charges == 1;
        }

        /// <summary>
        /// Apply stack updates and deletion to <see cref="IItem"/> on use
        /// </summary>
        public bool ItemUse(IItem item)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated || (item is Item reserved && reserved.HasRewardMutationReservation)) return false;
                if (item == null)
                    throw new ArgumentNullException(nameof(item), "Item is null.");

                IBag bag = GetBag(item.Location);
                if (bag == null || item.BagIndex >= bag.Slots || !ReferenceEquals(bag.GetItem(item.BagIndex), item))
                    return false;

                if (IsBankSavePending(item))
                    return false;

                // This should only apply for re-usable items, like Quest Clickies.
                if (item.Info.Entry.MaxCharges == 0 && item.Info.Entry.MaxStackCount == 1)
                    return true;

                using var notifications = BeginRealmBankNotifications(item.Location == InventoryLocation.RealmBank);

                if (item.Info.Entry.MaxStackCount > 1)
                {
                    if (item.StackCount <= 0)
                        return false;

                    if (item.Info.Entry.MaxCharges > 0 && item.Charges == 0)
                        item.Charges = item.Info.Entry.MaxCharges;
                }
                else if (item.Info.Entry.MaxCharges > 0 && item.Charges <= 0)
                {
                    return false;
                }

                uint consumedId = item.Id;
                AfterBankCommit(() => player.AchievementManager.CheckAchievements(player, AchievementType.ItemConsume, consumedId));

                bool remove = WillRemoveOnUse(item);
                if (item.Charges >= 1 && item.Info.Entry.MaxStackCount == 1)
                    item.Charges--;

                if (item.Info.Entry.MaxStackCount > 1 && item.StackCount > 0)
                    ItemStackCountUpdate(item, item.StackCount - 1, persistRealmBank: false);

                if (remove)
                {
                    ItemDelete(new ItemLocation
                    {
                        Location = item.Location,
                        BagIndex = item.BagIndex
                    }, ItemUpdateReason.ConsumeCharge);
                }

                if (item.Location == InventoryLocation.RealmBank && player != null)
                    PersistRealmBankStorage(item);

                notifications?.Complete();
                return true;
            }
        }

        public void ItemMoveToSupplySatchel(IItem item, uint amount)
        {
            if (player is Player owner)
            {
                SettleSatchelDeposit(owner, item, amount);
                return;
            }

            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || rewardOwnerInvalidated) throw new InvalidOperationException("Inventory requires a current authoritative owner.");
                if (item == null)
                    throw new ArgumentNullException();

                if (IsBankSavePending(item) || amount == 0u || amount > item.StackCount)
                    throw new InvalidPacketValueException();

                if (player.SupplySatchelManager.IsFull(item))
                    return;

                uint amountRemaining = player.SupplySatchelManager.AddAmount(item, amount);
                if (amountRemaining > 0)
                    ItemStackCountUpdate(item, item.StackCount - amount + amountRemaining);
                else if (amount < item.StackCount)
                    ItemStackCountUpdate(item, item.StackCount - amount);
                else
                    ItemDelete(new ItemLocation
                        {
                            Location = item.Location,
                            BagIndex = item.BagIndex
                        }, ItemUpdateReason.MaterialBagConversion);
            }
        }

        private IBag GetBag(InventoryLocation location)
        {
            return bags.TryGetValue(location, out IBag container) ? container : null;
        }

        private void ApplyProperties(IItem item)
        {
            if (realmBankNotifications != null)
            {
                var properties = item.Info.Properties.ToArray(); ItemSlot slot = (ItemSlot)item.Info.SlotEntry.Id;
                ApplyInventoryEffect(() => { foreach (var property in properties) player.AddItemProperty(property.Key, slot, property.Value); });
                return;
            }
            foreach (KeyValuePair<Property, float> property in item.Info.Properties)
                player.AddItemProperty(property.Key, (ItemSlot)item.Info.SlotEntry.Id, property.Value);
        }

        private void RemoveProperties(IItem item)
        {
            if (realmBankNotifications != null)
            {
                var properties = item.Info.Properties.Keys.ToArray(); ItemSlot slot = (ItemSlot)item.Info.SlotEntry.Id;
                ApplyInventoryEffect(() => { foreach (Property property in properties) player.RemoveItemProperty(property, slot); });
                return;
            }
            foreach (KeyValuePair<Property, float> property in item.Info.Properties)
                player.RemoveItemProperty(property.Key, (ItemSlot)item.Info.SlotEntry.Id);
        }

        private sealed class InventoryOperationSnapshot
        {
            public int DeletedItemCount { get; init; }
            public Dictionary<InventoryLocation, IItem[]> Bags { get; } = new();
            public Dictionary<ulong, ItemOperationSnapshot> Items { get; } = new();
        }

        private sealed class ItemOperationSnapshot
        {
            private readonly IItem item;
            private readonly ulong? characterId;
            private readonly InventoryLocation location;
            private readonly InventoryLocation previousLocation;
            private readonly uint bagIndex;
            private readonly uint previousBagIndex;
            private readonly uint stackCount;
            private readonly uint charges;
            private readonly float durability;
            private readonly uint expirationTimeLeft;
            private readonly bool pendingDelete;
            private readonly bool soulbound;
            private readonly uint[] microchips;
            private readonly ItemRuneSlot[] runes;

            public ItemOperationSnapshot(IItem item)
            {
                this.item = item;
                characterId = item.CharacterId;
                location = item.Location;
                previousLocation = item.PreviousLocation;
                bagIndex = item.BagIndex;
                previousBagIndex = item.PreviousBagIndex;
                stackCount = item.StackCount;
                charges = item.Charges;
                durability = item.Durability;
                expirationTimeLeft = item.ExpirationTimeLeft;
                pendingDelete = item.PendingDelete;
                soulbound = item.Soulbound; microchips = item.MicrochipIds?.ToArray() ?? [];
                runes = item.RuneSlots?.Select(slot => new ItemRuneSlot(slot.Type, slot.RuneItem2Id)).ToArray() ?? [];
            }

            public void Restore()
            {
                item.CharacterId = characterId;
                item.Location = location;
                item.PreviousLocation = previousLocation;
                item.BagIndex = bagIndex;
                item.PreviousBagIndex = previousBagIndex;
                item.EnqueueDelete(pendingDelete);

                if (item.Info == null)
                    return;

                item.StackCount = stackCount;
                item.Charges = charges;
                item.Durability = durability;
                item.ExpirationTimeLeft = expirationTimeLeft;
                if (item is Item concrete) concrete.RestoreBankPreparationMetadata(soulbound, microchips, runes);
            }

            internal Action PrepareRestore(ItemOperationSnapshot prepared)
            {
                if (!ReferenceEquals(item, prepared.item)) throw new InvalidOperationException("Bank rollback changed its Item reference.");
                var changes = new List<Action>();
                void Field<T>(T before, T target, Func<T> current, Action<T> set)
                {
                    if (EqualityComparer<T>.Default.Equals(before, target)) return;
                    if (!EqualityComparer<T>.Default.Equals(current(), target)) throw new InvalidOperationException("Bank rollback conflicts with later Item intent.");
                    changes.Add(() => set(before));
                }
                Field(characterId, prepared.characterId, () => item.CharacterId, value => item.CharacterId = value);
                Field(location, prepared.location, () => item.Location, value => item.Location = value);
                Field(previousLocation, prepared.previousLocation, () => item.PreviousLocation, value => item.PreviousLocation = value);
                Field(bagIndex, prepared.bagIndex, () => item.BagIndex, value => item.BagIndex = value);
                Field(previousBagIndex, prepared.previousBagIndex, () => item.PreviousBagIndex, value => item.PreviousBagIndex = value);
                Field(pendingDelete, prepared.pendingDelete, () => item.PendingDelete, item.EnqueueDelete);
                if (item.Info != null)
                {
                    Field(stackCount, prepared.stackCount, () => item.StackCount, value => item.StackCount = value);
                    Field(charges, prepared.charges, () => item.Charges, value => item.Charges = value);
                    Field(durability, prepared.durability, () => item.Durability, value => item.Durability = value);
                    Field(expirationTimeLeft, prepared.expirationTimeLeft, () => item.ExpirationTimeLeft, value => item.ExpirationTimeLeft = value);
                    static bool SameRunes(IEnumerable<ItemRuneSlot> left, IEnumerable<ItemRuneSlot> right)
                        => left.Select(slot => (slot.Type, slot.RuneItem2Id)).SequenceEqual(right.Select(slot => (slot.Type, slot.RuneItem2Id)));
                    bool chipsChanged = !microchips.SequenceEqual(prepared.microchips), runesChanged = !SameRunes(runes, prepared.runes), boundChanged = soulbound != prepared.soulbound;
                    if (chipsChanged && !prepared.microchips.SequenceEqual(item.MicrochipIds) || runesChanged && !SameRunes(prepared.runes, item.RuneSlots)
                        || boundChanged && item.Soulbound != prepared.soulbound) throw new InvalidOperationException("Bank rollback conflicts with later Item metadata.");
                    if ((chipsChanged || runesChanged || boundChanged) && item is Item concrete)
                    {
                        bool bound = boundChanged ? soulbound : item.Soulbound;
                        var chips = chipsChanged ? microchips : item.MicrochipIds.ToArray();
                        var slots = runesChanged ? runes : item.RuneSlots.Select(slot => new ItemRuneSlot(slot.Type, slot.RuneItem2Id)).ToArray();
                        changes.Add(() => concrete.RestoreBankPreparationMetadata(bound, chips, slots));
                    }
                }
                return () => { foreach (var change in changes) change(); };
            }
        }

        public IEnumerator<IBag> GetEnumerator()
        {
            return bags.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
