using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Duplicate-body alias for ItemTradeSkillKnown, with account-item claim rows using objectId as a Holo-Wardrobe Item2 unlock id.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unknown275)]
    public class PrerequisiteCheckUnknown275 : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckUnknown275(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters?.AccountItemContext == true)
            {
                uint expected = objectId != 0u ? objectId : value;
                uint current = player.Account?.CostumeManager?.HasItemUnlock(expected) == true ? expected : 0u;
                return PrerequisiteCompare.Compare(comparison, current, expected);
            }

            // Native (evaluator case 0x113, vtable +0x6a0, 1404a17e0) receives the primary unit, the comparison and ObjectId. The unit must be a player (0x14) or ghost (0x17), then the
            // known-item lookup of ObjectId is returned for Equal and negated for NotEqual; anything else is false (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // Value is never read. The previous path required a non-player target (the type gate read backwards) and compared the flag with Value, which inverted all 281 retail slots (NotEqual, Value 0).
            return PrerequisiteCompare.CompareFlag(comparison, TradeskillPrerequisiteHelper.HasKnownItem2Id(player, gameTableManager, objectId));
        }
    }
}
