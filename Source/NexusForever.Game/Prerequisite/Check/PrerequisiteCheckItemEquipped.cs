using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 13: native test <c>FUN_14049d1c0</c> (vtable <c>+0x120</c>) calls <c>FUN_1403ac590(inventory, 1, Value)</c>: <c>Value</c> is an Item2 id (26 of 27 retail slots; <c>ObjectId</c> is not read) and
    /// mask 1 selects the Equipped location. Equal returns "the item is equipped", NotEqual its negation and every other comparison is false. The previous implementation read <c>ObjectId</c> as an equipped slot index.
    /// See Decomp/Analysis/PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ItemEquipped)]
    public class PrerequisiteCheckItemEquipped : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, InventoryPrerequisiteHelper.HasItem(player, value, InventoryLocation.Equipped));
    }
}
