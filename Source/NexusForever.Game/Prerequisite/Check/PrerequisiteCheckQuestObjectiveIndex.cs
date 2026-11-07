using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite types 22, 23, 33, 34, 35 and 36 (<c>QuestObjective0..5</c>): the client evaluator forwards all six to one call
    /// <c>(unit, comparison, objectId, index, value)</c> with the index 0 to 5. In the retail rows <c>ObjectId</c> is a quest id and <c>Value</c> is at or below
    /// the required count of that quest's objective at the index, so the check compares the player's progress on that objective with <c>Value</c>.
    /// A quest that is not active, or has no objective at the index, does not meet the prerequisite (the behaviour of the unhandled types before this check).
    /// </summary>
    public abstract class PrerequisiteCheckQuestObjectiveIndex(byte objectiveIndex) : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return QuestObjectivePrerequisiteHelper.TryGetObjectiveProgress(player, objectId, objectiveIndex, out uint progress)
                && PrerequisiteCompare.Compare(comparison, progress, value);
        }
    }

    [PrerequisiteCheck(PrerequisiteType.QuestObjective0)]
    public sealed class PrerequisiteCheckQuestObjective0() : PrerequisiteCheckQuestObjectiveIndex(0);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective1)]
    public sealed class PrerequisiteCheckQuestObjective1() : PrerequisiteCheckQuestObjectiveIndex(1);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective2)]
    public sealed class PrerequisiteCheckQuestObjective2() : PrerequisiteCheckQuestObjectiveIndex(2);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective3)]
    public sealed class PrerequisiteCheckQuestObjective3() : PrerequisiteCheckQuestObjectiveIndex(3);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective4)]
    public sealed class PrerequisiteCheckQuestObjective4() : PrerequisiteCheckQuestObjectiveIndex(4);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective5)]
    public sealed class PrerequisiteCheckQuestObjective5() : PrerequisiteCheckQuestObjectiveIndex(5);
}
