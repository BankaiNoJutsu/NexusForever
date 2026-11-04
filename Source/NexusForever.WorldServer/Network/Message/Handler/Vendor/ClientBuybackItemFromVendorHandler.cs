using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    public class ClientBuybackItemFromVendorHandler : IMessageHandler<IWorldSession, ClientBuybackItemFromVendor>
    {
        #region Dependency Injection

        private readonly IBuybackManager buybackManager;

        public ClientBuybackItemFromVendorHandler(
            IBuybackManager buybackManager)
        {
            this.buybackManager = buybackManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientBuybackItemFromVendor buybackItemFromVendor)
        {
            IPlayer player = session.Player;
            IInventory inventory = player?.Inventory;
            if (inventory == null || !InventoryMutationScope.TryAcquire(inventory, out var mutationLease)) return;
            uint id = buybackItemFromVendor.UniqueId;
            BuybackItem offer;
            IGameSession ownerSession;
            EventQueue ownerEvents;
            ICurrencyManager wallet;
            using (mutationLease)
            {
                ownerSession = player.Session;
                ownerEvents = ownerSession?.Events;
                IBuybackItem buybackItem = buybackManager.GetItem(player, id);
                if (buybackItem == null)
                    return;

                if (inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) < 1)
                {
                    if (OwnsOriginalInventory())
                        player.SendGenericError(GenericError.ItemInventoryFull);
                    return;
                }

                wallet = player.CurrencyManager;
                foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in buybackItem.CurrencyChange)
                    if (!wallet.CanAfford(currencyTypeId, currencyAmount))
                        return;
                if (!IsCurrentOwner())
                    return;

                if (inventory is Inventory && wallet is CurrencyManager && buybackItem is BuybackItem captured)
                    offer = captured;
                else
                {
                    foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in buybackItem.CurrencyChange)
                        wallet.CurrencySubtractAmount(currencyTypeId, currencyAmount);

                    inventory.AddItem(buybackItem.Item, InventoryLocation.Inventory);
                    buybackManager.RemoveItem(player, buybackItem, notify: IsCurrentOwner());
                    return;
                }
            }

            ((Inventory)inventory).RedeemVendorBuyback(offer, IsCurrentOwner, () => ReferenceEquals(buybackManager.GetItem(player, id), offer));

            bool OwnsOriginalInventory() => ReferenceEquals(session.Player, player)
                && ReferenceEquals(player.Session, ownerSession) && ReferenceEquals(player.Inventory, inventory)
                && ReferenceEquals(ownerSession?.Events, ownerEvents);

            bool IsCurrentOwner() => OwnsOriginalInventory() && ReferenceEquals(player.CurrencyManager, wallet);
        }
    }
}
