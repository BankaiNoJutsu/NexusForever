using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits quest objectives tied to public-event objective ids.
    /// Evidence: QuestObjective types 20/25/31/33/44 <c>Data</c> correlate to
    /// <c>PublicEventObjective.Id</c> (client table crosswalk 2026-05-23). Only matching
    /// active objectives receive credit because <see cref="IQuestManager.ObjectiveUpdate"/>
    /// filters by type and data. Type 28 was in this list until 2026-11-10; its <c>Data</c> is a
    /// PathSoldierActivate id (see <see cref="SoldierActivateQuestObjectiveUpdater"/>).
    /// </summary>
    public static class PublicEventQuestObjectiveUpdater
    {
        private static readonly QuestObjectiveType[] PublicEventObjectiveLinkedTypes =
        [
            QuestObjectiveType.CompletePublicEventRecipe,
            QuestObjectiveType.CompleteEvent,
            QuestObjectiveType.CompletePublicEventObjective,
            QuestObjectiveType.CraftSchematic,
            QuestObjectiveType.CombatMomentum,
        ];

        public static void OnPublicEventObjectiveSucceeded(IPlayer player, uint publicEventObjectiveId)
        {
            if (player?.QuestManager == null || publicEventObjectiveId == 0u)
                return;

            if (player is Player && player.QuestManager is QuestManager manager)
            {
                manager.CreditPublicEventQuestObjective(publicEventObjectiveId, PublicEventObjectiveLinkedTypes);
                return;
            }

            foreach (QuestObjectiveType objectiveType in PublicEventObjectiveLinkedTypes)
                player.QuestManager.ObjectiveUpdate(objectiveType, publicEventObjectiveId, 1u);
        }
    }
}
