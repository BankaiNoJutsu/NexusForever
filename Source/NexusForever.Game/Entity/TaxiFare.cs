using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Fare of a flight path. The client never reads <c>TaxiRoute.Price</c> on its fare path: per hop the fare is <c>trunc((float)T * F * (float)T)</c> where <c>T</c> is the
    /// destination node's <see cref="TaxiNodeEntry.ContentTier"/> and <c>F</c> is <c>GameFormula[952].Datafloat0</c> (132.75 in the table and as the compiled-in fallback); the trip
    /// fare is the integer sum of the hops, with no minimum and no player, level, faction or distance term (native <c>FUN_1404ae170</c>, graph builder <c>FUN_1404ae270</c>).
    /// Checked against dated retail footage: tier 1 gives 1s32c, tier 2 5s31c, tier 3 11s94c, two tier 3 hops 23s88c. See Decomp/Analysis/TAXI_FARE_NATIVE_READ_20261108.md.
    /// </summary>
    public static class TaxiFare
    {
        public const uint FormulaId = 952u;
        public const float FallbackFormula = 132.75f;

        /// <summary>
        /// Fare of one hop in credits.
        /// </summary>
        public static ulong Hop(float formula, uint destinationTier)
        {
            float tier = destinationTier;
            float fare = tier * formula * tier;
            return float.IsFinite(fare) && fare > 0f ? (ulong)fare : 0ul;
        }

        /// <summary>
        /// Fare of the route chain in credits (a hop whose destination node is missing costs nothing).
        /// </summary>
        public static ulong Calculate(IGameTableManager tables, IEnumerable<TaxiRouteEntry> routes)
        {
            float formula = tables?.GameFormula?.GetEntry(FormulaId)?.Datafloat0 ?? FallbackFormula;

            ulong total = 0ul;
            foreach (TaxiRouteEntry route in routes)
            {
                TaxiNodeEntry destination = tables?.TaxiNode?.GetEntry(route.TaxiNodeIdDestination);
                total = checked(total + Hop(formula, destination?.ContentTier ?? 0u));
            }

            return total;
        }
    }
}
