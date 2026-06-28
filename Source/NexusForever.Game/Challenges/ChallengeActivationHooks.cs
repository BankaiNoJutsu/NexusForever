using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Challenges
{
    /// <summary>
    /// Credits active activation-style challenges when a successful interaction matches the challenge table target id.
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
