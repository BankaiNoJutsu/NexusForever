using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class AccountItemOwnedCount
    {
        /// <summary>
        /// Mirrors client <c>AccountItem_CountOwnedByItem2Id</c> @ <c>1403d2140</c>: sum Item2 stacks on the
        /// character inventory plus account-item rows for the same Item2 id, capped by Item2 MaxStackCount.
        /// </summary>
        public static uint CountOwnedByItem2Id(IPlayer player, uint item2Id, IGameTableManager gameTableManager)
        {
            uint count = player.Inventory?.GetItemCount(item2Id) ?? 0u;

            if (player.Account?.InventoryManager != null)
            {
                foreach (IAccountInventoryItem accountItem in player.Account.InventoryManager)
                {
                    if (accountItem.Entry.Item2Id == item2Id)
                        count++;
                }
            }

            Item2Entry item2 = gameTableManager.Item.GetEntry(item2Id);
            if (item2 != null && item2.MaxStackCount > 0u && count > item2.MaxStackCount)
                count = item2.MaxStackCount;

            return count;
        }

        /// <summary>
        /// Mirrors client <c>AccountItemList_WalkItem2IdMatch</c> @ <c>140497d9c</c>: counts account-item rows
        /// whose node <c>+0x20</c> Item2 id matches (handler-table types 97–101; excludes character bags).
        /// </summary>
        public static uint CountAccountRowsByItem2Id(IPlayer player, uint item2Id, IGameTableManager gameTableManager)
        {
            if (player.Account?.InventoryManager == null)
                return 0u;

            uint count = 0u;
            foreach (IAccountInventoryItem accountItem in player.Account.InventoryManager)
            {
                if (accountItem.Entry.Item2Id == item2Id)
                    count++;
            }

            Item2Entry item2 = gameTableManager.Item.GetEntry(item2Id);
            if (item2 != null && item2.MaxStackCount > 0u && count > item2.MaxStackCount)
                count = item2.MaxStackCount;

            return count;
        }

        public static bool IsItem2OnCharacter(IPlayer player, uint item2Id)
        {
            return (player.Inventory?.GetItemCount(item2Id) ?? 0u) > 0u;
        }
    }
}
