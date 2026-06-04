using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    public class ClientVendorSellHandler : IMessageHandler<IWorldSession, ClientVendorSell>
    {
        #region Dependency Injection

        private readonly IBuybackManager buybackManager;

        public ClientVendorSellHandler(
            IBuybackManager buybackManager)
        {
            this.buybackManager = buybackManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientVendorSell vendorSell)
        {
            IVendorInfo vendorInfo = session.Player.SelectedVendorInfo;
            if (vendorInfo == null)
                return;

            IItem item = session.Player.Inventory.GetItem(vendorSell.ItemLocation);
            if (item == null)
                return;

            uint quantity = vendorSell.Quantity == 0u ? item.StackCount : vendorSell.Quantity;
            VendorSellHelper.SellItem(session.Player, buybackManager, vendorSell.ItemLocation, item, quantity);
        }
    }
}
