using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Listing duration tiers from client <c>GameFormula</c> <c>1056</c> (<c>0x420</c>), used by
    /// commodity orders and the item-auction UI binder <c>FUN_1406a1e80</c>.
    /// </summary>
    internal static class MarketplaceListingDuration
    {
        public const uint ListingDurationGameFormulaId = 1056u;
        public const ulong FileTimeTicksPerSecond = 10_000_000ul;
        private static readonly ulong[] FallbackTierSeconds = { 300ul, 1800ul, 7200ul, 43200ul };

        public static ulong[] GetTierSeconds(IGameTableManager gameTables = null)
        {
            gameTables ??= LegacyServiceProvider.Provider?.GetService<IGameTableManager>()
                ?? LegacyServiceProvider.Provider?.GetService<GameTableManager>();
            GameFormulaEntry entry = gameTables?.GameFormula?.GetEntry(ListingDurationGameFormulaId);
            if (entry == null)
                return FallbackTierSeconds;

            var tiers = new List<ulong>(4);
            if (entry.Dataint0 > 0u)
                tiers.Add(entry.Dataint0);
            if (entry.Dataint01 > 0u)
                tiers.Add(entry.Dataint01);
            if (entry.Dataint02 > 0u)
                tiers.Add(entry.Dataint02);
            if (entry.Dataint03 > 0u)
                tiers.Add(entry.Dataint03);

            return tiers.Count > 0 ? tiers.ToArray() : FallbackTierSeconds;
        }

        public static bool IsValidTierDurationSeconds(ulong durationSeconds, IGameTableManager gameTables = null) =>
            GetTierSeconds(gameTables).Contains(durationSeconds);
    }
}
