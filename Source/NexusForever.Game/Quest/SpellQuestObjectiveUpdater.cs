using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits generic spell-cast quest objectives after a player spell successfully executes.
    /// </summary>
    public static class SpellQuestObjectiveUpdater
    {
        public static void UpdateSpellSuccessObjectives(IPlayer player, uint spell4Id)
        {
            if (player?.QuestManager == null || spell4Id == 0u)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess, spell4Id, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess2, spell4Id, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess3, spell4Id, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess4, spell4Id, 1u);
        }
    }
}
