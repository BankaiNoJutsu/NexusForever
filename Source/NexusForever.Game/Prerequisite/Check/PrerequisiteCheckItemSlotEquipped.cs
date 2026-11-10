using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 178 (enum name <c>ProgressTrackOnMatchingEntity</c>, an older guess): evaluator case <c>0xb2</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager
    /// vtable <c>+0x140</c> (<c>14049d2c0</c>) with the comparison and <b>ObjectId</b>. ObjectId is an <c>ItemSlot</c> id (<c>14020e7a0</c>). The index of the lowest set bit of the row's
    /// <c>equippedSlotFlags</c> selects an entry of the local player's equipped storage (<c>Entity_GetIndexedSlotEntry</c>, selector 0); the flag is "that entry exists". Equal returns it, NotEqual
    /// its negation, any other comparison false. Without a row, or with no usable bit, only NotEqual is met. Value is never read.
    /// </summary>
    /// <remarks>
    /// Retail: 2 slots, both Equal. ObjectId 46 is ItemSlot "ArmorGadget" (flags 0x800, equipped index 11) with Value 100, the unlock prerequisite of ActionSlotPrereq slot index 8, the gadget action
    /// slot: "the gadget slot unlocks when a gadget is equipped". ObjectId 43 is "ArmorShields" (flags 0x8000, index 15).
    /// The previous implementation followed the body at <c>1404a1890</c>, which the evaluator dispatches for type 291 only, and compared a progress scalar with Value.
    /// Status: Implemented from the native body; that the indexed storage with selector 0 is the equipped bag is inferred from the bit to equipped-index match. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.ProgressTrackOnMatchingEntity)]
    public class PrerequisiteCheckItemSlotEquipped : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemSlotEquipped(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            ItemSlotEntry slot = gameTableManager?.ItemSlot?.GetEntry(objectId);
            int index = slot == null || slot.EquippedSlotFlags == 0u ? -1 : BitOperations.TrailingZeroCount(slot.EquippedSlotFlags);
            if (index < 0 || index >= 0x1f)
                return comparison == PrerequisiteComparison.NotEqual;

            bool equipped = player?.Inventory?.GetItem(InventoryLocation.Equipped, (uint)index) != null;
            return PrerequisiteCompare.CompareFlag(comparison, equipped);
        }
    }
}
