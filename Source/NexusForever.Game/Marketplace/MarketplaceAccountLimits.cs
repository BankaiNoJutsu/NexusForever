using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Retail build 16042 auction house and commodity exchange slot limits (F2P era, not sunset buffs).
    /// </summary>
    public static class MarketplaceAccountLimits
    {
        public const int RetailFreeSlotLimit = 3;
        public const int RetailSignatureSlotLimit = 30;

        /// <summary>Store SKU "Auctions and Bids Limit (+50)" per stack.</summary>
        public const int ExtraAuctionEntitlementSlotsPerStack = 50;

        /// <summary>Store SKU commodity order limit (+50) per stack.</summary>
        public const int ExtraCommodityEntitlementSlotsPerStack = 50;

        /// <summary>Cosmic / loyalty AH bonus (+10 per tier on F2P wiki).</summary>
        public const int LoyaltyExtraAuctionSlotsPerStack = 10;

        /// <summary>Cosmic / loyalty CX bonus (+10 per tier on F2P wiki).</summary>
        public const int LoyaltyExtraCommoditySlotsPerStack = 10;

        public static int GetMaxAuctionSellLots(IPlayer player) =>
            GetAuctionSlotLimit(player);

        public static int GetMaxAuctionBids(IPlayer player) =>
            GetAuctionSlotLimit(player);

        public static int GetMaxCommoditySellOrders(IPlayer player) =>
            GetCommoditySlotLimit(player);

        public static int GetMaxCommodityBuyOrders(IPlayer player) =>
            GetCommoditySlotLimit(player);

        private static int GetAuctionSlotLimit(IPlayer player)
        {
            if (player == null)
                return 0;

            int limit = player.SignatureEnabled ? RetailSignatureSlotLimit : RetailFreeSlotLimit;
            IAccount account = player.Account;
            if (account == null)
                return limit;

            limit += GetEntitlementBonus(account, EntitlementType.ExtraAuctions, ExtraAuctionEntitlementSlotsPerStack);
            limit += GetEntitlementBonus(account, EntitlementType.LoyaltyExtraAuctions, LoyaltyExtraAuctionSlotsPerStack);
            return limit;
        }

        private static int GetCommoditySlotLimit(IPlayer player)
        {
            if (player == null)
                return 0;

            int limit = player.SignatureEnabled ? RetailSignatureSlotLimit : RetailFreeSlotLimit;
            IAccount account = player.Account;
            if (account == null)
                return limit;

            limit += GetEntitlementBonus(account, EntitlementType.ExtraCommodityOrders, ExtraCommodityEntitlementSlotsPerStack);
            limit += GetEntitlementBonus(account, EntitlementType.LoyaltyExtraCommodityOrders, LoyaltyExtraCommoditySlotsPerStack);
            return limit;
        }

        private static int GetEntitlementBonus(IAccount account, EntitlementType type, int slotsPerStack)
        {
            uint stacks = account.EntitlementManager?.GetEntitlement(type)?.Amount ?? 0u;
            return checked((int)(stacks * (uint)slotsPerStack));
        }
    }
}
