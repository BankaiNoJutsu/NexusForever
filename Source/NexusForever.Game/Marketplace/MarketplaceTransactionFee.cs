using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Auction-house transaction fee helpers backed by <c>wildstar_client.GameFormula</c> rows
    /// <c>436</c>/<c>437</c> (<c>Datafloat0 = 0.05</c>) with conservative fallbacks.
    /// </summary>
    internal static class MarketplaceTransactionFee
    {
        private const uint ItemAuctionFeeGameFormulaId = 436u;
        private const uint CommodityAuctionFeeGameFormulaId = 437u;
        private const uint MinimumFeeGameFormulaId = 1090u;
        private const float DefaultFeeRate = 0.05f;
        private const ulong FallbackMinimumFeeCredits = 500ul;
        private const ulong CreditsPerMinimumFeeUnit = 100ul;

        public static ulong CalculateItemAuctionSellerProceeds(ulong grossAmount) =>
            CalculateSellerProceeds(grossAmount, ItemAuctionFeeGameFormulaId);

        public static ulong CalculateCommoditySellerProceeds(ulong grossAmount) =>
            CalculateSellerProceeds(grossAmount, CommodityAuctionFeeGameFormulaId);

        private static ulong CalculateSellerProceeds(ulong grossAmount, uint feeFormulaId)
        {
            if (grossAmount == 0ul)
                return 0ul;

            float feeRate = GetFeeRate(feeFormulaId);
            ulong fee = (ulong)Math.Ceiling(grossAmount * feeRate);
            ulong minimumFee = GetMinimumFeeCredits();
            if (grossAmount >= minimumFee)
                fee = Math.Max(fee, minimumFee);

            if (fee > grossAmount)
                fee = grossAmount;

            return grossAmount - fee;
        }

        private static float GetFeeRate(uint feeFormulaId)
        {
            GameTableManager gameTables = LegacyServiceProvider.Provider?.GetService<GameTableManager>();
            GameFormulaEntry entry = gameTables?.GameFormula?.GetEntry(feeFormulaId);
            if (entry == null || entry.Datafloat0 <= 0f)
                return DefaultFeeRate;

            return entry.Datafloat0;
        }

        private static ulong GetMinimumFeeCredits()
        {
            GameTableManager gameTables = LegacyServiceProvider.Provider?.GetService<GameTableManager>();
            GameFormulaEntry entry = gameTables?.GameFormula?.GetEntry(MinimumFeeGameFormulaId);
            if (entry != null && entry.Dataint0 > 0u)
                return entry.Dataint0 * CreditsPerMinimumFeeUnit;

            return FallbackMinimumFeeCredits;
        }
    }
}
