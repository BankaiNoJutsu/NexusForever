using NexusForever.GameTable;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Commodity order list/expiration <c>FILETIME</c> normalization against <see cref="MarketplaceListingDuration"/>.
    /// </summary>
    internal static class MarketplaceCommodityDuration
    {
        public static ulong ResolveListFileTime(ulong clientListTime)
        {
            if (clientListTime != 0ul)
                return clientListTime;

            return (ulong)DateTime.UtcNow.ToFileTimeUtc();
        }

        public static ulong ResolveExpirationFileTime(ulong clientListTime, ulong clientExpirationTime, IGameTableManager gameTables = null)
        {
            ulong listFileTime = ResolveListFileTime(clientListTime);
            ulong[] tiers = MarketplaceListingDuration.GetTierSeconds(gameTables);

            if (clientExpirationTime > listFileTime)
            {
                ulong durationSeconds = (clientExpirationTime - listFileTime) / MarketplaceListingDuration.FileTimeTicksPerSecond;
                foreach (ulong tier in tiers)
                {
                    if (durationSeconds == tier)
                        return clientExpirationTime;
                }

                foreach (ulong tier in tiers.OrderBy(t => t))
                {
                    if (durationSeconds <= tier)
                        return listFileTime + tier * MarketplaceListingDuration.FileTimeTicksPerSecond;
                }

                return listFileTime + tiers[^1] * MarketplaceListingDuration.FileTimeTicksPerSecond;
            }

            return listFileTime + tiers[^1] * MarketplaceListingDuration.FileTimeTicksPerSecond;
        }
    }
}
