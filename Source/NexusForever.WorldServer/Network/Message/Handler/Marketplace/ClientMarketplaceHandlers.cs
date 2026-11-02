using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Marketplace;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientRequestCommodityInfoHandler : IMessageHandler<IWorldSession, ClientRequestCommodityInfo>
    {
        private readonly ILogger<ClientRequestCommodityInfoHandler> log;
        private readonly IItemManager itemManager;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientRequestCommodityInfoHandler(
            ILogger<ClientRequestCommodityInfoHandler> log,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientRequestCommodityInfo request)
        {
            marketplaceRequestHelper.ValidateItem2(itemManager, request.Item2Id);

            marketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning commodity info for marketplace request from player {PlayerGuid}: item {Item2Id}.",
                session.Player?.Guid, request.Item2Id);
            session.EnqueueMessageEncrypted(marketplaceRequestHelper.BuildCommodityInfo(request.Item2Id));
        }
    }

    public class ClientRequestOwnedCommodityOrdersHandler : IMessageHandler<IWorldSession, ClientRequestOwnedCommodityOrders>
    {
        private readonly ILogger<ClientRequestOwnedCommodityOrdersHandler> log;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientRequestOwnedCommodityOrdersHandler(
            ILogger<ClientRequestOwnedCommodityOrdersHandler> log,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientRequestOwnedCommodityOrders request)
        {
            marketplaceRequestHelper.SendEnabledStatus(session);

            List<CommodityOrder> orders = marketplaceRequestHelper.GetOwnedCommodityOrders(session);
            if (orders.Count == 0)
            {
                log.LogInformation("StorefrontCatalogDiagnostics marketplace owned commodity orders request for player {PlayerGuid}: rows=0.",
                    session.Player?.Guid);
            }

            log.LogDebug("Returning owned commodity orders for marketplace request from player {PlayerGuid}: rows {OrderCount}.",
                session.Player?.Guid, orders.Count);
            session.EnqueueMessageEncrypted(new ServerOwnedCommodityOrders
            {
                Orders = orders
            });
        }
    }

    public class ClientRequestOwnedItemAuctionsHandler : IMessageHandler<IWorldSession, ClientRequestOwnedItemAuctions>
    {
        private readonly ILogger<ClientRequestOwnedItemAuctionsHandler> log;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientRequestOwnedItemAuctionsHandler(
            ILogger<ClientRequestOwnedItemAuctionsHandler> log,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientRequestOwnedItemAuctions request)
        {
            marketplaceRequestHelper.SendEnabledStatus(session);

            List<AuctionInfo> auctions = marketplaceRequestHelper.GetOwnedItemAuctions(session);
            if (auctions.Count == 0)
            {
                log.LogInformation("StorefrontCatalogDiagnostics marketplace owned item auctions request for player {PlayerGuid}: rows=0.",
                    session.Player?.Guid);
            }

            log.LogDebug("Returning owned item auctions for marketplace request from player {PlayerGuid}: rows {AuctionCount}.",
                session.Player?.Guid, auctions.Count);
            session.EnqueueMessageEncrypted(new ServerOwnedItemAuctions
            {
                Auctions = auctions
            });
        }
    }

    public class ClientAuctionsByFilterRequestHandler : IMessageHandler<IWorldSession, ClientAuctionsByFilterRequest>
    {
        private readonly ILogger<ClientAuctionsByFilterRequestHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientAuctionsByFilterRequestHandler(
            ILogger<ClientAuctionsByFilterRequestHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.gameTableManager         = gameTableManager;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionsByFilterRequest request)
        {
            marketplaceRequestHelper.ValidateAuctionSearch(gameTableManager, itemManager, request);

            marketplaceRequestHelper.SendEnabledStatus(session);

            log.LogDebug("Returning auction search results for marketplace request from player {PlayerGuid}: page {Page}.",
                session.Player?.Guid, request.Page);
            session.EnqueueMessageEncrypted(marketplaceRequestHelper.SearchAuctions(itemManager, request));
        }
    }

    public class ClientAuctionBuyOrderSubmitHandler : IMessageHandler<IWorldSession, ClientAuctionBuyOrderSubmit>
    {
        private readonly ILogger<ClientAuctionBuyOrderSubmitHandler> log;
        private readonly IItemManager itemManager;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientAuctionBuyOrderSubmitHandler(
            ILogger<ClientAuctionBuyOrderSubmitHandler> log,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionBuyOrderSubmit buyOrderSubmit)
        {
            marketplaceRequestHelper.ValidateItem2(itemManager, buyOrderSubmit.Item2Id);
            if (buyOrderSubmit.AuctionId == 0ul || buyOrderSubmit.AmountOffered == 0ul)
                throw new InvalidPacketValueException();

            marketplaceRequestHelper.SendEnabledStatus(session);

            // Retain scalar request values and the original actor only. The
            // scoped handler/helper and reusable request are gone before the
            // asynchronous financial operation completes.
            ulong auctionId = buyOrderSubmit.AuctionId, amount = buyOrderSubmit.AmountOffered;
            uint definition = buyOrderSubmit.Item2Id;
            IPlayer buyer = session.Player;
            var logger = log;
            if (marketplaceRequestHelper.TryQueueAuctionPurchase(buyer, auctionId, definition, amount, (error, view) =>
            {
                if (!ReferenceEquals(session.Player, buyer)) return;
                logger.LogDebug("Processed auction bid from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, amount {AmountOffered}, result {Result}.",
                    buyer.Guid, auctionId, definition, amount, error);
                session.EnqueueMessageEncrypted(new ServerAuctionBidResult { Result = error, Auction = view });
            }, () => ReferenceEquals(session.Player, buyer))) return;

            GenericError result = marketplaceRequestHelper.BuyAuction(session, buyOrderSubmit, out AuctionInfo auction);
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
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientAuctionSellOrderSubmitHandler(
            ILogger<ClientAuctionSellOrderSubmitHandler> log,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionSellOrderSubmit sellOrderSubmit)
        {
            IItem item = marketplaceRequestHelper.GetInventoryItem(session, sellOrderSubmit.ItemGuid);
            if (sellOrderSubmit.MinimumBid == 0ul || (sellOrderSubmit.BuyoutPrice != 0ul && sellOrderSubmit.BuyoutPrice < sellOrderSubmit.MinimumBid))
                throw new InvalidPacketValueException();

            marketplaceRequestHelper.SendEnabledStatus(session);

            var owner = session.Player;
            ulong guid = sellOrderSubmit.ItemGuid, minimum = sellOrderSubmit.MinimumBid, buyout = sellOrderSubmit.BuyoutPrice;
            var logger = log;
            if (marketplaceRequestHelper.TryQueueAuctionPosting(owner, guid, minimum, buyout, (error, view) =>
            {
                if (!ReferenceEquals(session.Player, owner)) return;
                logger.LogDebug("Processed auction sell from player {PlayerGuid}: item {ItemGuid}, minimum {MinimumBid}, buyout {BuyoutPrice}, result {Result}.",
                    owner.Guid, guid, minimum, buyout, error);
                session.EnqueueMessageEncrypted(new ServerAuctionPostResult { Result = error, Auction = view });
            }, () => ReferenceEquals(session.Player, owner))) return;

            GenericError result = marketplaceRequestHelper.PostAuction(session, item, sellOrderSubmit.MinimumBid, sellOrderSubmit.BuyoutPrice, out AuctionInfo auction);
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
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientAuctionCancelHandler(
            ILogger<ClientAuctionCancelHandler> log,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionCancel auctionCancel)
        {
            marketplaceRequestHelper.ValidateItem2(itemManager, auctionCancel.Item2Id);
            if (auctionCancel.AuctionId == 0ul)
                throw new InvalidPacketValueException();

            marketplaceRequestHelper.SendEnabledStatus(session);

            ulong auctionId = auctionCancel.AuctionId;
            uint definition = auctionCancel.Item2Id;
            IPlayer owner = session.Player;
            var logger = log;
            if (marketplaceRequestHelper.TryQueueAuctionCancellation(owner, auctionId, definition, (error, view) =>
            {
                if (!ReferenceEquals(session.Player, owner)) return;
                logger.LogDebug("Processed auction cancel from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, result {Result}.",
                    owner.Guid, auctionId, definition, error);
                session.EnqueueMessageEncrypted(new ServerAuctionCancelResult { Result = error, Auction = view });
            }, () => ReferenceEquals(session.Player, owner))) return;

            GenericError result = marketplaceRequestHelper.CancelAuction(session, auctionCancel.AuctionId, auctionCancel.Item2Id, out AuctionInfo auction);
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
        private readonly IGameTableManager gameTableManager;
        private readonly IItemManager itemManager;
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientCommoditySellOrderSubmitHandler(
            ILogger<ClientCommoditySellOrderSubmitHandler> log,
            IGameTableManager gameTableManager,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.gameTableManager         = gameTableManager;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientCommoditySellOrderSubmit sellOrderSubmit)
        {
            marketplaceRequestHelper.ValidateCommodityOrder(gameTableManager, itemManager, sellOrderSubmit.Order);

            marketplaceRequestHelper.SendEnabledStatus(session);

            IPlayer owner = session.Player;
            uint definition = sellOrderSubmit.Order.Item2Id, quantity = sellOrderSubmit.Order.Quantity;
            bool buy = sellOrderSubmit.Order.IsBuyOrder;
            var logger = log;
            if (marketplaceRequestHelper.TryQueueCommodityPosting(owner, sellOrderSubmit.Order, (error, order) =>
            {
                if (!ReferenceEquals(session.Player, owner)) return;
                logger.LogDebug("Processed commodity order request from player {PlayerGuid}: item {Item2Id}, quantity {Quantity}, buy order {IsBuyOrder}, result {Result}.",
                    owner.Guid, definition, quantity, buy, error);
                session.EnqueueMessageEncrypted(new ServerCommodityOrderResult { Result = error, OrderPosted = order });
            }, () => ReferenceEquals(session.Player, owner))) return;

            GenericError result = marketplaceRequestHelper.PostCommodityOrder(session, sellOrderSubmit.Order, itemManager, out CommodityOrder postedOrder);
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
        private readonly MarketplaceRequestHelper marketplaceRequestHelper;

        public ClientCommodityOrderCancelHandler(
            ILogger<ClientCommodityOrderCancelHandler> log,
            IItemManager itemManager,
            MarketplaceRequestHelper marketplaceRequestHelper)
        {
            this.log                      = log;
            this.itemManager              = itemManager;
            this.marketplaceRequestHelper = marketplaceRequestHelper;
        }

        public void HandleMessage(IWorldSession session, ClientCommodityOrderCancel orderCancel)
        {
            marketplaceRequestHelper.ValidateItem2(itemManager, orderCancel.Item2Id);
            if (orderCancel.CommodityOrderId == 0ul)
                throw new InvalidPacketValueException();

            marketplaceRequestHelper.SendEnabledStatus(session);

            ulong orderId = orderCancel.CommodityOrderId;
            uint definition = orderCancel.Item2Id;
            bool buy = orderCancel.IsBuyOrder;
            IPlayer owner = session.Player;
            var logger = log;
            if (marketplaceRequestHelper.TryQueueCommodityCancellation(owner, orderId, definition, buy, (error, order) =>
            {
                if (!ReferenceEquals(session.Player, owner)) return;
                logger.LogDebug("Processed commodity cancel from player {PlayerGuid}: order {OrderId}, item {Item2Id}, buy order {IsBuyOrder}, result {Result}.",
                    owner.Guid, orderId, definition, buy, error);
                if (error == GenericError.Ok) session.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved { OrderRemoved = order, Type = AuctionEventType.Cancel });
                else session.EnqueueMessageEncrypted(new ServerCommodityOrderResult { Result = error, OrderPosted = order });
            }, () => ReferenceEquals(session.Player, owner))) return;

            GenericError result = marketplaceRequestHelper.CancelCommodityOrder(session, orderCancel.CommodityOrderId, orderCancel.Item2Id, orderCancel.IsBuyOrder, itemManager, out CommodityOrder cancelledOrder);
            log.LogDebug("Processed commodity order cancel request from player {PlayerGuid}: order {CommodityOrderId}, item {Item2Id}, buy order {IsBuyOrder}, result {Result}.",
                session.Player?.Guid, orderCancel.CommodityOrderId, orderCancel.Item2Id, orderCancel.IsBuyOrder, result);

            if (result == GenericError.Ok)
            {
                session.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved
                {
                    OrderRemoved = cancelledOrder,
                    Type         = AuctionEventType.Cancel
                });
            }
            else
            {
                session.EnqueueMessageEncrypted(new ServerCommodityOrderResult
                {
                    Result      = result,
                    OrderPosted = cancelledOrder
                });
            }
        }
    }

    public sealed class MarketplaceRequestHelper
    {
        private readonly IGlobalMarketplaceManager marketplace;

        public MarketplaceRequestHelper(
            IGlobalMarketplaceManager marketplace)
        {
            this.marketplace = marketplace;
        }

        public ServerCommodityInfoResults BuildCommodityInfo(uint item2Id) =>
            marketplace.BuildCommodityInfo(item2Id);

        public List<CommodityOrder> GetOwnedCommodityOrders(IWorldSession session) =>
            marketplace.GetOwnedCommodityOrders(session.Player);

        public List<AuctionInfo> GetOwnedItemAuctions(IWorldSession session) =>
            marketplace.GetOwnedItemAuctions(session.Player);

        public ServerAuctionSearchResults SearchAuctions(IItemManager itemManager, ClientAuctionsByFilterRequest request) =>
            marketplace.SearchAuctions(itemManager, request);

        public GenericError PostAuction(IWorldSession session, IItem item, ulong minimumBid, ulong buyoutPrice, out AuctionInfo auction) =>
            marketplace.PostAuction(session.Player, item, minimumBid, buyoutPrice, out auction);

        public bool TryQueueAuctionPosting(IPlayer owner, ulong guid, ulong minimum, ulong buyout,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed) =>
            marketplace is GlobalMarketplaceManager concrete && concrete.TryQueueAuctionPosting(owner, guid, minimum, buyout, reply, publicationAllowed);

        public GenericError BuyAuction(IWorldSession session, ClientAuctionBuyOrderSubmit buyOrderSubmit, out AuctionInfo auction) =>
            marketplace.BuyAuction(session.Player, buyOrderSubmit, out auction);

        public bool TryQueueAuctionPurchase(IPlayer buyer, ulong auctionId, uint definition, ulong amount,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null) => marketplace is GlobalMarketplaceManager global
                && global.TryQueueAuctionPurchase(buyer, auctionId, definition, amount, reply, publicationAllowed);

        public bool TryQueueAuctionCancellation(IPlayer owner, ulong auctionId, uint definition,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null) => marketplace is GlobalMarketplaceManager global
                && global.TryQueueAuctionCancellation(owner, auctionId, definition, reply, publicationAllowed);

        public GenericError CancelAuction(IWorldSession session, ulong auctionId, uint item2Id, out AuctionInfo auction) =>
            marketplace.CancelAuction(session.Player, auctionId, item2Id, out auction);

        public GenericError PostCommodityOrder(IWorldSession session, CommodityOrder order, IItemManager itemManager, out CommodityOrder postedOrder) =>
            marketplace.PostCommodityOrder(session.Player, order, out postedOrder, itemManager);

        public bool TryQueueCommodityPosting(IPlayer owner, CommodityOrder order, Action<GenericError, CommodityOrder> reply,
            Func<bool> publicationAllowed = null) => marketplace is GlobalMarketplaceManager global
                && global.TryQueueCommodityPosting(owner, order, reply, publicationAllowed);

        public bool TryQueueCommodityCancellation(IPlayer owner, ulong orderId, uint definition, bool buy,
            Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed = null) => marketplace is GlobalMarketplaceManager global
                && global.TryQueueCommodityCancellation(owner, orderId, definition, buy, reply, publicationAllowed);

        public GenericError CancelCommodityOrder(IWorldSession session, ulong orderId, uint item2Id, bool isBuyOrder, IItemManager itemManager, out CommodityOrder cancelledOrder) =>
            marketplace.CancelCommodityOrder(session.Player, orderId, item2Id, isBuyOrder, out cancelledOrder, itemManager);

        public void ValidateAuctionSearch(IGameTableManager gameTableManager, IItemManager itemManager, ClientAuctionsByFilterRequest request) =>
            marketplace.ValidateAuctionSearch(gameTableManager, itemManager, request);

        public void SendEnabledStatus(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerMarketplaceStatus
            {
                Status = 0
            });
        }

        public void ValidateItem2(IItemManager itemManager, uint item2Id) =>
            marketplace.ValidateItem2(itemManager, item2Id);

        public IItem GetInventoryItem(IWorldSession session, ulong itemGuid) =>
            marketplace.GetInventoryItem(session.Player, itemGuid);

        public void ValidateCommodityOrder(IGameTableManager gameTableManager, IItemManager itemManager, CommodityOrder order) =>
            marketplace.ValidateCommodityOrder(gameTableManager, itemManager, order);
    }
}
