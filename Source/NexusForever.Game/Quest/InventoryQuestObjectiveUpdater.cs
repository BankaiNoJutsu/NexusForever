using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Syncs CollectItem quest objectives from inventory counts.
    /// Evidence: QuestObjective type 4 <c>Data</c> correlates to Item2Id (client QuestObjective.tbl).
    /// </summary>
    public static class InventoryQuestObjectiveUpdater
    {
        public static void RefreshCollectItemForItem(IPlayer player, uint item2Id)
        {
            if (player?.QuestManager == null || player.Inventory == null || item2Id == 0u)
                return;

            uint owned = player.Inventory.GetItemCount(item2Id);
            if (owned == 0u)
                return;

            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                foreach (IQuestObjective objective in quest)
                {
                    if (objective.IsComplete())
                        continue;

                    if (objective.ObjectiveInfo.Type != QuestObjectiveType.CollectItem)
                        continue;

                    if (objective.ObjectiveInfo.Entry.Data != item2Id)
                        continue;

                    if (owned <= objective.Progress)
                        continue;

                    quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, owned - objective.Progress);
                }
            }
        }

        public static void SyncCollectObjectivesForQuest(IQuest quest)
        {
            if (quest?.Player?.Inventory == null || quest.Player.QuestManager == null)
                return;

            foreach (IQuestObjective objective in quest)
            {
                if (objective.ObjectiveInfo.Type != QuestObjectiveType.CollectItem)
                    continue;

                if (objective.ObjectiveInfo.Entry.Data == 0u)
                    continue;

                RefreshCollectItemForItem(quest.Player, objective.ObjectiveInfo.Entry.Data);
            }
        }
    }
}
