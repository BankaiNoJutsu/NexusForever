using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 68: handler table <c>14049ecc0</c> reads quest objective completion on player quest state.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.QuestObjective)]
    public class PrerequisiteCheckQuestObjective : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint complete = QuestObjectivePrerequisiteHelper.IsObjectiveComplete(player, objectId) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, complete, value);
        }
    }
}
