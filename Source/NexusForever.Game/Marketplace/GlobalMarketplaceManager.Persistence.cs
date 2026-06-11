using System;
using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private const string MarketplaceCreditSubject = "Marketplace";
        private const string MarketplaceCreditBody    = "Credits from the marketplace are enclosed.";

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
            return Persist(context => PersistAuctionUpdate(context, record));
        }

        private bool PersistAuctionUpdateWithOfflineCredit(
            MarketplaceAuction record,
            ulong characterId,
            ulong amount,
            IPlayer onlineOwner)
        {
            if (!RequiresOfflineCreditPersistence(amount, onlineOwner))
                return PersistAuctionUpdate(record);

            return PersistRequired(context =>
            {
                PersistAuctionUpdate(context, record);
                SaveOfflineCredit(context, characterId, CurrencyType.Credits, amount);
            });
        }

        private void PersistAuctionUpdate(CharacterContext context, MarketplaceAuction record)
        {
            MarketplaceAuctionModel model = context.MarketplaceAuction.SingleOrDefault(a => a.Id == record.Auction.AuctionId);
            if (model == null)
            {
                log.Error("PersistAuctionUpdate failed: auction id={0} missing from database.", record.Auction.AuctionId);
                return;
            }

            ApplyAuctionModel(model, record.Auction, record.ExpiresAtUtc);
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

        private bool TryPersistCommodityFillOrderChangesWithOfflineCredits(
            MarketplaceCommodityOrder buyOrder,
            uint buyRemainingQuantity,
            ulong buyRemainingPrice,
            MarketplaceCommodityOrder sellOrder,
            uint sellRemainingQuantity,
            ulong sellRemainingPrice,
            ulong sellerProceeds,
            IPlayer onlineSeller,
            ulong buyerRefund,
            IPlayer onlineBuyer)
        {
            void SaveAction(CharacterContext context)
            {
                PersistCommodityFillOrderChangesWithOfflineCredits(
                    context,
                    buyOrder,
                    buyRemainingQuantity,
                    buyRemainingPrice,
                    sellOrder,
                    sellRemainingQuantity,
                    sellRemainingPrice,
                    sellerProceeds,
                    onlineSeller,
                    buyerRefund,
                    onlineBuyer);
            }

            if (RequiresOfflineCreditPersistence(sellerProceeds, onlineSeller)
                || RequiresOfflineCreditPersistence(buyerRefund, onlineBuyer))
            {
                return PersistRequired(SaveAction);
            }

            return Persist(SaveAction);
        }

        private void PersistCommodityFillOrderChangesWithOfflineCredits(
            CharacterContext context,
            MarketplaceCommodityOrder buyOrder,
            uint buyRemainingQuantity,
            ulong buyRemainingPrice,
            MarketplaceCommodityOrder sellOrder,
            uint sellRemainingQuantity,
            ulong sellRemainingPrice,
            ulong sellerProceeds,
            IPlayer onlineSeller,
            ulong buyerRefund,
            IPlayer onlineBuyer)
        {
            PersistCommodityFillOrderChanges(
                context,
                buyOrder,
                buyRemainingQuantity,
                buyRemainingPrice,
                sellOrder,
                sellRemainingQuantity,
                sellRemainingPrice);

            SaveOfflineCreditIfOwnerOffline(context, sellOrder.OwnerCharacterId, sellerProceeds, onlineSeller);
            SaveOfflineCreditIfOwnerOffline(context, buyOrder.OwnerCharacterId, buyerRefund, onlineBuyer);
        }

        private static bool RequiresOfflineCreditPersistence(ulong amount, IPlayer onlineOwner)
        {
            return amount != 0ul && onlineOwner == null;
        }

        private void SaveOfflineCreditIfOwnerOffline(
            CharacterContext context,
            ulong characterId,
            ulong amount,
            IPlayer onlineOwner)
        {
            if (characterId == 0ul || !RequiresOfflineCreditPersistence(amount, onlineOwner))
                return;

            SaveOfflineCredit(context, characterId, CurrencyType.Credits, amount);
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

        private bool PersistAuctionDeleteWithSellerCredit(MarketplaceAuction record, ulong grossAmount, IPlayer onlineSeller)
        {
            return PersistAuctionDeleteWithCredits(record, grossAmount, onlineSeller, 0ul, 0ul, null);
        }

        private bool PersistAuctionDeleteWithBidderRefund(
            MarketplaceAuction record,
            ulong refundCharacterId,
            ulong refundAmount,
            IPlayer onlineRefundOwner)
        {
            return PersistAuctionDeleteWithCredits(record, 0ul, null, refundCharacterId, refundAmount, onlineRefundOwner);
        }

        private bool PersistAuctionDeleteWithCredits(
            MarketplaceAuction record,
            ulong grossAmount,
            IPlayer onlineSeller,
            ulong refundCharacterId,
            ulong refundAmount,
            IPlayer onlineRefundOwner)
        {
            ulong proceeds = MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(grossAmount, GetGameTableManager());
            if (!RequiresOfflineCreditPersistence(proceeds, onlineSeller)
                && !RequiresOfflineCreditPersistence(refundAmount, onlineRefundOwner))
            {
                return PersistAuctionDelete(record);
            }

            return PersistRequired(context => RemoveAuctionModelAndCreditOfflineParticipants(
                context,
                record,
                grossAmount,
                saveItem: true,
                onlineSeller,
                refundCharacterId,
                refundAmount,
                onlineRefundOwner));
        }

        private void RemoveAuctionModelAndCreditOfflineParticipants(
            CharacterContext context,
            MarketplaceAuction record,
            ulong grossAmount,
            bool saveItem,
            IPlayer onlineSeller,
            ulong refundCharacterId,
            ulong refundAmount,
            IPlayer onlineRefundOwner)
        {
            RemoveAuctionModel(context, record);
            if (saveItem)
                SaveSettledAuctionItem(context, record.Item);

            CreditOfflineSeller(context, record, grossAmount, onlineSeller);
            SaveOfflineCreditIfOwnerOffline(context, refundCharacterId, refundAmount, onlineRefundOwner);
        }

        private void CreditOfflineSeller(CharacterContext context, MarketplaceAuction record, ulong grossAmount, IPlayer onlineSeller)
        {
            if (grossAmount == 0ul)
                return;

            ulong proceeds = MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(grossAmount, GetGameTableManager());
            SaveOfflineCreditIfOwnerOffline(context, record.Auction.OwnerCharacterId, proceeds, onlineSeller);
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

        private bool PersistCommodityOrderDeleteWithOfflineCredit(
            MarketplaceCommodityOrder record,
            CurrencyType currencyType,
            ulong amount,
            IPlayer onlineOwner)
        {
            if (amount == 0ul || onlineOwner != null)
                return PersistCommodityOrderDelete(record);

            return PersistRequired(context =>
            {
                RemoveCommodityOrderModel(context, record);
                SaveOfflineCredit(context, record.OwnerCharacterId, currencyType, amount);
            });
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

        private bool PersistRequired(Action<CharacterContext> action)
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
            {
                log.Warn("GlobalMarketplaceManager.PersistRequired failed: CharacterDatabase is null. Offline marketplace settlement cannot be completed in memory-only mode.");
                return false;
            }

            try
            {
                database.SaveBlocking(action);
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "GlobalMarketplaceManager.PersistRequired failed. Rolling back in-memory marketplace mutation where possible.");
                return false;
            }
        }

        private void SaveOfflineCredit(
            CharacterContext context,
            ulong characterId,
            CurrencyType currencyType,
            ulong amount)
        {
            if (amount == 0ul)
                return;

            if (currencyType == CurrencyType.Credits)
            {
                MarketplaceMailDelivery.SaveMarketplaceCreditMail(
                    context,
                    characterId,
                    amount,
                    MarketplaceCreditSubject,
                    MarketplaceCreditBody,
                    GetGameTableManager(),
                    assetManager);
                return;
            }

            byte currencyId = (byte)currencyType;
            CharacterCurrencyModel currency = context.CharacterCurrency
                .FirstOrDefault(c => c.Id == characterId && c.CurrencyId == currencyId);
            if (currency == null)
            {
                context.CharacterCurrency.Add(new CharacterCurrencyModel
                {
                    Id         = characterId,
                    CurrencyId = currencyId,
                    Amount     = amount
                });
            }
            else
                currency.Amount += amount;
        }

        private CharacterDatabase TryGetCharacterDatabase()
        {
            return databaseManager?.GetDatabase<CharacterDatabase>();
        }
    }
}
