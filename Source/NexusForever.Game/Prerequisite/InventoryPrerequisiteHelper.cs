using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Prerequisite
{
    internal static class InventoryPrerequisiteHelper
    {
        /// <summary>
        /// Native <c>FUN_1403ac590(inventory, locationMask, item2Id)</c> as used by prerequisite types 13 and 14: whether a bag of one of the supplied inventory locations holds an item
        /// with the Item2 id. Bit n of the native mask selects inventory location n.
        /// </summary>
        public static bool HasItem(IPlayer player, uint item2Id, params InventoryLocation[] locations)
        {
            if (item2Id == 0u || player.Inventory == null)
                return false;

            foreach (IBag bag in player.Inventory)
            {
                if (!locations.Contains(bag.Location))
                    continue;

                foreach (IItem item in bag)
                {
                    if (item.Id == item2Id)
                        return true;
                }
            }

            return false;
        }
    }
}
