using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits generic spell-cast quest objectives after a player spell successfully executes.
    /// </summary>
    public static class SpellQuestObjectiveUpdater
    {
        /// <param name="target">
        /// The primary target of the cast. SpellSuccess2 is a checklist objective: its progress is the bit of the target's checklist index, so a cast without a target does not
        /// advance it. The three retail SpellSuccess2 objectives all use spells cast on a creature (Spell4 26617, 36071, 36419).
        /// </param>
        public static void UpdateSpellSuccessObjectives(IPlayer player, uint spell4Id, IWorldEntity target = null)
        {
            if (player?.QuestManager == null || spell4Id == 0u)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess, spell4Id, 1u);
            if (target != null && target is not IPlayer)
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess2, spell4Id, target.QuestChecklistIdx);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess3, spell4Id, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess4, spell4Id, 1u);
        }
    }
}
