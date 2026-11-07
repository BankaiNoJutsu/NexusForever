using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class QuestObjectivePrerequisiteHelper
    {
        public static bool IsObjectiveComplete(IPlayer player, uint questObjectiveId)
        {
            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                foreach (IQuestObjective objective in quest)
                {
                    if (objective.ObjectiveInfo.Id == questObjectiveId && objective.IsComplete())
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Native test behind prerequisite types 68 and 94 (<c>FUN_1405510d0</c>, reached through <c>FUN_14049f460</c> / <c>FUN_14049f5b0</c>): the quest with id <paramref name="questId"/>
        /// has its objective at <paramref name="objectiveIndex"/> complete. A Completed quest counts every valid index as complete; an active quest uses that objective's own
        /// completion; an unknown, abandoned or otherwise inactive quest, and any index beyond the quest's objectives, is not complete. The native index bound is 6.
        /// </summary>
        public static bool IsQuestObjectiveIndexComplete(IPlayer player, uint questId, uint objectiveIndex)
        {
            if (questId > ushort.MaxValue || objectiveIndex >= 6u)
                return false;

            switch (player.QuestManager.GetQuestState((ushort)questId))
            {
                case QuestState.Completed:
                    return true;
                case QuestState.Accepted:
                case QuestState.Achieved:
                    foreach (IQuest quest in player.QuestManager.GetActiveQuests())
                    {
                        if (quest.Id != questId)
                            continue;

                        foreach (IQuestObjective objective in quest)
                        {
                            if (objective.Index == objectiveIndex)
                                return objective.IsComplete();
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Native comparison handling shared by types 68 and 94: Equal returns the result, NotEqual its negation, anything else is false.
        /// </summary>
        public static bool CompareCompletion(PrerequisiteComparison comparison, bool complete)
            => comparison switch
            {
                PrerequisiteComparison.Equal    => complete,
                PrerequisiteComparison.NotEqual => !complete,
                _                               => false
            };

        /// <summary>
        /// Progress compared by prerequisite types 22, 23, 33, 34, 35 and 36 (<c>QuestObjective0..5</c>, native <c>FUN_14049f4d0</c>): the quest definition is looked up by id
        /// (<c>FUN_1405a8a40</c>); a quest that is not defined has progress 0. A Completed quest reports the objective's required count (<c>FUN_140551840</c>: 0 when
        /// there is no objective at the index, 100 for a kill-type objective with a count of at least 2 and without flag 0x200). Otherwise the progress is that of the
        /// player's active quest objective, and 0 when the quest is not active or has no objective at the index. The result is always handed to the compare helper,
        /// so progress 0 can still satisfy Equal 0 or LessThan.
        /// Unresolved: for kill-type objectives without flag 0x200 the native active-quest path also rescales the objective's own value by <c>100 * x * 0.001</c>
        /// (<c>FUN_1406113b0</c>); the unit of the client's runtime value is not known, so the server's own progress is returned unscaled for them.
        /// </summary>
        public static uint GetObjectiveProgress(IPlayer player, IGameTableManager gameTableManager, uint questId, byte objectiveIndex)
        {
            if (questId > ushort.MaxValue || gameTableManager.Quest2?.GetEntry(questId) is not Quest2Entry definition)
                return 0u;

            switch (player.QuestManager.GetQuestState((ushort)questId))
            {
                case QuestState.Completed:
                    return GetCompletedObjectiveCount(definition, gameTableManager, objectiveIndex);
                default:
                    foreach (IQuest quest in player.QuestManager.GetActiveQuests())
                    {
                        if (quest.Id != questId)
                            continue;

                        foreach (IQuestObjective objective in quest)
                        {
                            if (objective.Index == objectiveIndex)
                                return objective.Progress;
                        }
                    }

                    return 0u;
            }
        }

        private static uint GetCompletedObjectiveCount(Quest2Entry definition, IGameTableManager gameTableManager, byte objectiveIndex)
        {
            if (definition.Objectives == null || objectiveIndex >= definition.Objectives.Length)
                return 0u;

            uint objectiveId = definition.Objectives[objectiveIndex];
            if (objectiveId == 0u || gameTableManager.QuestObjective?.GetEntry(objectiveId) is not QuestObjectiveEntry objective)
                return 0u;

            if (objective.Count < 2u)
                return objective.Count;

            // Native bitset 0x400000018104: objective types 2, 8, 15, 16 and 46 (the kill types) report 100 unless flag 0x200 is set.
            bool killType = (QuestObjectiveType)objective.Type is QuestObjectiveType.KillCreature or QuestObjectiveType.KillTargetGroups
                or QuestObjectiveType.KillNamedCreature or QuestObjectiveType.KillTargetGroup or QuestObjectiveType.KillCreature2;
            return killType && (objective.Flags & 0x200u) == 0u ? 100u : objective.Count;
        }
    }
}
