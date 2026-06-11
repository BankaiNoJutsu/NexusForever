using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Resolves marketplace mail localized text from client achievement-text id <c>0x3950</c>
    /// (<c>Game.ItemAuction</c> / <c>Game.CommodityOrder</c> in <c>FUN_140433750</c> / <c>FUN_140433640</c>).
    /// </summary>
    internal static class MarketplaceMailTexts
    {
        /// <summary>Shared achievement-text id for item-auction and commodity mail templates.</summary>
        public const uint MarketplaceMailAchievementTextId = 0x3950u;

        /// <summary>WildStar client DB: achievement text 14672 → localized text 278287.</summary>
        public const uint FallbackMarketplaceLocalizedTextId = 278287u;

        public static bool TryGetMarketplaceMailLocalizedTextId(out uint localizedTextId, IGameTableManager gameTables = null)
        {
            localizedTextId = 0u;
            AchievementTextEntry entry = gameTables?.AchievementText?.GetEntry(MarketplaceMailAchievementTextId);
            if (entry != null && entry.LocalizedTextId != 0u)
            {
                localizedTextId = entry.LocalizedTextId;
                return true;
            }

            localizedTextId = FallbackMarketplaceLocalizedTextId;
            return localizedTextId != 0u;
        }
    }
}
