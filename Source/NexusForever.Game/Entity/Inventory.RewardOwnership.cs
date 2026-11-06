using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        // Lazy initialization also supports the existing uninitialized test adapters.
        private object inventoryMutationGate;
        private object MutationGate => LazyInitializer.EnsureInitialized(ref inventoryMutationGate);
        private HashSet<RewardDeliveryReservation> rewardDeliveries;
        private HashSet<RewardDeliveryReservation> RewardDeliveries => rewardDeliveries ??= [];
        private Dictionary<IItem, ulong> deferredRewardExpirations;
        private int inventorySaveLeases;
        private int inventoryMutationLeases;
        private int inventoryMutationDepth;
        private long rewardPublicationGeneration;
        private int rewardNotificationDepth;
        private volatile bool rewardOwnerInvalidated;
        private volatile bool rewardRequiresReload;

        private readonly struct InventoryMutationOperation : IDisposable
        {
            private readonly Inventory owner;
            public InventoryMutationOperation(Inventory owner, bool readOnly = false)
            {
                if (owner.lootPreparation != null && owner.inventoryMutationDepth != 0 && !readOnly)
                    throw new InvalidOperationException("Physical loot preparation cannot reenter inventory mutation.");
                if ((owner.realmBankPublishing || owner.IsMarketplacePublishing) && !readOnly) throw new InvalidOperationException("Bank publication cannot reenter inventory mutation.");
                this.owner = owner; ++owner.inventoryMutationDepth;
            }
            public void Dispose() => --owner.inventoryMutationDepth;
        }

        public bool SupportsTransactionalSoulboundDelivery => true;
        public bool OwnsRewardReservation(IInventoryRewardReservation reservation)
        {
            lock (MutationGate)
                return reservation is RewardDeliveryReservation held && RewardDeliveries.Contains(held);
        }
        public bool RequiresAuthoritativeReload => rewardRequiresReload || RealmBankPersistenceFailed;
        public bool IsSaveBlocked
        {
            get
            {
                lock (MutationGate)
                    return RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked || realmBankNotifications != null || HasPendingRealmBankItemSave || HasPendingRewardDelivery || inventorySaveLeases != 0
                        || inventoryMutationLeases != 0 || rewardNotificationDepth != 0;
            }
        }
        public bool HasPendingRewardDelivery
        {
            get
            {
                lock (MutationGate)
                    return RewardDeliveries.Count != 0 || bags.Values.Any(bag => bag.HasReservedSlots);
            }
        }

        public bool TryAcquireSaveLease(out IDisposable lease) => TryAcquireOperationLease(true, out lease);
        public bool TryAcquireMutationLease(out IDisposable lease) => TryAcquireOperationLease(false, out lease);

        private bool TryAcquireOperationLease(bool save, out IDisposable lease)
        {
            lock (MutationGate)
            {
                lease = null;
                if (RequiresAuthoritativeReload || (IsRealmBankPersistenceBlocked || HasPendingRealmBankItemSave) && !(save && BankReconciliationSaveAllowed) || realmBankNotifications != null || HasPendingRewardDelivery || inventorySaveLeases != 0 || inventoryMutationLeases != 0 || rewardNotificationDepth != 0
                    || (!save && rewardOwnerInvalidated))
                    return false;
                if (save) ++inventorySaveLeases; else ++inventoryMutationLeases;
                lease = new InventoryOperationLease(this, save);
                return true;
            }
        }

        private sealed class InventoryOperationLease(Inventory owner, bool save) : IDisposable
        {
            private bool released;
            public void Dispose()
            {
                lock (owner.MutationGate)
                {
                    if (released) return;
                    released = true;
                    if (save) --owner.inventorySaveLeases; else --owner.inventoryMutationLeases;
                }
            }
        }

        public void InvalidateRewardDeliveries()
        {
            lock (MutationGate)
            {
                rewardOwnerInvalidated = true;
                if (bankPublications != null)
                    foreach (var bank in bankPublications.ToArray()) bank.Abandon();
                foreach (var delivery in RewardDeliveries.ToArray()) delivery.OnOwnerInvalidated();
            }
        }

        private void DeferRewardExpiration(IItem item, uint elapsed)
        {
            deferredRewardExpirations ??= new(ReferenceEqualityComparer.Instance);
            ulong before = deferredRewardExpirations.GetValueOrDefault(item);
            deferredRewardExpirations[item] = before > ulong.MaxValue - elapsed ? ulong.MaxValue : before + elapsed;
        }

        private ServerItemDelete ApplyDeferredRewardExpiration(IItem item)
        {
            if (deferredRewardExpirations == null || !deferredRewardExpirations.Remove(item, out ulong elapsed)
                || elapsed == 0 || item.ExpirationTimeLeft == 0 || item.Location == InventoryLocation.None)
                return null;
            if (elapsed < item.ExpirationTimeLeft)
            {
                item.ExpirationTimeLeft -= (uint)elapsed;
                return null;
            }
            IBag bag = GetBag(item.Location);
            if (bag == null) throw new InvalidOperationException("Deferred expiration lost its owning bag.");
            if (ItemDelete(bag, item, NexusForever.Network.World.Message.Static.ItemUpdateReason.Expired, false) == null)
                throw new InvalidOperationException("Deferred expiration could not remove its expired Item.");
            return new ServerItemDelete { Guid = item.Guid, Reason = NexusForever.Network.World.Message.Static.ItemUpdateReason.Expired };
        }
    }
}
