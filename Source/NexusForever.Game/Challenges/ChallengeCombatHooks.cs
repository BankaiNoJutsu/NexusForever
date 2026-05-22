using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Challenges
{
    /// <summary>
    /// Credits active combat challenges when a creature kill matches the challenge table target id.
    /// </summary>
    public static class ChallengeCombatHooks
    {
        public static void OnCreatureKilled(IPlayer player, uint creatureId)
        {
            if (player?.ChallengeManager is not ChallengeManager challengeManager)
                return;

            challengeManager.TryAdvanceCombatKill(creatureId);
        }
    }
}
