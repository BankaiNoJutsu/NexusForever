using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    internal static class VendorSellHelper
    {
        // Client Item.CodeEnumItem2Category.Junk; verified by jabbithole.item_categories game_id 94.
        private const uint JunkItem2CategoryId = 94u;

        public static void SellJunk(IPlayer player, IBuybackManager buybackManager)
        {
            if (player.SelectedVendorInfo == null)
                return;

            List<IItem> junkItems = player.Inventory
                .Where(b => b.Location == InventoryLocation.Inventory)
                .SelectMany(b => b)
                .Where(IsJunkItem)
                .ToList();

            foreach (IItem item in junkItems)
            {
                if (item.StackCount == 0u)
                    continue;

                var itemLocation = new ItemLocation
                {
                    Location = item.Location,
                    BagIndex = item.BagIndex
                };

                SellItem(player, buybackManager, itemLocation, item, item.StackCount);
            }
        }

        public static bool SellItem(IPlayer player, IBuybackManager buybackManager, ItemLocation itemLocation, IItem item, uint quantity)
        {
            if (player.SelectedVendorInfo == null)
                return false;

            if (item?.Info == null)
                return false;

            if (quantity == 0u || quantity > item.StackCount)
                return false;

            float costMultiplier = player.SelectedVendorInfo.SellPriceMultiplier * quantity;
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

            IItem soldItem = player.Inventory.ItemDelete(itemLocation, quantity, ItemUpdateReason.Vendor);
            if (soldItem == null)
                return false;

            foreach ((CurrencyType currencyTypeId, ulong currencyAmount) in currencyChange)
                player.CurrencyManager.CurrencyAddAmount(currencyTypeId, currencyAmount);

            buybackManager.AddItem(player, soldItem, quantity, currencyChange);
            return true;
        }

        private static bool IsJunkItem(IItem item)
        {
            return item?.Info?.Entry?.Item2CategoryId == JunkItem2CategoryId;
        }
    }
}
