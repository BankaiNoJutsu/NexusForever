using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        private static string FingerprintCommodityDatabase(string database) => database == null ? null
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(database)));

        internal void ObserveCommodityRollback(NexusForever.Database.Character.BidReconciliationRolledBackException proof)
        {
            lock (persistenceGate)
            {
                if (uncertainStates == null) return;
                if (activeSave != null || persistenceBaseline == null || realmBankBaseline != null || uncertainBankStates != null
                    || proof.ContextIdentity != uncertainSaveContext || proof.DatabaseFingerprint != uncertainDatabaseFingerprint
                    || proof.CharacterId != CharacterId || !uncertainStates.Any(state => SameState(state, persistenceBaseline)))
                    throw new InvalidOperationException("Commodity rollback does not own this Item's uncertain save.");
                // The exact whole Character receipt proves noncommit. Keep the
                // original baseline and live later intent; no SQL row is adopted.
                uncertainStates = null; uncertainSaveContext = uncertainDatabaseFingerprint = null;
            }
        }

        internal GlobalMarketplaceManager.BidItem CaptureCommodityMergePreimage()
        {
            lock (persistenceGate)
            {
                var model = CaptureAuctionDeliveryPreimage();
                if (saveMask != ItemSaveMask.None || stackCount != model.StackCount || bagIndex != model.BagIndex)
                    throw new InvalidOperationException("Commodity merge requires its clean loaded Item.");
                return GlobalMarketplaceManager.BidItem.Capture(model);
            }
        }

        internal void AdoptCommodityMerge(GlobalMarketplaceManager.BidItem before, GlobalMarketplaceManager.BidItem after,
            RewardMutationReservation reservation)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(rewardMutationReservation, reservation)
                    || GlobalMarketplaceManager.BidItem.Capture(persistenceBaseline) != before
                    || after != (before with { Quantity = after.Quantity }) || after.Quantity <= before.Quantity)
                    throw new InvalidOperationException("Commodity merge lost its exact loaded Item owner.");
                reservation.Dispose();
                stackCount = after.Quantity; persistenceBaseline = after.Model();
                // Enrol an unchanged complete postimage before any packet.
                MarkDirty(ItemSaveMask.StackCount);
            }
        }

        internal void AdoptCommodityPosting(GlobalMarketplaceManager.BidItem before, GlobalMarketplaceManager.BidItem after,
            RewardMutationReservation reservation)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(rewardMutationReservation, reservation) || activeSave != null
                    || GlobalMarketplaceManager.BidItem.Capture(persistenceBaseline) != before
                    || after != null && (after.Quantity == 0 || after != (before with { Quantity = after.Quantity })))
                    throw new InvalidOperationException("Commodity posting lost its exact captured Item owner.");
                reservation.Dispose(); persistenceBaseline = after?.Model(); uncertainStates = null;
                if (after == null)
                {
                    // The old object cannot later recreate its consumed SQL row.
                    requiresAuthoritativeRewardReload = true; location = InventoryLocation.None;
                    saveMask = ItemSaveMask.None;
                }
                else { stackCount = after.Quantity; MarkDirty(ItemSaveMask.StackCount); }
            }
        }
    }
}
