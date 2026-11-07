using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite types 22, 23, 33, 34, 35 and 36 (<c>QuestObjective0..5</c>): the client evaluator forwards all six to one call
    /// <c>FUN_14049f4d0(unit, comparison, objectId, index, value)</c> with the index 0 to 5. <c>ObjectId</c> is a quest id and the objective's progress
    /// (see <see cref="QuestObjectivePrerequisiteHelper.GetObjectiveProgress"/>) is always compared with <c>Value</c>, so a quest the player has not started compares
    /// progress 0. See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    public abstract class PrerequisiteCheckQuestObjectiveIndex(byte objectiveIndex, IGameTableManager gameTableManager) : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint progress = QuestObjectivePrerequisiteHelper.GetObjectiveProgress(player, gameTableManager, objectId, objectiveIndex);
            return PrerequisiteCompare.Compare(comparison, progress, value);
        }
    }

    [PrerequisiteCheck(PrerequisiteType.QuestObjective0)]
    public sealed class PrerequisiteCheckQuestObjective0(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(0, gameTableManager);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective1)]
    public sealed class PrerequisiteCheckQuestObjective1(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(1, gameTableManager);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective2)]
    public sealed class PrerequisiteCheckQuestObjective2(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(2, gameTableManager);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective3)]
    public sealed class PrerequisiteCheckQuestObjective3(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(3, gameTableManager);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective4)]
    public sealed class PrerequisiteCheckQuestObjective4(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(4, gameTableManager);

    [PrerequisiteCheck(PrerequisiteType.QuestObjective5)]
    public sealed class PrerequisiteCheckQuestObjective5(IGameTableManager gameTableManager) : PrerequisiteCheckQuestObjectiveIndex(5, gameTableManager);
}
