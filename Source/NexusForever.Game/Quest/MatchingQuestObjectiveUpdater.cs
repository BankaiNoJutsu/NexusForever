using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits ParticipateInGroupContent objectives when a player enters instanced match content.
    /// Evidence: QuestObjective type 40 <c>Data</c> values 1/2 match <c>MatchingGameType.Id</c>.
    /// </summary>
    public static class MatchingQuestObjectiveUpdater
    {
        public static void OnMatchEntered(IPlayer player, uint matchingGameTypeId)
        {
            if (player?.QuestManager == null || matchingGameTypeId == 0u)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ParticipateInGroupContent, matchingGameTypeId, 1u);
        }
    }
}
