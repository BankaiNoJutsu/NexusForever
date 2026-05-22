using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Item-auction server expiry. Retail auction-house listings were a fixed 48 hours with no shorter or
    /// longer player option. Client post (<c>Marketplace_SendClientAuctionSellOrderSubmit</c>) sends only
    /// item guid, minimum bid, and buyout (no duration on <c>0x06DC</c>); the server applies
    /// <c>GameFormula</c> <c>821</c> (<c>Dataint0</c> hours, default 48).
    /// </summary>
    internal static class MarketplaceAuctionDuration
    {
        private const uint DefaultAuctionDurationGameFormulaId = 821u;
        private const ulong FallbackExpirationSeconds = 172800ul;

        public static ulong GetDefaultExpirationSeconds()
        {
            GameTableManager gameTables = LegacyServiceProvider.Provider?.GetService<GameTableManager>();
            GameFormulaEntry entry = gameTables?.GameFormula?.GetEntry(DefaultAuctionDurationGameFormulaId);
            if (entry != null && entry.Dataint0 > 0u)
                return entry.Dataint0 * 3600ul;

            return FallbackExpirationSeconds;
        }
    }
}
