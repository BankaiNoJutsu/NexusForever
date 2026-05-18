using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Marketplace;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model.Marketplace.Filter;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientRequestCommodityInfoHandler : IMessageHandler<IWorldSession, ClientRequestCommodityInfo>
    {
        private readonly ILogger<ClientRequestCommodityInfoHandler> log;
        private readonly IItemManager itemManager;

        public ClientRequestCommodityInfoHandler(
            ILogger<ClientRequestCommodityInfoHandler> log,
            IItemManager itemManager)
        {
            this.log         = log;
            this.itemManager = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientRequestCommodityInfo request)
        {
            MarketplaceRequestHelper.ValidateItem2(itemManager, request.Item2Id);

            MarketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning commodity info for marketplace request from player {PlayerGuid}: item {Item2Id}.",
                session.Player?.Guid, request.Item2Id);
            session.EnqueueMessageEncrypted(MarketplaceRequestHelper.BuildCommodityInfo(request.Item2Id));
        }
    }

    public class ClientRequestOwnedCommodityOrdersHandler : IMessageHandler<IWorldSession, ClientRequestOwnedCommodityOrders>
    {
        private readonly ILogger<ClientRequestOwnedCommodityOrdersHandler> log;

        public ClientRequestOwnedCommodityOrdersHandler(ILogger<ClientRequestOwnedCommodityOrdersHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRequestOwnedCommodityOrders request)
        {
            MarketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning owned commodity orders for marketplace request from player {PlayerGuid}.",
                session.Player?.Guid);
            session.EnqueueMessageEncrypted(new ServerOwnedCommodityOrders
            {
                Orders = MarketplaceRequestHelper.GetOwnedCommodityOrders(session)
            });
        }
    }

    public class ClientRequestOwnedItemAuctionsHandler : IMessageHandler<IWorldSession, ClientRequestOwnedItemAuctions>
    {
        private readonly ILogger<ClientRequestOwnedItemAuctionsHandler> log;

        public ClientRequestOwnedItemAuctionsHandler(ILogger<ClientRequestOwnedItemAuctionsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRequestOwnedItemAuctions request)
        {
            MarketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning owned item auctions for marketplace request from player {PlayerGuid}.",
                session.Player?.Guid);
            session.EnqueueMessageEncrypted(new ServerOwnedItemAuctions
            {
                Auctions = MarketplaceRequestHelper.GetOwnedItemAuctions(session)
            });
        }
    }

    public class ClientAuctionsByFilterRequestHandler : IMessageHandler<IWorldSession, ClientAuctionsByFilterRequest>
    {
        private readonly ILogger<ClientAuctionsByFilterRequestHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;

        public ClientAuctionsByFilterRequestHandler(
            ILogger<ClientAuctionsByFilterRequestHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
            this.itemManager      = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionsByFilterRequest request)
        {
            MarketplaceRequestHelper.ValidateAuctionSearch(gameTableManager, itemManager, request);

            MarketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning auction search results for marketplace request from player {PlayerGuid}: page {Page}.",
                session.Player?.Guid, request.Page);
            session.EnqueueMessageEncrypted(MarketplaceRequestHelper.SearchAuctions(itemManager, request));
        }
    }

    public class ClientAuctionBuyOrderSubmitHandler : IMessageHandler<IWorldSession, ClientAuctionBuyOrderSubmit>
    {
        private readonly ILogger<ClientAuctionBuyOrderSubmitHandler> log;
        private readonly IItemManager itemManager;

        public ClientAuctionBuyOrderSubmitHandler(
            ILogger<ClientAuctionBuyOrderSubmitHandler> log,
            IItemManager itemManager)
        {
            this.log         = log;
            this.itemManager = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionBuyOrderSubmit buyOrderSubmit)
        {
            MarketplaceRequestHelper.ValidateItem2(itemManager, buyOrderSubmit.Item2Id);
            if (buyOrderSubmit.AuctionId == 0ul || buyOrderSubmit.AmountOffered == 0ul)
                throw new InvalidPacketValueException();

            MarketplaceRequestHelper.SendEnabledStatus(session);

            GenericError result = MarketplaceRequestHelper.BuyAuction(session, buyOrderSubmit, out AuctionInfo auction);
            log.LogDebug("Processed auction buy request from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, amount {AmountOffered}, result {Result}.",
                session.Player?.Guid, buyOrderSubmit.AuctionId, buyOrderSubmit.Item2Id, buyOrderSubmit.AmountOffered, result);
            session.EnqueueMessageEncrypted(new ServerAuctionBidResult
            {
                Result  = result,
                Auction = auction
            });
        }
    }

    public class ClientAuctionSellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientAuctionSellOrderSubmit>
    {
        private readonly ILogger<ClientAuctionSellOrderSubmitHandler> log;

        public ClientAuctionSellOrderSubmitHandler(ILogger<ClientAuctionSellOrderSubmitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionSellOrderSubmit sellOrderSubmit)
        {
            IItem item = MarketplaceRequestHelper.GetInventoryItem(session, sellOrderSubmit.ItemGuid);
            if (sellOrderSubmit.MinimumBid == 0ul || (sellOrderSubmit.BuyoutPrice != 0ul && sellOrderSubmit.BuyoutPrice < sellOrderSubmit.MinimumBid))
                throw new InvalidPacketValueException();

            MarketplaceRequestHelper.SendEnabledStatus(session);

            GenericError result = MarketplaceRequestHelper.PostAuction(session, item, sellOrderSubmit.MinimumBid, sellOrderSubmit.BuyoutPrice, out AuctionInfo auction);
            log.LogDebug("Processed auction sell request from player {PlayerGuid}: item {ItemGuid}, minimum bid {MinimumBid}, buyout {BuyoutPrice}, result {Result}.",
                session.Player?.Guid, sellOrderSubmit.ItemGuid, sellOrderSubmit.MinimumBid, sellOrderSubmit.BuyoutPrice, result);
            session.EnqueueMessageEncrypted(new ServerAuctionPostResult
            {
                Result  = result,
                Auction = auction
            });
        }
    }

    public class ClientAuctionCancelHandler : IMessageHandler<IWorldSession, ClientAuctionCancel>
    {
        private readonly ILogger<ClientAuctionCancelHandler> log;
        private readonly IItemManager itemManager;

        public ClientAuctionCancelHandler(
            ILogger<ClientAuctionCancelHandler> log,
            IItemManager itemManager)
        {
            this.log         = log;
            this.itemManager = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionCancel auctionCancel)
        {
            MarketplaceRequestHelper.ValidateItem2(itemManager, auctionCancel.Item2Id);
            if (auctionCancel.AuctionId == 0ul)
                throw new InvalidPacketValueException();

            MarketplaceRequestHelper.SendEnabledStatus(session);

            GenericError result = MarketplaceRequestHelper.CancelAuction(session, auctionCancel.AuctionId, auctionCancel.Item2Id, out AuctionInfo auction);
            log.LogDebug("Processed auction cancel request from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, result {Result}.",
                session.Player?.Guid, auctionCancel.AuctionId, auctionCancel.Item2Id, result);
            session.EnqueueMessageEncrypted(new ServerAuctionCancelResult
            {
                Result  = result,
                Auction = auction
            });
        }
    }

    public class ClientCommoditySellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCommoditySellOrderSubmit>
    {
        private readonly ILogger<ClientCommoditySellOrderSubmitHandler> log;
        private readonly IItemManager itemManager;

        public ClientCommoditySellOrderSubmitHandler(
            ILogger<ClientCommoditySellOrderSubmitHandler> log,
            IItemManager itemManager)
        {
            this.log         = log;
            this.itemManager = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientCommoditySellOrderSubmit sellOrderSubmit)
        {
            MarketplaceRequestHelper.ValidateCommodityOrder(itemManager, sellOrderSubmit.Order);

            MarketplaceRequestHelper.SendEnabledStatus(session);

            GenericError result = MarketplaceRequestHelper.PostCommodityOrder(session, sellOrderSubmit.Order, out CommodityOrder postedOrder);
            log.LogDebug("Processed commodity order request from player {PlayerGuid}: item {Item2Id}, quantity {Quantity}, buy order {IsBuyOrder}, result {Result}.",
                session.Player?.Guid, sellOrderSubmit.Order.Item2Id, sellOrderSubmit.Order.Quantity, sellOrderSubmit.Order.IsBuyOrder, result);
            session.EnqueueMessageEncrypted(new ServerCommodityOrderResult
            {
                Result      = result,
                OrderPosted = postedOrder
            });
        }
    }

    public class ClientCommodityOrderCancelHandler : IMessageHandler<IWorldSession, ClientCommodityOrderCancel>
    {
        private readonly ILogger<ClientCommodityOrderCancelHandler> log;
        private readonly IItemManager itemManager;

        public ClientCommodityOrderCancelHandler(
            ILogger<ClientCommodityOrderCancelHandler> log,
            IItemManager itemManager)
        {
            this.log         = log;
            this.itemManager = itemManager;
        }

        public void HandleMessage(IWorldSession session, ClientCommodityOrderCancel orderCancel)
        {
            MarketplaceRequestHelper.ValidateItem2(itemManager, orderCancel.Item2Id);
            if (orderCancel.CommodityOrderId == 0ul)
                throw new InvalidPacketValueException();

            MarketplaceRequestHelper.SendEnabledStatus(session);

            bool cancelled = MarketplaceRequestHelper.CancelCommodityOrder(session, orderCancel.CommodityOrderId, orderCancel.Item2Id, orderCancel.IsBuyOrder);
            log.LogDebug("Processed commodity order cancel request from player {PlayerGuid}: order {CommodityOrderId}, item {Item2Id}, buy order {IsBuyOrder}, cancelled {Cancelled}.",
                session.Player?.Guid, orderCancel.CommodityOrderId, orderCancel.Item2Id, orderCancel.IsBuyOrder, cancelled);
        }
    }

    internal static class MarketplaceRequestHelper
    {
        private const uint MaxAuctionFilters = 7u;
        private const uint AuctionPageSize = 50u;
        private const ulong DefaultAuctionExpirationSeconds = 172800ul;

        private static readonly object syncRoot = new();
        private static readonly List<MarketplaceAuction> auctions = [];
        private static readonly List<MarketplaceCommodityOrder> commodityOrders = [];
        private static ulong nextAuctionId = 1ul;
        private static ulong nextCommodityOrderId = 1ul;

        public static ServerCommodityInfoResults BuildCommodityInfo(uint item2Id)
        {
            lock (syncRoot)
            {
                List<MarketplaceCommodityOrder> itemOrders = commodityOrders
                    .Where(o => o.Order.Item2Id == item2Id)
                    .ToList();

                return new ServerCommodityInfoResults
                {
                    Item2Id          = item2Id,
                    BuyOrderCount    = (uint)itemOrders.Count(o => o.Order.IsBuyOrder),
                    BuyOrderPrices   = BuildCommodityPriceBuckets(itemOrders.Where(o => o.Order.IsBuyOrder), true),
                    SellOrderCount   = (uint)itemOrders.Count(o => !o.Order.IsBuyOrder),
                    SellOrderPrices  = BuildCommodityPriceBuckets(itemOrders.Where(o => !o.Order.IsBuyOrder), false),
                    Orders           = itemOrders.Select(o => CloneOrder(o.Order)).ToList()
                };
            }
        }

        public static List<CommodityOrder> GetOwnedCommodityOrders(IWorldSession session)
        {
            if (session.Player == null)
                return [];

            lock (syncRoot)
            {
                return commodityOrders
                    .Where(o => o.OwnerCharacterId == session.Player.CharacterId)
                    .Select(o => CloneOrder(o.Order))
                    .ToList();
            }
        }

        public static List<AuctionInfo> GetOwnedItemAuctions(IWorldSession session)
        {
            if (session.Player == null)
                return [];

            lock (syncRoot)
            {
                return auctions
                    .Where(a => a.Auction.OwnerCharacterId == session.Player.CharacterId)
                    .Select(a => CloneAuction(a.Auction))
                    .ToList();
            }
        }

        public static ServerAuctionSearchResults SearchAuctions(IItemManager itemManager, ClientAuctionsByFilterRequest request)
        {
            lock (syncRoot)
            {
                List<AuctionInfo> results = auctions
                    .Where(a => MatchesAuctionRequest(itemManager, a, request))
                    .Select(a => CloneAuction(a.Auction))
                    .ToList();

                results = SortAuctions(results, request);

                int skip = checked((int)(request.Page * AuctionPageSize));
                return new ServerAuctionSearchResults
                {
                    TotalResultCount = (uint)results.Count,
                    CurrentPage      = request.Page,
                    Auctions         = results.Skip(skip).Take((int)AuctionPageSize).ToList()
                };
            }
        }

        public static GenericError PostAuction(IWorldSession session, IItem item, ulong minimumBid, ulong buyoutPrice, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                Item2Id     = item.Id,
                Quantity    = item.StackCount,
                MinimumBid  = minimumBid,
                BuyoutPrice = buyoutPrice
            };

            if (session.Player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                auction.AuctionId        = nextAuctionId++;
                auction.OwnerCharacterId = session.Player.CharacterId;
                auction.ExpirationTime   = DefaultAuctionExpirationSeconds;

                session.Player.Inventory.ItemRemove(item, ItemUpdateReason.Auction);
                auctions.Add(new MarketplaceAuction
                {
                    Auction = CloneAuction(auction),
                    Item    = item
                });
            }

            return GenericError.Ok;
        }

        public static GenericError BuyAuction(IWorldSession session, ClientAuctionBuyOrderSubmit buyOrderSubmit, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                AuctionId  = buyOrderSubmit.AuctionId,
                Item2Id    = buyOrderSubmit.Item2Id,
                CurrentBid = buyOrderSubmit.AmountOffered
            };

            if (session.Player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                MarketplaceAuction record = auctions.FirstOrDefault(a => a.Auction.AuctionId == buyOrderSubmit.AuctionId);
                if (record == null || record.Auction.Item2Id != buyOrderSubmit.Item2Id)
                    return GenericError.ItemBadId;

                if (record.Auction.OwnerCharacterId == session.Player.CharacterId)
                {
                    auction = CloneAuction(record.Auction);
                    return GenericError.Params;
                }

                ulong minimumOffer = record.Auction.CurrentBid == 0ul ? record.Auction.MinimumBid : record.Auction.CurrentBid + 1ul;
                bool isBuyout = record.Auction.BuyoutPrice != 0ul && buyOrderSubmit.AmountOffered >= record.Auction.BuyoutPrice;
                ulong acceptedAmount = isBuyout ? record.Auction.BuyoutPrice : buyOrderSubmit.AmountOffered;
                if (acceptedAmount < minimumOffer)
                {
                    auction = CloneAuction(record.Auction);
                    return GenericError.Params;
                }

                if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, acceptedAmount))
                {
                    auction = CloneAuction(record.Auction);
                    return GenericError.VendorNotEnoughCash;
                }

                if (isBuyout && session.Player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) == 0u)
                {
                    auction = CloneAuction(record.Auction);
                    return GenericError.ItemInventoryFull;
                }

                RefundTopBidder(record);
                session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, acceptedAmount);
                record.Auction.CurrentBid = acceptedAmount;
                record.Auction.TopBidderCharacterId = session.Player.CharacterId;

                if (isBuyout)
                {
                    auctions.Remove(record);
                    PaySeller(record, acceptedAmount);
                    record.Item.CharacterId = session.Player.CharacterId;
                    session.Player.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
                    session.EnqueueMessageEncrypted(new ServerAuctionWon
                    {
                        Auction = CloneAuction(record.Auction)
                    });
                }

                auction = CloneAuction(record.Auction);
                return GenericError.Ok;
            }
        }

        public static GenericError CancelAuction(IWorldSession session, ulong auctionId, uint item2Id, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                AuctionId = auctionId,
                Item2Id   = item2Id
            };

            if (session.Player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                MarketplaceAuction record = auctions.FirstOrDefault(a => a.Auction.AuctionId == auctionId);
                if (record == null || record.Auction.Item2Id != item2Id)
                    return GenericError.ItemBadId;

                auction = CloneAuction(record.Auction);
                if (record.Auction.OwnerCharacterId != session.Player.CharacterId)
                    return GenericError.Params;

                if (session.Player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) == 0u)
                    return GenericError.ItemInventoryFull;

                auctions.Remove(record);
                RefundTopBidder(record);
                record.Item.CharacterId = session.Player.CharacterId;
                session.Player.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
                return GenericError.Ok;
            }
        }

        public static GenericError PostCommodityOrder(IWorldSession session, CommodityOrder order, out CommodityOrder postedOrder)
        {
            postedOrder = CloneOrder(order);
            if (session.Player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                if (order.IsBuyOrder)
                {
                    if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, order.Price))
                        return GenericError.VendorNotEnoughCash;

                    session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, order.Price);
                }
                else
                {
                    if (!session.Player.Inventory.HasItemCount(order.Item2Id, order.Quantity))
                        return GenericError.ItemBadId;

                    session.Player.Inventory.ItemDelete(order.Item2Id, order.Quantity, ItemUpdateReason.Auction);
                }

                postedOrder.CommodityOrderId = nextCommodityOrderId++;
                postedOrder.ListTime         = (ulong)DateTime.UtcNow.ToFileTimeUtc();
                postedOrder.ExpirationTime   = (ulong)DateTime.UtcNow.AddDays(2d).ToFileTimeUtc();
                commodityOrders.Add(new MarketplaceCommodityOrder
                {
                    OwnerCharacterId = session.Player.CharacterId,
                    Order            = CloneOrder(postedOrder)
                });
            }

            return GenericError.Ok;
        }

        public static bool CancelCommodityOrder(IWorldSession session, ulong orderId, uint item2Id, bool isBuyOrder)
        {
            if (session.Player == null)
                return false;

            lock (syncRoot)
            {
                MarketplaceCommodityOrder record = commodityOrders.FirstOrDefault(o =>
                    o.OwnerCharacterId == session.Player.CharacterId &&
                    o.Order.CommodityOrderId == orderId &&
                    o.Order.Item2Id == item2Id &&
                    o.Order.IsBuyOrder == isBuyOrder);
                if (record == null)
                    return false;

                commodityOrders.Remove(record);
                if (record.Order.IsBuyOrder)
                    session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
                else
                    session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, record.Order.Item2Id, record.Order.Quantity, ItemUpdateReason.Auction);

                return true;
            }
        }

        public static void ValidateAuctionSearch(IGameTableManager gameTableManager, IItemManager itemManager, ClientAuctionsByFilterRequest request)
        {
            uint selectorCount = 0u;

            if (request.Item2FamilyId != 0u)
            {
                if (gameTableManager.Item2Family.GetEntry(request.Item2FamilyId) == null)
                    throw new InvalidPacketValueException();
                selectorCount++;
            }

            if (request.Item2CategoryId != 0u)
            {
                if (gameTableManager.Item2Category.GetEntry(request.Item2CategoryId) == null)
                    throw new InvalidPacketValueException();
                selectorCount++;
            }

            if (request.Item2TypeId != 0u)
            {
                if (gameTableManager.Item2Type.GetEntry(request.Item2TypeId) == null)
                    throw new InvalidPacketValueException();
                selectorCount++;
            }

            if (request.Item2Ids.Count != 0)
            {
                foreach (uint item2Id in request.Item2Ids)
                    ValidateItem2(itemManager, item2Id);
                selectorCount++;
            }

            if (selectorCount > 1u || request.Filters.Count > MaxAuctionFilters)
                throw new InvalidPacketValueException();

            foreach (IAuctionFilter filter in request.Filters)
                ValidateAuctionFilter(filter);
        }

        public static void SendEnabledStatus(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerMarketplaceStatus
            {
                Status = 0
            });
        }

        public static void ValidateItem2(IItemManager itemManager, uint item2Id)
        {
            if (item2Id == 0u || itemManager.GetItemInfo(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public static IItem GetInventoryItem(IWorldSession session, ulong itemGuid)
        {
            IItem item = session.Player.Inventory.GetItem(itemGuid);
            if (item == null || item.Info == null)
                throw new InvalidPacketValueException();

            return item;
        }

        public static void ValidateCommodityOrder(IItemManager itemManager, CommodityOrder order)
        {
            ValidateItem2(itemManager, order.Item2Id);
            if (order.Quantity == 0u || order.PricePerUnit == 0ul || order.Price == 0ul)
                throw new InvalidPacketValueException();
        }

        private static ulong[] BuildCommodityPriceBuckets(IEnumerable<MarketplaceCommodityOrder> orders, bool buyOrders)
        {
            IEnumerable<ulong> prices = buyOrders
                ? orders.Select(o => o.Order.PricePerUnit).OrderByDescending(p => p)
                : orders.Select(o => o.Order.PricePerUnit).OrderBy(p => p);

            ulong[] buckets = new ulong[3];
            int index = 0;
            foreach (ulong price in prices.Take(buckets.Length))
                buckets[index++] = price;

            return buckets;
        }

        private static bool MatchesAuctionRequest(IItemManager itemManager, MarketplaceAuction auction, ClientAuctionsByFilterRequest request)
        {
            IItemInfo itemInfo = itemManager.GetItemInfo(auction.Auction.Item2Id);
            if (itemInfo == null)
                return false;

            if (request.Item2FamilyId != 0u && itemInfo.FamilyEntry?.Id != request.Item2FamilyId)
                return false;

            if (request.Item2CategoryId != 0u && itemInfo.CategoryEntry?.Id != request.Item2CategoryId)
                return false;

            if (request.Item2TypeId != 0u && itemInfo.TypeEntry?.Id != request.Item2TypeId)
                return false;

            if (request.Item2Ids.Count != 0 && !request.Item2Ids.Contains(auction.Auction.Item2Id))
                return false;

            foreach (IAuctionFilter filter in request.Filters)
            {
                switch (filter)
                {
                    case BuyoutMaxAuctionFilter buyout when auction.Auction.BuyoutPrice > buyout.BuyoutPrice:
                    case LevelAuctionFilter level when itemInfo.Entry.PowerLevel < level.Minimum || itemInfo.Entry.PowerLevel > level.Maximum:
                    case QualityAuctionFilter quality when itemInfo.Entry.ItemQualityId < (uint)quality.Minimum || itemInfo.Entry.ItemQualityId > (uint)quality.Maximum:
                        return false;
                }
            }

            return true;
        }

        private static List<AuctionInfo> SortAuctions(List<AuctionInfo> auctionsToSort, ClientAuctionsByFilterRequest request)
        {
            bool descending = (request.ReverseSort & 1u) != 0u;
            IOrderedEnumerable<AuctionInfo> sorted = request.AuctionSort switch
            {
                AuctionSort.Buyout   => descending ? auctionsToSort.OrderByDescending(a => a.BuyoutPrice) : auctionsToSort.OrderBy(a => a.BuyoutPrice),
                AuctionSort.TimeLeft => descending ? auctionsToSort.OrderByDescending(a => a.ExpirationTime) : auctionsToSort.OrderBy(a => a.ExpirationTime),
                _                    => descending ? auctionsToSort.OrderByDescending(a => a.CurrentBid == 0ul ? a.MinimumBid : a.CurrentBid) : auctionsToSort.OrderBy(a => a.CurrentBid == 0ul ? a.MinimumBid : a.CurrentBid)
            };

            return sorted.ToList();
        }

        private static void RefundTopBidder(MarketplaceAuction record)
        {
            if (record.Auction.TopBidderCharacterId == 0ul || record.Auction.CurrentBid == 0ul)
                return;

            IPlayer bidder = PlayerManager.Instance.GetPlayer(record.Auction.TopBidderCharacterId);
            bidder?.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Auction.CurrentBid);
        }

        private static void PaySeller(MarketplaceAuction record, ulong amount)
        {
            IPlayer seller = PlayerManager.Instance.GetPlayer(record.Auction.OwnerCharacterId);
            seller?.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, amount);
        }

        private static AuctionInfo CloneAuction(AuctionInfo auction)
        {
            return new AuctionInfo
            {
                AuctionId                = auction.AuctionId,
                OwnerCharacterId         = auction.OwnerCharacterId,
                MinimumBid               = auction.MinimumBid,
                BuyoutPrice              = auction.BuyoutPrice,
                CurrentBid               = auction.CurrentBid,
                TopBidderCharacterId     = auction.TopBidderCharacterId,
                ExpirationTime           = auction.ExpirationTime,
                Item2Id                  = auction.Item2Id,
                Quantity                 = auction.Quantity,
                WorldRequirement_Item2Id = auction.WorldRequirement_Item2Id,
                UnknownArray             = auction.UnknownArray.ToList(),
                CircuitData              = auction.CircuitData,
                GlyphData                = auction.GlyphData,
                ThresholdData            = auction.ThresholdData,
                Unknown2                 = auction.Unknown2
            };
        }

        private static CommodityOrder CloneOrder(CommodityOrder order)
        {
            return new CommodityOrder
            {
                CommodityOrderId = order.CommodityOrderId,
                Item2Id          = order.Item2Id,
                Quantity         = order.Quantity,
                PricePerUnit     = order.PricePerUnit,
                Price            = order.Price,
                IsBuyOrder       = order.IsBuyOrder,
                ForceImmediate   = order.ForceImmediate,
                ListTime         = order.ListTime,
                ExpirationTime   = order.ExpirationTime
            };
        }

        private sealed class MarketplaceAuction
        {
            public AuctionInfo Auction { get; init; }
            public IItem Item { get; init; }
        }

        private sealed class MarketplaceCommodityOrder
        {
            public ulong OwnerCharacterId { get; init; }
            public CommodityOrder Order { get; init; }
        }

        private static void ValidateAuctionFilter(IAuctionFilter filter)
        {
            switch (filter)
            {
                case null:
                    throw new InvalidPacketValueException();
                case LevelAuctionFilter level when level.Minimum > level.Maximum:
                    throw new InvalidPacketValueException();
                case QualityAuctionFilter quality when quality.Minimum > quality.Maximum:
                    throw new InvalidPacketValueException();
                case BuyoutMaxAuctionFilter buyout when buyout.BuyoutPrice == 0ul:
                    throw new InvalidPacketValueException();
                case PropertyMinAuctionFilter property when float.IsNaN(property.Minimum) || float.IsInfinity(property.Minimum):
                    throw new InvalidPacketValueException();
                case PropertyMaxAuctionFilter property when float.IsNaN(property.Maximum) || float.IsInfinity(property.Maximum):
                    throw new InvalidPacketValueException();
            }
        }
    }
}
