using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;

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
        /// Progress of the objective at <paramref name="objectiveIndex"/> of the player's active quest <paramref name="questId"/>.
        /// Types 22, 23, 33, 34, 35 and 36 (<c>QuestObjective0..5</c>) pass the quest id as <c>ObjectId</c> and the objective index through one shared evaluator
        /// call. Returns false when the quest is not active or has no objective at that index, which is how these types behaved while they were unhandled.
        /// </summary>
        public static bool TryGetObjectiveProgress(IPlayer player, uint questId, byte objectiveIndex, out uint progress)
        {
            progress = 0u;
            if (questId > ushort.MaxValue)
                return false;

            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                if (quest.Id != questId)
                    continue;

                foreach (IQuestObjective objective in quest)
                {
                    if (objective.Index != objectiveIndex)
                        continue;

                    progress = objective.Progress;
                    return true;
                }
            }

            return false;
        }
    }
}
