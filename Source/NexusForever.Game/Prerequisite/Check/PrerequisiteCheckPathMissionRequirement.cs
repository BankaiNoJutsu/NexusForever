using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 67: native test <c>FUN_14049fe10</c> finds the path mission with id <c>Value</c> (a PathMission id in 174 of 191 retail slots) and reads its completion
    /// (mission virtual <c>+0x38</c>, the same flag type 61 counts). Equal meets when complete, NotEqual when not (a missing mission is not complete), other comparisons never meet.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathMissionRequirement)]
    public class PrerequisiteCheckPathMissionRequirement : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool complete = player.PathManager?.IsMissionComplete(value) ?? false;
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, complete);
        }
    }
}
