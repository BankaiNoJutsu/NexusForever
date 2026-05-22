using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model.Marketplace.Filter;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Abstract.Marketplace
{
    public interface IGlobalMarketplaceManager
    {
        void Initialise();

        void Update(double lastTick);

        ServerCommodityInfoResults BuildCommodityInfo(uint item2Id);
        List<CommodityOrder> GetOwnedCommodityOrders(IPlayer player);
        List<AuctionInfo> GetOwnedItemAuctions(IPlayer player);
        ServerAuctionSearchResults SearchAuctions(IItemManager itemManager, ClientAuctionsByFilterRequest request);
        GenericError PostAuction(IPlayer player, IItem item, ulong minimumBid, ulong buyoutPrice, out AuctionInfo auction);
        GenericError BuyAuction(IPlayer player, ClientAuctionBuyOrderSubmit buyOrderSubmit, out AuctionInfo auction);
        GenericError CancelAuction(IPlayer player, ulong auctionId, uint item2Id, out AuctionInfo auction);
        GenericError PostCommodityOrder(IPlayer player, CommodityOrder order, out CommodityOrder postedOrder);
        bool CancelCommodityOrder(IPlayer player, ulong orderId, uint item2Id, bool isBuyOrder, out CommodityOrder cancelledOrder);

        void ValidateAuctionSearch(IGameTableManager gameTableManager, IItemManager itemManager, ClientAuctionsByFilterRequest request);
        void ValidateItem2(IItemManager itemManager, uint item2Id);
        IItem GetInventoryItem(IPlayer player, ulong itemGuid);
        void ValidateCommodityOrder(IGameTableManager gameTableManager, IItemManager itemManager, CommodityOrder order);
    }
}
