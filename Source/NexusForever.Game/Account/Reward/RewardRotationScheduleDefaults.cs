using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Schedule expiry duration for reward rotation rows.
    /// Client <c>RewardRotation_ApplyServerScheduleUpdate</c> (140636280) treats the wire
    /// <c>Duration</c> float as days (FILETIME expiry = now + days * 864000000000).
    /// </summary>
    internal static class RewardRotationScheduleDefaults
    {
        // Correlated with retail 48-hour cadence via the same GameFormula row used for 48h auctions.
        public const uint RotationDurationGameFormulaId = 821u;
        public const float FallbackRotationDurationDays = 2f;

        public static float GetRotationDurationDays(IGameTableManager gameTables = null)
        {
            if (gameTables == null)
                return FallbackRotationDurationDays;

            GameFormulaEntry entry = gameTables.GameFormula?.GetEntry(RotationDurationGameFormulaId);
            if (entry != null && entry.Dataint0 > 0u)
                return entry.Dataint0 / 24f;

            return FallbackRotationDurationDays;
        }
    }
}
