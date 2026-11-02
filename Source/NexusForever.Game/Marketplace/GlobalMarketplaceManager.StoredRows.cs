using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        // Immutable copies of every mapped scalar. Navigation objects and the
        // mutable order book must never become a loaded SQL preimage.
        internal sealed record AuctionRow(ulong Id, ulong Owner, ulong Item, ulong MinimumBid, ulong Buyout,
            ulong Bid, ulong Bidder, ulong Expiration, uint Definition, uint Quantity, uint Requirement,
            ulong Circuit, uint Glyph, ulong Threshold, uint Unknown, string Microchips)
        {
            internal static AuctionRow Capture(MarketplaceAuctionModel row) => row == null ? null : new(
                row.Id, row.OwnerCharacterId, row.ItemId, row.MinimumBid, row.BuyoutPrice, row.CurrentBid,
                row.TopBidderCharacterId, row.ExpirationTime, row.Item2Id, row.Quantity, row.WorldRequirementItem2Id,
                row.CircuitData, row.GlyphData, row.ThresholdData, row.Unknown2, row.MicrochipIds);

            internal MarketplaceAuctionModel Model() => new()
            {
                Id = Id, OwnerCharacterId = Owner, ItemId = Item, MinimumBid = MinimumBid, BuyoutPrice = Buyout,
                CurrentBid = Bid, TopBidderCharacterId = Bidder, ExpirationTime = Expiration, Item2Id = Definition,
                Quantity = Quantity, WorldRequirementItem2Id = Requirement, CircuitData = Circuit, GlyphData = Glyph,
                ThresholdData = Threshold, Unknown2 = Unknown, MicrochipIds = Microchips
            };
        }

        internal sealed record CommodityRow(ulong Id, ulong Owner, uint Definition, uint Quantity, ulong UnitPrice,
            ulong Price, bool Buy, bool Immediate, ulong Listed, ulong Expiration)
        {
            internal static CommodityRow Capture(MarketplaceCommodityOrderModel row) => row == null ? null : new(
                row.Id, row.OwnerCharacterId, row.Item2Id, row.Quantity, row.PricePerUnit, row.Price,
                row.IsBuyOrder, row.ForceImmediate, row.ListTime, row.ExpirationTime);

            internal MarketplaceCommodityOrderModel Model() => new()
            {
                Id = Id, OwnerCharacterId = Owner, Item2Id = Definition, Quantity = Quantity, PricePerUnit = UnitPrice,
                Price = Price, IsBuyOrder = Buy, ForceImmediate = Immediate, ListTime = Listed, ExpirationTime = Expiration
            };
        }

        // These check the retained live view, not SQL freshness. Require*Row
        // separately compares the loaded preimage with the destination SQL.
        private bool HasConsistentAuctionView(MarketplaceAuction record)
            => TryGetCharacterDatabase() == null || !record.HasStoredOutcome || record.StoredRow != null && record.LiveRow != null
                && record.LiveRow == AuctionRow.Capture(ToAuctionModel(record));

        private bool HasConsistentCommodityView(MarketplaceCommodityOrder record)
            => TryGetCharacterDatabase() == null || !record.HasStoredOutcome || !record.RequiresStoredRow || record.StoredRow != null
                && record.StoredRow == CommodityRow.Capture(ToCommodityOrderModel(record));

        private static MarketplaceAuctionModel RequireAuctionRow(CharacterContext context, MarketplaceAuction record)
        {
            RequireStoredDatabase(context, record.StoredDatabase);
            var actual = context.MarketplaceAuction.SingleOrDefault(row => row.Id == record.Auction.AuctionId);
            if (actual == null)
                throw new InvalidOperationException($"Marketplace auction {record.Auction.AuctionId} is missing from the database.");
            var stored = context.MarketplaceAuction.AsNoTracking().SingleOrDefault(row => row.Id == record.Auction.AuctionId);
            if (record.StoredRow == null || record.StoredRow != AuctionRow.Capture(actual) || record.StoredRow != AuctionRow.Capture(stored))
                throw new InvalidOperationException("Marketplace auction differs from its complete loaded SQL row.");
            return actual;
        }

        private static MarketplaceCommodityOrderModel RequireCommodityRow(CharacterContext context, MarketplaceCommodityOrder record)
        {
            RequireStoredDatabase(context, record.StoredDatabase);
            var actual = context.MarketplaceCommodityOrder.SingleOrDefault(row => row.Id == record.Order.CommodityOrderId);
            if (actual == null)
                throw new InvalidOperationException($"Marketplace commodity order {record.Order.CommodityOrderId} is missing from the database.");
            var stored = context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefault(row => row.Id == record.Order.CommodityOrderId);
            if (!record.RequiresStoredRow || record.StoredRow == null || record.StoredRow != CommodityRow.Capture(actual) || record.StoredRow != CommodityRow.Capture(stored))
                throw new InvalidOperationException("Marketplace commodity order differs from its complete loaded SQL row.");
            return actual;
        }

        private static void RequireStoredDatabase(CharacterContext context, string expected)
        {
            if (expected == null || GameItem.GetRewardDatabaseKey(context) != expected)
                throw new InvalidOperationException("Marketplace row changed its retained database route.");
        }

        private static void ObserveAuctionRow(CharacterContext context, MarketplaceAuction record, MarketplaceAuctionModel target)
        {
            var captured = AuctionRow.Capture(target);
            ulong id = record.Auction.AuctionId;
            var model = target ?? context.ChangeTracker.Entries<MarketplaceAuctionModel>().Single(entry => entry.Entity.Id == id).Entity;
            ObserveListingWrite(context, model, target == null, AuctionRow.Capture,
                () => AuctionRow.Capture(context.MarketplaceAuction.AsNoTracking().SingleOrDefault(row => row.Id == id)));
            string database = GameItem.GetRewardDatabaseKey(context);
            context.OnSaveSettled(committed =>
            {
                if (!committed) return;
                record.StoredRow = captured; record.LiveRow = captured; record.HasStoredOutcome = true; record.StoredDatabase = database;
            });
        }

        private static void ObserveCommodityRow(CharacterContext context, MarketplaceCommodityOrder record, MarketplaceCommodityOrderModel target)
        {
            var captured = CommodityRow.Capture(target);
            ulong id = record.Order.CommodityOrderId;
            var model = target ?? context.ChangeTracker.Entries<MarketplaceCommodityOrderModel>().Single(entry => entry.Entity.Id == id).Entity;
            ObserveListingWrite(context, model, target == null, CommodityRow.Capture,
                () => CommodityRow.Capture(context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefault(row => row.Id == id)));
            string database = GameItem.GetRewardDatabaseKey(context);
            context.OnSaveSettled(committed =>
            {
                if (!committed) return;
                record.StoredRow = captured; record.HasStoredOutcome = true; record.RequiresStoredRow = true; record.StoredDatabase = database;
            });
        }
    }
}
