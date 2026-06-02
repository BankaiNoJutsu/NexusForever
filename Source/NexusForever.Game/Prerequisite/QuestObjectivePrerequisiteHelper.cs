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
    }
}
