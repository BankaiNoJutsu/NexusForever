using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits quest objectives tied to public-event objective ids.
    /// Evidence: QuestObjective types 20/25/28/31/33/44 <c>Data</c> correlate to
    /// <c>PublicEventObjective.Id</c> (client table crosswalk 2026-05-23). Only matching
    /// active objectives receive credit because <see cref="IQuestManager.ObjectiveUpdate"/>
    /// filters by type and data.
    /// </summary>
    public static class PublicEventQuestObjectiveUpdater
    {
        private static readonly QuestObjectiveType[] PublicEventObjectiveLinkedTypes =
        [
            QuestObjectiveType.Unknown20,
            QuestObjectiveType.CompleteEvent,
            QuestObjectiveType.Unknown28,
            QuestObjectiveType.Unknown31,
            QuestObjectiveType.CraftSchematic,
            QuestObjectiveType.CombatMomentum,
        ];

        public static void OnPublicEventObjectiveSucceeded(IPlayer player, uint publicEventObjectiveId)
        {
            if (player?.QuestManager == null || publicEventObjectiveId == 0u)
                return;

            foreach (QuestObjectiveType objectiveType in PublicEventObjectiveLinkedTypes)
                player.QuestManager.ObjectiveUpdate(objectiveType, publicEventObjectiveId, 1u);
        }
    }
}
