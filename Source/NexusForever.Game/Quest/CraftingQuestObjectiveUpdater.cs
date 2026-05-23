using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits CraftSchematic quest objectives when a tradeskill schematic is crafted.
    /// Evidence: QuestObjective type 33 <c>Data</c> matches TradeskillSchematic2.Id for craft-recipe rows.
    /// </summary>
    public static class CraftingQuestObjectiveUpdater
    {
        public static void OnSchematicCrafted(IPlayer player, uint tradeskillSchematic2Id, uint craftCount)
        {
            if (player?.QuestManager == null || tradeskillSchematic2Id == 0u || craftCount == 0u)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, tradeskillSchematic2Id, craftCount);
        }
    }
}
