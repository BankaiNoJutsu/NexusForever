using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 56 (enum name <c>DifficultyRankSlotAssigned</c>): evaluator case 0x38 reads <c>Value</c> as a slot index and, for an index below 28, tests that the pointer at
    /// <c>unit+0x2d8 + index*0x10</c> is non-null; the shared tail returns that for Equal, its negation for NotEqual and false for every other comparison. An index of 28 or more is
    /// treated as unassigned. Retail: 124 slots, <c>Value</c> 0 to 24 (led by 8, 0, 9, 20, 2, 15), <c>ObjectId</c> unused, Equal (64) and NotEqual (60).
    /// <para>
    /// Inferred, not read: that the array is the unit's equipped-item slots, indexed like <see cref="EquippedItem"/> (so System 8, Augment 9, Shields 15, bags 17 to 20), is taken from
    /// the retail Values; the body only shows a populated pointer array. The server tests the player's <c>Equipped</c> inventory location.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </para>
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.DifficultyRankSlotAssigned)]
    public class PrerequisiteCheckEquipmentSlotAssigned : IPrerequisiteCheck
    {
        private const uint SlotCount = 28u;

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            bool assigned = value < SlotCount && player.Inventory?.GetItem(InventoryLocation.Equipped, value) != null;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => assigned,
                PrerequisiteComparison.NotEqual => !assigned,
                _                               => false
            };
        }
    }
}
