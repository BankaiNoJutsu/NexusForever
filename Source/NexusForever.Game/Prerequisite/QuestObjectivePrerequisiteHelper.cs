using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;

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
