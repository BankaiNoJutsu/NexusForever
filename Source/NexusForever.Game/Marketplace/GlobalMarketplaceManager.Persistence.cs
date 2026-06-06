using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private bool PersistAuctionInsert(MarketplaceAuction record)
        {
            return Persist(context =>
            {
                if (record.Item is Item item)
                    item.Save(context);

                context.MarketplaceAuction.Add(ToAuctionModel(record));
            });
        }

        private bool PersistAuctionUpdate(MarketplaceAuction record)
        {
            return Persist(context =>
            {
                MarketplaceAuctionModel model = context.MarketplaceAuction.SingleOrDefault(a => a.Id == record.Auction.AuctionId);
                if (model == null)
                {
                    log.Error("PersistAuctionUpdate failed: auction id={0} missing from database.", record.Auction.AuctionId);
                    return;
                }

                ApplyAuctionModel(model, record.Auction, record.ExpiresAtUtc);
            });
        }

        private bool PersistCommodityOrderUpdate(MarketplaceCommodityOrder record)
        {
            return Persist(context =>
            {
                MarketplaceCommodityOrderModel model = context.MarketplaceCommodityOrder
                    .SingleOrDefault(o => o.Id == record.Order.CommodityOrderId);
                if (model == null)
                {
                    log.Error("PersistCommodityOrderUpdate failed: commodity order id={0} missing from database.", record.Order.CommodityOrderId);
                    return;
                }

                model.Quantity     = record.Order.Quantity;
                model.Price        = record.Order.Price;
                model.PricePerUnit = record.Order.PricePerUnit;
            });
        }

        private bool PersistCommodityOrderUpdate(MarketplaceCommodityOrder record, bool alreadyPersisted)
        {
            return alreadyPersisted || PersistCommodityOrderUpdate(record);
        }

        private void PersistCommodityFillOrderChanges(
            CharacterContext context,
            MarketplaceCommodityOrder buyOrder,
            uint buyRemainingQuantity,
            ulong buyRemainingPrice,
            MarketplaceCommodityOrder sellOrder,
            uint sellRemainingQuantity,
            ulong sellRemainingPrice)
        {
            PersistCommodityFillOrderChange(context, buyOrder, buyRemainingQuantity, buyRemainingPrice);
            PersistCommodityFillOrderChange(context, sellOrder, sellRemainingQuantity, sellRemainingPrice);
        }

        private bool TryPersistCommodityFillOrderChanges(
            MarketplaceCommodityOrder buyOrder,
            uint buyRemainingQuantity,
            ulong buyRemainingPrice,
            MarketplaceCommodityOrder sellOrder,
            uint sellRemainingQuantity,
            ulong sellRemainingPrice)
        {
            return Persist(context => PersistCommodityFillOrderChanges(
                context,
                buyOrder,
                buyRemainingQuantity,
                buyRemainingPrice,
                sellOrder,
                sellRemainingQuantity,
                sellRemainingPrice));
        }

        private void PersistCommodityFillOrderChange(
            CharacterContext context,
            MarketplaceCommodityOrder record,
            uint remainingQuantity,
            ulong remainingPrice)
        {
            if (!IsRestingCommodityOrder(record))
                return;

            if (remainingQuantity == 0u)
            {
                RemoveCommodityOrderModel(context, record);
                return;
            }

            MarketplaceCommodityOrderModel model = context.MarketplaceCommodityOrder
                .SingleOrDefault(o => o.Id == record.Order.CommodityOrderId);
            if (model == null)
            {
                log.Error("PersistCommodityFillOrderChange failed: commodity order id={0} missing from database.", record.Order.CommodityOrderId);
                return;
            }

            model.Quantity     = remainingQuantity;
            model.Price        = remainingPrice;
            model.PricePerUnit = record.Order.PricePerUnit;
        }

        private bool PersistAuctionDelete(MarketplaceAuction record)
        {
            return Persist(context =>
            {
                RemoveAuctionModel(context, record);
                SaveSettledAuctionItem(context, record.Item);
            });
        }

        private static void RemoveAuctionModel(CharacterContext context, MarketplaceAuction record)
        {
            MarketplaceAuctionModel model = context.MarketplaceAuction.SingleOrDefault(a => a.Id == record.Auction.AuctionId);
            if (model != null)
                context.MarketplaceAuction.Remove(model);
        }

        private static void SaveSettledAuctionItem(CharacterContext context, IItem item)
        {
            item?.Save(context);
        }

        private bool PersistCommodityOrderInsert(MarketplaceCommodityOrder record)
        {
            return Persist(context => context.MarketplaceCommodityOrder.Add(ToCommodityOrderModel(record)));
        }

        private bool PersistCommodityOrderDelete(MarketplaceCommodityOrder record)
        {
            return Persist(context => RemoveCommodityOrderModel(context, record));
        }

        private static void RemoveCommodityOrderModel(CharacterContext context, MarketplaceCommodityOrder record)
        {
            MarketplaceCommodityOrderModel model = context.MarketplaceCommodityOrder
                .SingleOrDefault(o => o.Id == record.Order.CommodityOrderId);
            if (model != null)
                context.MarketplaceCommodityOrder.Remove(model);
        }

        private bool Persist(Action<CharacterContext> action)
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
            {
                log.Warn("GlobalMarketplaceManager.Persist skipped: CharacterDatabase is null. Operating in in-memory-only marketplace mode.");
                return true;
            }

            try
            {
                database.SaveBlocking(action);
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "GlobalMarketplaceManager.Persist failed. Rolling back in-memory marketplace mutation where possible.");
                return false;
            }
        }

        private CharacterDatabase TryGetCharacterDatabase()
        {
            IDatabaseManager manager = databaseManager
                ?? LegacyServiceProvider.Provider?.GetService<IDatabaseManager>()
                ?? LegacyServiceProvider.Provider?.GetService<DatabaseManager>();

            return manager?.GetDatabase<CharacterDatabase>();
        }
    }
}
