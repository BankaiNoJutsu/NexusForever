using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Challenges
{
    /// <summary>
    /// Credits active ChecklistActivate challenges after a matching creature interaction.
    /// </summary>
    public static class ChallengeActivationHooks
    {
        public static void OnTargetActivated(IPlayer player, uint creatureId)
        {
            if (player?.ChallengeManager is not ChallengeManager challengeManager)
                return;

            challengeManager.TryAdvanceActivationTarget(creatureId);
        }
    }
}
