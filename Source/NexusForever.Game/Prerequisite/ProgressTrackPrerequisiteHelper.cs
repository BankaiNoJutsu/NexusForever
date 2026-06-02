using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// NF proxy for client <c>Progress_GetTrackedScalar</c> (<c>1403fa980</c>) used by prerequisite
    /// type 178 (<c>trackType=1</c>, <c>trackId=objectId0</c>). Client walks
    /// <c>DAT_140c65898+0x8010</c>; runtime storage is not modeled yet.
    /// </summary>
    internal static class ProgressTrackPrerequisiteHelper
    {
        public const uint ClientProgressTrackTypeEnum = 1u;

        public static uint GetTrackedScalar(IPlayer player, IGameTableManager gameTableManager, uint trackType, uint trackId)
        {
            if (trackType != ClientProgressTrackTypeEnum)
                return 0u;

            if (gameTableManager.Challenge.GetEntry(trackId) != null)
                return player.ChallengeManager.GetCompletionCount((ushort)trackId);

            return 0u;
        }
    }
}
