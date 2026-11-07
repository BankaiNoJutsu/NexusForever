using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 201: native test <c>FUN_14049f810</c> looks up the achievement with id <c>ObjectId</c> and tests bit <c>Value</c> of its record
    /// (<c>FUN_140642a00</c>: bits 0 to 31 of the progress state, 32 to 63 of the credited checklist word). Equal meets when the bit is set, NotEqual when it is
    /// clear (including an achievement the player has no record of), other comparisons never meet. See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.AchievementObject)]
    public class PrerequisiteCheckAchievementChecklistBit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (objectId > ushort.MaxValue)
                return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, false);

            bool set = player.AchievementManager.IsChecklistBitSet((ushort)objectId, value);
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, set);
        }
    }
}
