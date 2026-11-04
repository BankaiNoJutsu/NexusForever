using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

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
            using var heldMutation = mutationLease;
            var ownerSession = player.Session;
            IBuybackItem buybackItem = buybackManager.GetItem(player, buybackItemFromVendor.UniqueId);
            if (buybackItem == null)
                return;

            if (inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) < 1)
            {
                if (IsCurrentOwner())
                    player.SendGenericError(GenericError.ItemInventoryFull);
                return;
            }

            ICurrencyManager wallet = player.CurrencyManager;
            foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in buybackItem.CurrencyChange)
                if (!wallet.CanAfford(currencyTypeId, currencyAmount))
                    return;
            if (!IsCurrentOwner())
                return;

            foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in buybackItem.CurrencyChange)
                wallet.CurrencySubtractAmount(currencyTypeId, currencyAmount);

            inventory.AddItem(buybackItem.Item, InventoryLocation.Inventory);
            buybackManager.RemoveItem(player, buybackItem, notify: IsCurrentOwner());

            bool IsCurrentOwner() => ReferenceEquals(session.Player, player)
                && ReferenceEquals(player.Session, ownerSession) && ReferenceEquals(player.Inventory, inventory);
        }
    }
}
