using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 94: native test <c>FUN_1405510d0</c>. <c>ObjectId</c> is a quest id and <c>Value</c> an objective index (retail rows: quest ids in about 99% of
    /// slots, small indexes); the objective at that index is complete (a Completed quest counts every valid index as complete). Equal meets when complete, NotEqual
    /// when not, other comparisons never meet. Earlier code read <c>ObjectId</c> as an objective id; see Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.QuestObjective6)]
    public class PrerequisiteCheckQuestObjective6 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool complete = QuestObjectivePrerequisiteHelper.IsQuestObjectiveIndexComplete(player, objectId, value);
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, complete);
        }
    }
}
