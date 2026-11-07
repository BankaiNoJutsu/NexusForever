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
        /// For a kill-type objective with a required count of at least 2 and without flag 0x200 both branches report a percentage: 100 when the quest is Completed and, for an active quest,
        /// <c>100 * raw * 0.001</c> of the objective's own value (<c>FUN_1406113b0</c>). That raw value is taken to be per-mille of completion, so the server's kill count becomes
        /// <c>kills * 100 / count</c>, capped at 100 (inferred: the unit of the client's runtime value was not read, but 100 for Completed fixes the scale as percent).
        /// </summary>
        public static uint GetObjectiveProgress(IPlayer player, IGameTableManager gameTableManager, uint questId, byte objectiveIndex)
        {
            if (questId > ushort.MaxValue || gameTableManager.Quest2?.GetEntry(questId) is not Quest2Entry definition)
                return 0u;

            QuestObjectiveEntry entry = GetObjectiveEntry(definition, gameTableManager, objectiveIndex);
            switch (player.QuestManager.GetQuestState((ushort)questId))
            {
                case QuestState.Completed:
                    if (entry == null)
                        return 0u;

                    return IsPercentageObjective(entry) ? 100u : entry.Count;
                default:
                    foreach (IQuest quest in player.QuestManager.GetActiveQuests())
                    {
                        if (quest.Id != questId)
                            continue;

                        foreach (IQuestObjective objective in quest)
                        {
                            if (objective.Index != objectiveIndex)
                                continue;

                            if (entry != null && IsPercentageObjective(entry))
                                return (uint)Math.Min(100ul, (ulong)objective.Progress * 100ul / entry.Count);

                            return objective.Progress;
                        }
                    }

                    return 0u;
            }
        }

        private static QuestObjectiveEntry GetObjectiveEntry(Quest2Entry definition, IGameTableManager gameTableManager, byte objectiveIndex)
        {
            if (definition.Objectives == null || objectiveIndex >= definition.Objectives.Length)
                return null;

            uint objectiveId = definition.Objectives[objectiveIndex];
            return objectiveId == 0u ? null : gameTableManager.QuestObjective?.GetEntry(objectiveId);
        }

        /// <summary>
        /// Native bitset <c>0x400000018104</c>: the kill objective types 2, 8, 15, 16 and 46, with a required count of at least 2 and flag 0x200 clear, are reported as a percentage.
        /// </summary>
        private static bool IsPercentageObjective(QuestObjectiveEntry objective)
        {
            if (objective.Count < 2u || (objective.Flags & 0x200u) != 0u)
                return false;

            return (QuestObjectiveType)objective.Type is QuestObjectiveType.KillCreature or QuestObjectiveType.KillTargetGroups
                or QuestObjectiveType.KillNamedCreature or QuestObjectiveType.KillTargetGroup or QuestObjectiveType.KillCreature2;
        }
    }
}
