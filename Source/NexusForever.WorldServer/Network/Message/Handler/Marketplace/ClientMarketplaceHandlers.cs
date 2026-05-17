using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Returning empty commodity info for marketplace request from player {PlayerGuid}: item {Item2Id}, reason marketplace-service-unavailable.",
                session.Player?.Guid, request.Item2Id);
            session.EnqueueMessageEncrypted(new ServerCommodityInfoResults
            {
                Item2Id = request.Item2Id
            });
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
            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Returning empty owned commodity orders for marketplace request from player {PlayerGuid}: reason marketplace-service-unavailable.",
                session.Player?.Guid);
            session.EnqueueMessageEncrypted(new ServerOwnedCommodityOrders());
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
            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Returning empty owned item auctions for marketplace request from player {PlayerGuid}: reason marketplace-service-unavailable.",
                session.Player?.Guid);
            session.EnqueueMessageEncrypted(new ServerOwnedItemAuctions());
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Returning empty auction search results for marketplace request from player {PlayerGuid}: page {Page}, reason marketplace-service-unavailable.",
                session.Player?.Guid, request.Page);
            session.EnqueueMessageEncrypted(new ServerAuctionSearchResults
            {
                CurrentPage = request.Page
            });
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Rejecting auction buy request from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, amount {AmountOffered}, reason marketplace-service-unavailable.",
                session.Player?.Guid, buyOrderSubmit.AuctionId, buyOrderSubmit.Item2Id, buyOrderSubmit.AmountOffered);
            session.EnqueueMessageEncrypted(new ServerAuctionBidResult
            {
                Result = GenericError.AuctionItemAuctionDisabled,
                Auction = new AuctionInfo
                {
                    AuctionId  = buyOrderSubmit.AuctionId,
                    Item2Id    = buyOrderSubmit.Item2Id,
                    CurrentBid = buyOrderSubmit.AmountOffered
                }
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Rejecting auction sell request from player {PlayerGuid}: item {ItemGuid}, minimum bid {MinimumBid}, buyout {BuyoutPrice}, reason marketplace-service-unavailable.",
                session.Player?.Guid, sellOrderSubmit.ItemGuid, sellOrderSubmit.MinimumBid, sellOrderSubmit.BuyoutPrice);
            session.EnqueueMessageEncrypted(new ServerAuctionPostResult
            {
                Result = GenericError.AuctionItemAuctionDisabled,
                Auction = new AuctionInfo
                {
                    Item2Id      = item.Id,
                    Quantity     = item.StackCount,
                    MinimumBid   = sellOrderSubmit.MinimumBid,
                    BuyoutPrice  = sellOrderSubmit.BuyoutPrice
                }
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Rejecting auction cancel request from player {PlayerGuid}: auction {AuctionId}, item {Item2Id}, reason marketplace-service-unavailable.",
                session.Player?.Guid, auctionCancel.AuctionId, auctionCancel.Item2Id);
            session.EnqueueMessageEncrypted(new ServerAuctionCancelResult
            {
                Result = GenericError.AuctionItemAuctionDisabled,
                Auction = new AuctionInfo
                {
                    AuctionId = auctionCancel.AuctionId,
                    Item2Id   = auctionCancel.Item2Id
                }
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Rejecting commodity order request from player {PlayerGuid}: item {Item2Id}, quantity {Quantity}, buy order {IsBuyOrder}, reason marketplace-service-unavailable.",
                session.Player?.Guid, sellOrderSubmit.Order.Item2Id, sellOrderSubmit.Order.Quantity, sellOrderSubmit.Order.IsBuyOrder);
            session.EnqueueMessageEncrypted(new ServerCommodityOrderResult
            {
                Result = GenericError.AuctionCommodityDisabled,
                OrderPosted = sellOrderSubmit.Order
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

            MarketplaceRequestHelper.SendDisabledStatus(session);

            log.LogDebug("Ignoring commodity order cancel request from player {PlayerGuid}: order {CommodityOrderId}, item {Item2Id}, buy order {IsBuyOrder}, reason marketplace-service-unavailable.",
                session.Player?.Guid, orderCancel.CommodityOrderId, orderCancel.Item2Id, orderCancel.IsBuyOrder);
        }
    }

    internal static class MarketplaceRequestHelper
    {
        private const uint MaxAuctionFilters = 7u;

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

        public static void SendDisabledStatus(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerMarketplaceStatus
            {
                Status = MarketplaceStatus.AuctionsDisabled | MarketplaceStatus.CommoditiesDisabled
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
