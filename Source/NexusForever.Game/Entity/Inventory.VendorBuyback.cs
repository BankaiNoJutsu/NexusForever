using NexusForever.Database.Character;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public void RedeemVendorBuyback(BuybackItem offer, Func<bool> currentOwner, Func<bool> currentOffer)
        {
            if (!currentOwner()) return;
            // A detached offer may still depend on the preceding sale's source
            // deletion or stack reduction. Save that sale before redemption.
            if (offer.Item is Item item && (item.PendingCreate || item.HasUnsettledDeletion) && player is Player actor)
                actor.Save(() => SettleVendorBuyback(offer, currentOwner, currentOffer));
            else
                SettleVendorBuyback(offer, currentOwner, currentOffer);
        }

        private void SettleVendorBuyback(BuybackItem offer, Func<bool> currentOwner, Func<bool> currentOffer)
        {
            lock (MutationGate)
            {
                if (!TryAcquireMutationLease(out var lease)) return;
                using var held = lease;
                using var mutation = new InventoryMutationOperation(this);
                if (!currentOwner() || !currentOffer() || rewardOwnerInvalidated || RequiresAuthoritativeReload || IsRealmBankPersistenceBlocked
                    || player.CurrencyManager is not CurrencyManager currencies || offer.HasExpired
                    || offer.Item is not Item item || item.Location != InventoryLocation.None
                    || item.CharacterId != characterId || GetItem(item.Guid) != null)
                    return;

                uint? slot = GetBag(InventoryLocation.Inventory)?.GetFirstAvailableBagIndex();
                if (!slot.HasValue) return;
                try
                {
                    using var batch = BeginRealmBankNotifications(true);
                    if (batch == null || !offer.TryReserveRedemption()) return;
                    batch.Attach(new VendorBuybackReservation(offer));
                    batch.RequireActor(currentOwner);
                    batch.RetainPreparation(item);
                    var payment = currencies.CaptureBankCurrency(player, offer.CurrencyChange.Where(cost => cost.CurrencyAmount != 0).Select(cost =>
                        new CurrencyManager.BankCurrencyChange(cost.CurrencyTypeId, cost.CurrencyAmount, Subtract: true)),
                        batch.CurrentActor, batch.After);
                    batch.Attach(payment);
                    batch.Add(payment.Publish);

                    AddItem(item, InventoryLocation.Inventory, slot.Value);
                    if (!player.IsLoading)
                        batch.Add(new ServerItemAdd
                        {
                            InventoryItem = new InventoryItem { Item = item.Build(), Reason = ItemUpdateReason.NoReason }
                        });
                    batch.Add(new ServerBuybackItemRemoved { UniqueId = offer.UniqueId });
                    batch.Complete();
                }
                catch (Exception error)
                {
                    log.Error(error, "Vendor buyback preparation failed.");
                }
            }
        }

        private sealed class VendorBuybackReservation(BuybackItem offer) : IBankSaveCompanion
        {
            public void Stage(CharacterContext context) { }
            public void Adopt() => offer.ConsumeRedemption();
            public void Complete() { }
            public void Abort() => offer.ReleaseRedemption();
            public void Quarantine() => offer.ConsumeRedemption();
        }
    }
}
