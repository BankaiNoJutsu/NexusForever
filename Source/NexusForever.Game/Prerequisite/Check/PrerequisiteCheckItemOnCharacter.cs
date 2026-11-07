using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 14: native test <c>FUN_14049d200</c> (vtable <c>+0x128</c>) calls <c>FUN_1403ac590(inventory, 0x303, Value)</c>: <c>Value</c> is an Item2 id (491 of 493 retail slots; <c>ObjectId</c> is
    /// not read) and the mask selects the inventory locations 0, 1, 8 and 9. Equal returns "the item is held", NotEqual its negation and every other comparison is false. The server checks the Equipped and Inventory
    /// bags; locations 8 and 9 are not modelled. Retail: Equal 394, NotEqual 99. The previous implementation counted items by <c>ObjectId</c> (0 in 468 slots) and compared the count with <c>Value</c>.
    /// See Decomp/Analysis/PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ItemOnCharacter)]
    public class PrerequisiteCheckItemOnCharacter : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, InventoryPrerequisiteHelper.HasItem(player, value, InventoryLocation.Equipped, InventoryLocation.Inventory));
    }
}
