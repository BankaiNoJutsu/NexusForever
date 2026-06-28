using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Challenges
{
    /// <summary>
    /// Credits active data-owned challenges by challenge id from script or runtime objective producers.
    /// </summary>
    public static class ChallengeProgressHooks
    {
        public static bool OnChallengeProgress(IPlayer player, ushort challengeId, uint progress = 1u)
        {
            if (player?.ChallengeManager == null)
                return false;

            return player.ChallengeManager.TryAdvanceProgress(challengeId, progress);
        }
    }
}
