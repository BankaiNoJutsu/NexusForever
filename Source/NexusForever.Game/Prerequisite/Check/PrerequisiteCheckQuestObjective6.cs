using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 94: handler table <c>14049f700</c>; quest objective completion on evaluated unit.
    /// NF reuses the type 68 quest-objective completion path.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.QuestObjective6)]
    public class PrerequisiteCheckQuestObjective6 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint complete = QuestObjectivePrerequisiteHelper.IsObjectiveComplete(player, objectId) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, complete, value);
        }
    }
}
