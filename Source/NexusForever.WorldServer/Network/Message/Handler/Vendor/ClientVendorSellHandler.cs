using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

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

            if (item.Info == null)
                return;

            if (vendorSell.Quantity == 0u || vendorSell.Quantity > item.StackCount)
                return;

            float costMultiplier = vendorInfo.SellPriceMultiplier * vendorSell.Quantity;
            var currencyChange = new List<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)>();
            for (byte i = 0; i < 2; i++)
            {
                CurrencyType currencyId = item.GetVendorSellCurrency(i);
                if (currencyId == CurrencyType.None)
                    continue;

                ulong currencyAmount = (ulong)(item.GetVendorSellAmount(i) * costMultiplier);
                if (currencyAmount == 0ul)
                    continue;

                currencyChange.Add((currencyId, currencyAmount));
            }

            foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in currencyChange)
                session.Player.CurrencyManager.CurrencyAddAmount(currencyTypeId, currencyAmount);

            IItem soldItem = session.Player.Inventory.ItemDelete(vendorSell.ItemLocation, vendorSell.Quantity, ItemUpdateReason.Vendor);
            buybackManager.AddItem(session.Player, soldItem, vendorSell.Quantity, currencyChange);
        }
    }
}
