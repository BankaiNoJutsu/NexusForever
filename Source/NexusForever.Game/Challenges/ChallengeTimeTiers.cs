using NexusForever.GameTable.Model;

namespace NexusForever.Game.Challenges
{
    /// <summary>
    /// Rules for time-tiered challenges (<c>ChallengeFlags &amp; 8</c>, 80 rows): the tier goals are minimum remaining times in milliseconds, not counts, and the per-challenge time limit is server
    /// data (the packet's <c>TimeTotalActive</c>), not a table column. See Decomp/Analysis/CHALLENGE_TIME_TIERED_20261108.md and CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md.
    /// </summary>
    public static class ChallengeTimeTiers
    {
        public const uint TimeTieredFlag = 8u;

        public static bool IsTimeTiered(ChallengeEntry entry)
            => entry != null && (entry.ChallengeFlags & TimeTieredFlag) != 0u;

        /// <summary>
        /// Number of tiers a completion at <paramref name="elapsedMs"/> earns: a tier with goal <c>g</c> is lost once <c>elapsed &gt;= limit - g</c> (the client's counter for a tier is the main timer
        /// minus its goal, read natively and confirmed in a 2016 retail clip), so it is kept while <c>elapsed &lt; limit - g</c>. A goal of 0 is kept for as long as the timer runs.
        /// </summary>
        public static uint AchievedTierCount(uint limitMs, uint elapsedMs, IReadOnlyList<uint> goalsMs)
        {
            uint achieved = 0u;
            foreach (uint goal in goalsMs)
            {
                if ((long)elapsedMs < (long)limitMs - goal)
                    achieved++;
            }

            return achieved;
        }
    }

    /// <summary>
    /// Time limits (seconds) of challenges whose start timer was read in dated retail footage; every other challenge keeps the server default. Only exact readings are listed (a probable value or a
    /// lower bound is not): the widget first appears at limit minus one to two seconds, and several clips agree. Source: Decomp/Analysis/CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md.
    /// </summary>
    public static class ChallengeTimeLimits
    {
        private static readonly Dictionary<uint, double> ObservedSeconds = new()
        {
            [273u]  = 360d,   // Bringin' the Bingberries, two videos (2014)
            [481u]  = 120d,   // Up the Falls (not time-tiered), one video (2014)
            [511u]  = 60d,    // Ravenok Racer, one video (2014)
            [753u]  = 80d,    // Top of the World, six videos (2014)
            [956u]  = 65d,    // Back to Basic, two videos (2014, 2015)
            [979u]  = 1200d,  // Doing Things The Hard Way, three videos (2014)
            [1002u] = 120d    // Thayd Sprint, five videos (2014, 2018)
        };

        /// <summary>
        /// The observed limit of the challenge in seconds, or null when none was observed.
        /// </summary>
        public static double? GetSeconds(uint challengeId)
            => ObservedSeconds.TryGetValue(challengeId, out double seconds) ? seconds : null;
    }
}
