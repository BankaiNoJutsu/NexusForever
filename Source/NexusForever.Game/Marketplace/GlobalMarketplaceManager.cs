using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Marketplace;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model.Marketplace.Filter;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager : IGlobalMarketplaceManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const uint MaxAuctionFilters = 7u;
        private const uint AuctionPageSize = 50u;
        private const uint CommodityOrderQuantityLimitGameFormulaId = 0x439u;
        private const uint DefaultMaxCommodityOrderQuantity = 200u;

        private readonly object syncRoot = new();
        private readonly List<MarketplaceAuction> auctions = [];
        private readonly List<MarketplaceCommodityOrder> commodityOrders = [];
        private readonly Dictionary<uint, CommodityOrderBook> commodityOrderBooks = [];
        private ulong nextAuctionId = 1ul;
        private ulong nextCommodityOrderId = 1ul;
        private double expirationCheckTimer;

        private readonly IDatabaseManager databaseManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IAssetManager assetManager;
        private readonly IPlayerManager playerManager;
        private readonly IItemManager itemManager;

        public GlobalMarketplaceManager()
        {
        }

        public GlobalMarketplaceManager(
            IDatabaseManager databaseManager,
            IGameTableManager gameTableManager,
            IAssetManager assetManager = null,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null)
        {
            this.databaseManager  = databaseManager;
            this.gameTableManager = gameTableManager;
            this.assetManager     = assetManager;
            this.playerManager    = playerManager;
            this.itemManager      = itemManager;
        }

        public void Initialise()
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
            {
                log.Warn("GlobalMarketplaceManager.Initialise skipped: CharacterDatabase is null. No auction/order data loaded.");
                return;
            }

            nextAuctionId        = database.GetNextMarketplaceAuctionId() + 1ul;
            nextCommodityOrderId = database.GetNextMarketplaceCommodityOrderId() + 1ul;

            lock (syncRoot)
            {
                auctions.Clear();
                commodityOrders.Clear();
                commodityOrderBooks.Clear();
                IGameTableManager tables = GetGameTableManager();

                foreach (MarketplaceAuctionModel model in database.GetMarketplaceAuctions())
                {
                    if (model.Item == null)
                    {
                        log.Debug("GlobalMarketplaceManager.Initialise skipping auction id={0}: model.Item is null.", model.Id);
                        continue;
                    }

                    auctions.Add(new MarketplaceAuction
                    {
                        Auction      = ToAuctionInfo(model),
                        Item         = new Item(model.Item, itemManager, tables),
                        ExpiresAtUtc = model.ExpirationTime
                    });
                }

                foreach (MarketplaceCommodityOrderModel model in database.GetMarketplaceCommodityOrders())
                {
                    if (!IsValidPersistedCommodityOrder(model, tables))
                        continue;

                    AddCommodityOrder(new MarketplaceCommodityOrder
                    {
                        OwnerCharacterId = model.OwnerCharacterId,
                        Order            = ToCommodityOrder(model)
                    });
                }
            }
        }

        public void Update(double lastTick)
        {
            expirationCheckTimer += lastTick;
            if (expirationCheckTimer < 1d)
                return;

            expirationCheckTimer = 0d;
            ProcessExpiredAuctions();
            ProcessExpiredCommodityOrders();
        }

        public ServerCommodityInfoResults BuildCommodityInfo(uint item2Id)
        {
            List<MarketplaceCommodityOrder> itemOrders;
            lock (syncRoot)
            {
                itemOrders = commodityOrders
                    .Where(o => o.Order.Item2Id == item2Id)
                    .ToList();
            }

            return new ServerCommodityInfoResults
            {
                Item2Id         = item2Id,
                BuyOrderCount   = (uint)itemOrders.Count(o => o.Order.IsBuyOrder),
                BuyOrderPrices  = BuildCommodityPriceBuckets(itemOrders.Where(o => o.Order.IsBuyOrder), true),
                SellOrderCount  = (uint)itemOrders.Count(o => !o.Order.IsBuyOrder),
                SellOrderPrices = BuildCommodityPriceBuckets(itemOrders.Where(o => !o.Order.IsBuyOrder), false),
                Orders          = itemOrders.Select(o => CloneOrder(o.Order)).ToList()
            };
        }

        public List<CommodityOrder> GetOwnedCommodityOrders(IPlayer player)
        {
            if (player == null)
                return [];

            lock (syncRoot)
            {
                return commodityOrders
                    .Where(o => o.OwnerCharacterId == player.CharacterId)
                    .Select(o => CloneOrder(o.Order))
                    .ToList();
            }
        }

        public List<AuctionInfo> GetOwnedItemAuctions(IPlayer player)
        {
            if (player == null)
                return [];

            lock (syncRoot)
            {
                return auctions
                    .Where(a => a.Auction.OwnerCharacterId == player.CharacterId)
                    .Select(a => CloneAuction(a.Auction, a.ExpiresAtUtc))
                    .ToList();
            }
        }

        public ServerAuctionSearchResults SearchAuctions(IItemManager itemManager, ClientAuctionsByFilterRequest request)
        {
            List<MarketplaceAuction> snapshot;
            lock (syncRoot)
                snapshot = auctions.ToList();

            List<MarketplaceAuction> matching = snapshot
                .Where(a => MatchesAuctionRequest(itemManager, a, request))
                .ToList();

            bool descending = (request.ReverseSort & 1u) != 0u;
            IOrderedEnumerable<MarketplaceAuction> sorted = request.AuctionSort switch
            {
                AuctionSort.Buyout   => descending ? matching.OrderByDescending(a => a.Auction.BuyoutPrice) : matching.OrderBy(a => a.Auction.BuyoutPrice),
                AuctionSort.TimeLeft => descending ? matching.OrderByDescending(GetAuctionSortExpiration) : matching.OrderBy(GetAuctionSortExpiration),
                _                    => descending ? matching.OrderByDescending(a => a.Auction.CurrentBid == 0ul ? a.Auction.MinimumBid : a.Auction.CurrentBid) : matching.OrderBy(a => a.Auction.CurrentBid == 0ul ? a.Auction.MinimumBid : a.Auction.CurrentBid)
            };
            List<MarketplaceAuction> sortedList = sorted.ToList();

            ulong skip = (ulong)request.Page * AuctionPageSize;
            List<AuctionInfo> page = skip > int.MaxValue
                ? []
                : sortedList
                    .Skip((int)skip)
                    .Take((int)AuctionPageSize)
                    .Select(a => CloneAuction(a.Auction, a.ExpiresAtUtc))
                    .ToList();

            return new ServerAuctionSearchResults
            {
                TotalResultCount = (uint)sortedList.Count,
                CurrentPage      = request.Page,
                Auctions         = page
            };
        }

        public GenericError PostAuction(IPlayer player, IItem item, ulong minimumBid, ulong buyoutPrice, out AuctionInfo auction)
        {
            auction = new AuctionInfo();

            if (player == null || item?.Info == null)
                return GenericError.Params;

            auction.Item2Id     = item.Id;
            auction.Quantity    = item.StackCount;
            auction.MinimumBid  = minimumBid;
            auction.BuyoutPrice = buyoutPrice;

            if (!CanPostAuctionItem(player, item))
                return GenericError.AuctionCannotFillOrder;

            MarketplaceAuction record;
            lock (syncRoot)
            {
                if (CountOwnedSellAuctions(player.CharacterId) >= MarketplaceAccountLimits.GetMaxAuctionSellLots(player))
                    return GenericError.AuctionTooManyOrders;

                ulong expirationSeconds = MarketplaceAuctionDuration.GetDefaultExpirationSeconds(GetGameTableManager());
                ulong expiresAtUtc = (ulong)DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

                auction.AuctionId        = nextAuctionId++;
                auction.OwnerCharacterId = player.CharacterId;
                auction.ExpirationTime   = expirationSeconds;

                player.Inventory.ItemRemove(item, ItemUpdateReason.Auction);

                record = new MarketplaceAuction
                {
                    Auction      = CloneAuction(auction, expiresAtUtc),
                    Item         = item,
                    ExpiresAtUtc = expiresAtUtc
                };
                auctions.Add(record);
            }

            if (!PersistAuctionInsert(record))
            {
                lock (syncRoot)
                {
                    auctions.Remove(record);
                    if (auction.AuctionId + 1ul == nextAuctionId)
                        nextAuctionId--;
                }

                RestoreRemovedAuctionItem(player, item);
                return GenericError.DbFailure;
            }

            return GenericError.Ok;
        }

        public GenericError BuyAuction(IPlayer player, ClientAuctionBuyOrderSubmit buyOrderSubmit, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                AuctionId  = buyOrderSubmit.AuctionId,
                Item2Id    = buyOrderSubmit.Item2Id,
                CurrentBid = buyOrderSubmit.AmountOffered
            };

            if (player == null)
                return GenericError.Params;

            MarketplaceAuction record;
            bool isBuyout;
            ulong originalBid;
            ulong originalTopBidderId;
            ulong previousBidderId;
            ulong previousBid;
            ulong amountToCharge;
            ulong acceptedAmount;

            lock (syncRoot)
            {
                record = auctions.FirstOrDefault(a => a.Auction.AuctionId == buyOrderSubmit.AuctionId);
                if (record == null || record.Auction.Item2Id != buyOrderSubmit.Item2Id)
                    return GenericError.ItemBadId;

                if (record.Auction.OwnerCharacterId == player.CharacterId)
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.Params;
                }

                ulong minimumOffer = record.Auction.CurrentBid == 0ul ? record.Auction.MinimumBid : record.Auction.CurrentBid + 1ul;
                isBuyout = record.Auction.BuyoutPrice != 0ul && buyOrderSubmit.AmountOffered >= record.Auction.BuyoutPrice;
                bool alreadyTopBidder = record.Auction.TopBidderCharacterId == player.CharacterId;
                if (!isBuyout && !alreadyTopBidder
                    && CountActiveAuctionBids(player.CharacterId) >= MarketplaceAccountLimits.GetMaxAuctionBids(player))
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.AuctionTooManyBids;
                }

                acceptedAmount = isBuyout ? record.Auction.BuyoutPrice : buyOrderSubmit.AmountOffered;
                if (acceptedAmount < minimumOffer)
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.Params;
                }

                previousBidderId = record.Auction.TopBidderCharacterId;
                previousBid      = record.Auction.CurrentBid;
                amountToCharge   = previousBidderId == player.CharacterId ? acceptedAmount - previousBid : acceptedAmount;
                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, amountToCharge))
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.VendorNotEnoughCash;
                }

                if (isBuyout && !CanDeliverAuctionItemToCharacter(player.CharacterId, record.Item, player))
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.ItemInventoryFull;
                }

                originalBid           = record.Auction.CurrentBid;
                originalTopBidderId   = record.Auction.TopBidderCharacterId;
                record.Auction.CurrentBid           = acceptedAmount;
                record.Auction.TopBidderCharacterId = player.CharacterId;

                if (!isBuyout)
                    player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, amountToCharge);

                auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
            }

            bool refundPreviousBidder = previousBidderId != 0ul && previousBidderId != player.CharacterId;
            IPlayer previousBidder = refundPreviousBidder ? playerManager?.GetPlayer(previousBidderId) : null;
            ulong previousBidderRefund = refundPreviousBidder ? previousBid : 0ul;

            if (!isBuyout)
            {
                if (!PersistAuctionUpdateWithOfflineCredit(record, previousBidderId, previousBidderRefund, previousBidder))
                {
                    lock (syncRoot)
                    {
                        record.Auction.CurrentBid           = originalBid;
                        record.Auction.TopBidderCharacterId = originalTopBidderId;
                        auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    }

                    player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, amountToCharge);
                    return GenericError.DbFailure;
                }

                if (refundPreviousBidder)
                {
                    CreditOnlinePlayer(previousBidder, CurrencyType.Credits, previousBidderRefund);
                    NotifyOutbid(previousBidderId, CloneAuction(record.Auction, record.ExpiresAtUtc));
                }

                return GenericError.Ok;
            }

            AuctionInfo wonAuction = CloneAuction(record.Auction, record.ExpiresAtUtc);
            bool deliverToInventory = player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
            if (deliverToInventory)
            {
                ulong? previousItemCharacterId;
                lock (syncRoot)
                {
                    previousItemCharacterId = record.Item.CharacterId;
                    record.Item.CharacterId = player.CharacterId;
                }

                IPlayer onlineSeller = playerManager?.GetPlayer(record.Auction.OwnerCharacterId);
                if (!PersistAuctionDeleteWithCredits(record, acceptedAmount, onlineSeller, previousBidderId, previousBidderRefund, previousBidder))
                {
                    lock (syncRoot)
                    {
                        record.Item.CharacterId               = previousItemCharacterId;
                        record.Auction.CurrentBid           = originalBid;
                        record.Auction.TopBidderCharacterId = originalTopBidderId;
                    }

                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.DbFailure;
                }

                CreditOnlinePlayer(previousBidder, CurrencyType.Credits, previousBidderRefund);

                player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, amountToCharge);
                player.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);

                lock (syncRoot)
                {
                    PayOnlineSeller(record, acceptedAmount, onlineSeller);
                    auctions.Remove(record);
                }

                NotifyAuctionWon(player, wonAuction);
                return GenericError.Ok;
            }

            bool persistedAuctionDelete;
            lock (syncRoot)
            {
                if (!CompleteAuctionSale(
                    record,
                    player.CharacterId,
                    acceptedAmount,
                    wonAuction,
                    player,
                    true,
                    previousBidderId,
                    previousBidderRefund,
                    previousBidder,
                    out persistedAuctionDelete))
                {
                    record.Auction.CurrentBid           = originalBid;
                    record.Auction.TopBidderCharacterId = originalTopBidderId;
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.ItemInventoryFull;
                }

                player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, amountToCharge);
                auctions.Remove(record);
            }

            if (!persistedAuctionDelete)
                PersistAuctionDeleteWithCredits(record, acceptedAmount, playerManager?.GetPlayer(record.Auction.OwnerCharacterId), previousBidderId, previousBidderRefund, previousBidder);

            return GenericError.Ok;
        }

        public GenericError CancelAuction(IPlayer player, ulong auctionId, uint item2Id, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                AuctionId = auctionId,
                Item2Id   = item2Id
            };

            if (player == null)
                return GenericError.Params;

            MarketplaceAuction record;
            bool canReturnToInventory;
            ulong? previousItemCharacterId;
            bool persistedAuctionDelete;
            ulong topBidderId;
            ulong topBidderRefund;
            IPlayer topBidder;

            lock (syncRoot)
            {
                record = auctions.FirstOrDefault(a => a.Auction.AuctionId == auctionId);
                if (record == null || record.Auction.Item2Id != item2Id)
                    return GenericError.ItemBadId;

                auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                if (record.Auction.OwnerCharacterId != player.CharacterId)
                    return GenericError.Params;

                canReturnToInventory = player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
                if (!canReturnToInventory && !IsMarketplaceMailAvailable())
                    return GenericError.ItemInventoryFull;

                previousItemCharacterId = record.Item.CharacterId;
                record.Item.CharacterId = player.CharacterId;
                persistedAuctionDelete  = false;
                topBidderId             = record.Auction.TopBidderCharacterId;
                topBidderRefund         = record.Auction.CurrentBid;
                topBidder               = topBidderId != 0ul && topBidderRefund != 0ul
                    ? playerManager?.GetPlayer(topBidderId)
                    : null;
            }

            if (canReturnToInventory)
            {
                if (!PersistAuctionDeleteWithBidderRefund(record, topBidderId, topBidderRefund, topBidder))
                {
                    record.Item.CharacterId = previousItemCharacterId;
                    return GenericError.DbFailure;
                }
            }
            else if (!TrySendItemAuctionReturnMail(
                player.CharacterId,
                record.Item,
                context => RemoveAuctionModelAndCreditOfflineParticipants(
                    context,
                    record,
                    grossAmount: 0ul,
                    saveItem: false,
                    onlineSeller: null,
                    refundCharacterId: topBidderId,
                    refundAmount: topBidderRefund,
                    onlineRefundOwner: topBidder)))
            {
                record.Item.CharacterId = previousItemCharacterId;
                return GenericError.ItemInventoryFull;
            }
            else
            {
                persistedAuctionDelete = true;
            }

            lock (syncRoot)
            {
                auctions.Remove(record);
                CreditOnlinePlayer(topBidder, CurrencyType.Credits, topBidderRefund);
            }

            if (canReturnToInventory)
                player.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
            else if (!persistedAuctionDelete)
                PersistAuctionDelete(record);

            return GenericError.Ok;
        }

        public GenericError PostCommodityOrder(IPlayer player, CommodityOrder order, out CommodityOrder postedOrder, IItemManager itemManager = null)
        {
            postedOrder = CloneOrder(order);
            if (player == null)
                return GenericError.Params;

            MarketplaceCommodityOrder record = null;
            bool persistInsertedOrder = false;

            lock (syncRoot)
            {
                bool forceImmediate = order.ForceImmediate;
                if (!forceImmediate)
                {
                    int maxOrders = order.IsBuyOrder
                        ? MarketplaceAccountLimits.GetMaxCommodityBuyOrders(player)
                        : MarketplaceAccountLimits.GetMaxCommoditySellOrders(player);
                    if (CountOwnedCommodityOrders(player.CharacterId, order.IsBuyOrder) >= maxOrders)
                        return GenericError.AuctionTooManyOrders;
                }

                if (order.IsBuyOrder)
                {
                    if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, order.Price))
                        return GenericError.VendorNotEnoughCash;

                    player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, order.Price);
                }
                else
                {
                    if (!player.Inventory.HasItemCount(order.Item2Id, order.Quantity))
                        return GenericError.ItemBadId;

                    uint unmatchedSellQuantity = forceImmediate
                        ? GetImmediateCommoditySellUnmatchedQuantity(order, itemManager)
                        : order.Quantity;
                    if (forceImmediate
                        && unmatchedSellQuantity != 0u
                        && !CanDeliverCommodityItemsToInventory(player, order.Item2Id, unmatchedSellQuantity, itemManager)
                        && !IsMarketplaceMailAvailable())
                    {
                        return GenericError.ItemInventoryFull;
                    }

                    player.Inventory.ItemDelete(order.Item2Id, order.Quantity, ItemUpdateReason.Auction);
                }

                postedOrder.CommodityOrderId = nextCommodityOrderId++;
                postedOrder.ListTime         = MarketplaceCommodityDuration.ResolveListFileTime(order.ListTime);
                postedOrder.ExpirationTime   = MarketplaceCommodityDuration.ResolveExpirationFileTime(order.ListTime, order.ExpirationTime, GetGameTableManager());

                record = new MarketplaceCommodityOrder
                {
                    OwnerCharacterId = player.CharacterId,
                    Order            = CloneOrder(postedOrder)
                };

                if (!forceImmediate)
                {
                    AddCommodityOrder(record);
                    persistInsertedOrder = true;
                }

                TryMatchCommodityOrder(record, itemManager);

                if (forceImmediate)
                {
                    GenericError immediateResult = FinaliseImmediateCommodityOrder(player, record, itemManager);
                    postedOrder = CloneOrder(record.Order);
                    return immediateResult;
                }
            }

            if (persistInsertedOrder && !PersistCommodityOrderInsert(record))
            {
                lock (syncRoot)
                {
                    RemoveCommodityOrderRecord(record);
                    if (postedOrder.CommodityOrderId + 1ul == nextCommodityOrderId)
                        nextCommodityOrderId--;
                }

                if (order.IsBuyOrder)
                    player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, order.Price);
                else
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, order.Item2Id, order.Quantity, ItemUpdateReason.Auction);

                return GenericError.DbFailure;
            }

            return GenericError.Ok;
        }

        public GenericError CancelCommodityOrder(IPlayer player, ulong orderId, uint item2Id, bool isBuyOrder, out CommodityOrder cancelledOrder, IItemManager itemManager = null)
        {
            cancelledOrder = new CommodityOrder
            {
                CommodityOrderId = orderId,
                Item2Id          = item2Id,
                IsBuyOrder       = isBuyOrder
            };

            if (player == null)
                return GenericError.Params;

            MarketplaceCommodityOrder record;
            bool returnByMail;
            lock (syncRoot)
            {
                record = commodityOrders.FirstOrDefault(o =>
                    o.OwnerCharacterId == player.CharacterId &&
                    o.Order.CommodityOrderId == orderId &&
                    o.Order.Item2Id == item2Id &&
                    o.Order.IsBuyOrder == isBuyOrder);
                if (record == null)
                    return GenericError.ItemBadId;

                cancelledOrder = CloneOrder(record.Order);

                returnByMail = !record.Order.IsBuyOrder
                    && !CanDeliverCommodityItemsToInventory(player, record.Order.Item2Id, record.Order.Quantity, itemManager);
                if (returnByMail)
                {
                    if (!IsMarketplaceMailAvailable()
                        || !TrySendCommodityAuctionReturnMail(
                            player.CharacterId,
                            record.Order.Item2Id,
                            record.Order.Quantity,
                            context => RemoveCommodityOrderModel(context, record),
                            itemManager))
                    {
                        return GenericError.ItemInventoryFull;
                    }
                }
            }

            if (!returnByMail && !PersistCommodityOrderDelete(record))
                return GenericError.DbFailure;

            lock (syncRoot)
            {
                RemoveCommodityOrderRecord(record);
            }

            if (record.Order.IsBuyOrder)
                player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
            else if (!returnByMail)
                player.Inventory.ItemCreate(InventoryLocation.Inventory, record.Order.Item2Id, record.Order.Quantity, ItemUpdateReason.Auction);

            return GenericError.Ok;
        }

        public void ValidateAuctionSearch(IGameTableManager gameTableManager, IItemManager itemManager, ClientAuctionsByFilterRequest request)
        {
            uint selectorCount = 0u;

            if (request.Item2FamilyId != 0u)
            {
                if (gameTableManager.Item2Family?.GetEntry(request.Item2FamilyId) == null)
                    throw new InvalidPacketValueException();
                selectorCount++;
            }

            if (request.Item2CategoryId != 0u)
            {
                if (gameTableManager.Item2Category?.GetEntry(request.Item2CategoryId) == null)
                    throw new InvalidPacketValueException();
                selectorCount++;
            }

            if (request.Item2TypeId != 0u)
            {
                if (gameTableManager.Item2Type?.GetEntry(request.Item2TypeId) == null)
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

            if (request.AuctionSort == AuctionSort.Property)
                throw new InvalidPacketValueException();

            foreach (IAuctionFilter filter in request.Filters)
                ValidateAuctionFilter(filter);
        }

        public void ValidateItem2(IItemManager itemManager, uint item2Id)
        {
            if (item2Id == 0u || itemManager.GetItemInfo(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public IItem GetInventoryItem(IPlayer player, ulong itemGuid)
        {
            IItem item = player.Inventory.GetItem(itemGuid);
            if (item == null || item.Info == null)
                throw new InvalidPacketValueException();

            return item;
        }

        private static bool CanPostAuctionItem(IPlayer player, IItem item)
        {
            return item.CharacterId == player.CharacterId
                && item.Location == InventoryLocation.Inventory
                && !item.Soulbound
                && !item.Info.IsEquippableBag();
        }

        public void ValidateCommodityOrder(IGameTableManager gameTableManager, IItemManager itemManager, CommodityOrder order)
        {
            ValidateItem2(itemManager, order.Item2Id);
            if (order.Quantity == 0u || order.PricePerUnit == 0ul || order.Price == 0ul)
                throw new InvalidPacketValueException();

            if (!TryComputeCommodityEscrowPrice(order.PricePerUnit, order.Quantity, out ulong expectedPrice)
                || order.Price != expectedPrice)
                throw new InvalidPacketValueException();

            GameFormulaEntry quantityLimit = gameTableManager.GameFormula.GetEntry(CommodityOrderQuantityLimitGameFormulaId);
            uint maxQuantity = quantityLimit?.Dataint02 ?? DefaultMaxCommodityOrderQuantity;
            if (order.Quantity > maxQuantity)
                throw new InvalidPacketValueException();

            ValidateCommodityDuration(gameTableManager, order);
        }

        private static void ValidateCommodityDuration(IGameTableManager gameTableManager, CommodityOrder order)
        {
            if (order.ListTime == 0ul && order.ExpirationTime == 0ul)
                return;

            ulong listFileTime = order.ListTime != 0ul
                ? order.ListTime
                : (ulong)DateTime.UtcNow.ToFileTimeUtc();

            if (order.ExpirationTime <= listFileTime)
                throw new InvalidPacketValueException();

            ulong durationSeconds = (order.ExpirationTime - listFileTime) / MarketplaceListingDuration.FileTimeTicksPerSecond;
            if (!MarketplaceListingDuration.IsValidTierDurationSeconds(durationSeconds, gameTableManager))
                throw new InvalidPacketValueException();
        }

        private IGameTableManager GetGameTableManager()
        {
            return gameTableManager;
        }

        private bool IsMarketplaceMailAvailable()
        {
            return MarketplaceMailDelivery.IsAvailable(TryGetCharacterDatabase());
        }

        private bool TrySendItemAuctionReturnMail(ulong recipientCharacterId, IItem item)
        {
            return MarketplaceMailDelivery.TrySendItemAuctionReturnMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item,
                playerManager);
        }

        private bool TrySendItemAuctionReturnMail(
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction)
        {
            return MarketplaceMailDelivery.TrySendItemAuctionReturnMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item,
                additionalSaveAction,
                playerManager);
        }

        private bool TrySendItemAuctionWonMail(ulong recipientCharacterId, IItem item)
        {
            return MarketplaceMailDelivery.TrySendItemAuctionWonMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item,
                playerManager);
        }

        private bool TrySendItemAuctionWonMail(
            ulong recipientCharacterId,
            IItem item,
            Action<CharacterContext> additionalSaveAction)
        {
            return MarketplaceMailDelivery.TrySendItemAuctionWonMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item,
                additionalSaveAction,
                playerManager);
        }

        private bool TrySendCommodityAuctionFillMail(
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction = null,
            IItemManager mailItemManager = null)
        {
            mailItemManager ??= itemManager;
            return MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item2Id,
                quantity,
                additionalSaveAction,
                playerManager,
                mailItemManager);
        }

        private bool TrySendCommodityAuctionReturnMail(
            ulong recipientCharacterId,
            uint item2Id,
            uint quantity,
            Action<CharacterContext> additionalSaveAction = null,
            IItemManager mailItemManager = null)
        {
            mailItemManager ??= itemManager;
            return MarketplaceMailDelivery.TrySendCommodityAuctionReturnMail(
                TryGetCharacterDatabase(),
                GetGameTableManager(),
                assetManager,
                recipientCharacterId,
                item2Id,
                quantity,
                additionalSaveAction,
                playerManager,
                mailItemManager);
        }

        private static MarketplaceAuctionModel ToAuctionModel(MarketplaceAuction record)
        {
            var model = new MarketplaceAuctionModel();
            ApplyAuctionModel(model, record.Auction, record.ExpiresAtUtc);
            model.ItemId = record.Item.Guid;
            return model;
        }

        private static ulong GetRemainingExpirationSeconds(ulong expiresAtUtc)
        {
            ulong now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return expiresAtUtc > now ? expiresAtUtc - now : 0ul;
        }

        private static void ApplyAuctionModel(MarketplaceAuctionModel model, AuctionInfo auction, ulong expiresAtUtc)
        {
            model.Id                    = auction.AuctionId;
            model.OwnerCharacterId      = auction.OwnerCharacterId;
            model.MinimumBid            = auction.MinimumBid;
            model.BuyoutPrice           = auction.BuyoutPrice;
            model.CurrentBid            = auction.CurrentBid;
            model.TopBidderCharacterId  = auction.TopBidderCharacterId;
            model.ExpirationTime        = expiresAtUtc;
            model.Item2Id               = auction.Item2Id;
            model.Quantity              = auction.Quantity;
            model.WorldRequirementItem2Id = auction.WorldRequirement_Item2Id;
            model.CircuitData           = auction.CircuitData;
            model.GlyphData             = auction.GlyphData;
            model.ThresholdData         = auction.ThresholdData;
            model.Unknown2              = auction.Unknown2;
            model.MicrochipIds          = SerializeMicrochipIds(auction.MicrochipIds);
        }

        private static AuctionInfo ToAuctionInfo(MarketplaceAuctionModel model)
        {
            return new AuctionInfo
            {
                AuctionId                = model.Id,
                OwnerCharacterId         = model.OwnerCharacterId,
                MinimumBid               = model.MinimumBid,
                BuyoutPrice              = model.BuyoutPrice,
                CurrentBid               = model.CurrentBid,
                TopBidderCharacterId     = model.TopBidderCharacterId,
                ExpirationTime           = GetRemainingExpirationSeconds(model.ExpirationTime),
                Item2Id                  = model.Item2Id,
                Quantity                 = model.Quantity,
                WorldRequirement_Item2Id = model.WorldRequirementItem2Id,
                CircuitData              = model.CircuitData,
                GlyphData                = model.GlyphData,
                ThresholdData            = model.ThresholdData,
                Unknown2                 = model.Unknown2,
                MicrochipIds             = DeserializeMicrochipIds(model.MicrochipIds)
            };
        }

        private static MarketplaceCommodityOrderModel ToCommodityOrderModel(MarketplaceCommodityOrder record)
        {
            return new MarketplaceCommodityOrderModel
            {
                Id               = record.Order.CommodityOrderId,
                OwnerCharacterId = record.OwnerCharacterId,
                Item2Id          = record.Order.Item2Id,
                Quantity         = record.Order.Quantity,
                PricePerUnit     = record.Order.PricePerUnit,
                Price            = record.Order.Price,
                IsBuyOrder       = record.Order.IsBuyOrder,
                ForceImmediate   = record.Order.ForceImmediate,
                ListTime         = record.Order.ListTime,
                ExpirationTime   = record.Order.ExpirationTime
            };
        }

        private static CommodityOrder ToCommodityOrder(MarketplaceCommodityOrderModel model)
        {
            return new CommodityOrder
            {
                CommodityOrderId = model.Id,
                Item2Id          = model.Item2Id,
                Quantity         = model.Quantity,
                PricePerUnit     = model.PricePerUnit,
                Price            = model.Price,
                IsBuyOrder       = model.IsBuyOrder,
                ForceImmediate   = model.ForceImmediate,
                ListTime         = model.ListTime,
                ExpirationTime   = model.ExpirationTime
            };
        }

        private static bool IsValidPersistedCommodityOrder(MarketplaceCommodityOrderModel model, IGameTableManager gameTableManager = null)
        {
            string reason = GetInvalidPersistedCommodityOrderReason(model, gameTableManager);
            if (reason == null)
                return true;

            log.Warn("GlobalMarketplaceManager.Initialise skipping commodity order id={0}: {1}.",
                model?.Id ?? 0ul,
                reason);
            return false;
        }

        private static string GetInvalidPersistedCommodityOrderReason(MarketplaceCommodityOrderModel model, IGameTableManager gameTableManager)
        {
            if (model == null)
                return "model is null";

            if (model.Id == 0ul)
                return "order id is zero";

            if (model.OwnerCharacterId == 0ul)
                return "owner character id is zero";

            if (model.Item2Id == 0u)
                return "item id is zero";

            if (gameTableManager?.Item != null && gameTableManager.Item.GetEntry(model.Item2Id) == null)
                return $"item id {model.Item2Id} is missing from Item2";

            if (model.Quantity == 0u)
                return "quantity is zero";

            if (model.PricePerUnit == 0ul)
                return "price per unit is zero";

            if (model.Price == 0ul)
                return "price is zero";

            if (model.ForceImmediate)
                return "ForceImmediate orders must not be persisted";

            if (!TryComputeCommodityEscrowPrice(model.PricePerUnit, model.Quantity, out ulong expectedPrice))
                return "price overflows quantity escrow";

            if (model.Price != expectedPrice)
                return $"price {model.Price} does not match pricePerUnit * quantity {expectedPrice}";

            if (model.ListTime != 0ul && model.ExpirationTime != 0ul && model.ExpirationTime <= model.ListTime)
                return "expiration time is not after list time";

            return null;
        }

        // AuctionInfo.MicrochipIds matches the shared-item 3-bit microchip id array that sits beside the circuit/glyph payload.
        private static string SerializeMicrochipIds(List<uint> microchipIds)
        {
            if (microchipIds == null || microchipIds.Count == 0)
                return string.Empty;

            return string.Join(',', microchipIds);
        }

        private static List<uint> DeserializeMicrochipIds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return [];

            try
            {
                return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => uint.Parse(v, CultureInfo.InvariantCulture))
                    .ToList();
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                log.Error(ex, "GlobalMarketplaceManager.DeserializeMicrochipIds failed to parse corrupt DB value '{0}'. Returning empty array.",
                    value.Length > 200 ? value[..200] + "..." : value);
                return [];
            }
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

        private void ProcessExpiredAuctions()
        {
            ulong now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<MarketplaceAuction> expired;
            lock (syncRoot)
                expired = auctions.Where(a => a.ExpiresAtUtc <= now).ToList();

            foreach (MarketplaceAuction record in expired)
            {
                bool shouldExpire;
                lock (syncRoot)
                    shouldExpire = auctions.Contains(record) && record.ExpiresAtUtc <= now;

                if (shouldExpire)
                    ExpireAuction(record);
            }
        }

        private void ExpireAuction(MarketplaceAuction record)
        {
            AuctionInfo auctionSnapshot = CloneAuction(record.Auction, record.ExpiresAtUtc);
            bool persistedDelete = false;

            if (record.Auction.TopBidderCharacterId != 0ul && record.Auction.CurrentBid != 0ul)
            {
                ulong winningBidderId = record.Auction.TopBidderCharacterId;
                IPlayer winningBidder = playerManager?.GetPlayer(winningBidderId);
                if (winningBidder != null && winningBidder.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
                {
                    ulong? previousItemCharacterId = record.Item.CharacterId;
                    record.Item.CharacterId = winningBidderId;
                    IPlayer onlineSeller = playerManager?.GetPlayer(record.Auction.OwnerCharacterId);
                    if (!PersistAuctionDeleteWithSellerCredit(record, record.Auction.CurrentBid, onlineSeller))
                    {
                        record.Item.CharacterId = previousItemCharacterId;
                        return;
                    }

                    winningBidder.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
                    PayOnlineSeller(record, record.Auction.CurrentBid, onlineSeller);
                    NotifyAuctionWon(winningBidder, auctionSnapshot);
                    persistedDelete = true;
                }
                else if (!CompleteAuctionSale(
                    record,
                    winningBidderId,
                    record.Auction.CurrentBid,
                    auctionSnapshot,
                    winningBidder,
                    true,
                    0ul,
                    0ul,
                    null,
                    out bool persistedMailDelete))
                {
                    return;
                }
                else
                {
                    persistedDelete = persistedMailDelete;
                }
            }
            else
            {
                if (!ReturnExpiredAuctionToOwner(record, auctionSnapshot, true, out bool persistedReturnDelete))
                    return;

                persistedDelete = persistedReturnDelete;
            }

            lock (syncRoot)
            {
                auctions.Remove(record);
            }

            if (!persistedDelete)
                PersistAuctionDelete(record);
        }

        private void ProcessExpiredCommodityOrders()
        {
            long nowFileTime = DateTime.UtcNow.ToFileTimeUtc();

            List<MarketplaceCommodityOrder> expired;
            lock (syncRoot)
            {
                expired = commodityOrders
                    .Where(o => o.Order.ExpirationTime != 0ul && (long)o.Order.ExpirationTime <= nowFileTime)
                    .ToList();
            }

            foreach (MarketplaceCommodityOrder record in expired)
            {
                bool shouldExpire;
                lock (syncRoot)
                    shouldExpire = commodityOrders.Contains(record);

                if (shouldExpire)
                    ExpireCommodityOrder(record);
            }
        }

        private void ExpireCommodityOrder(MarketplaceCommodityOrder record)
        {
            IPlayer owner = playerManager?.GetPlayer(record.OwnerCharacterId);
            bool directBuyRefund = record.Order.IsBuyOrder && owner != null;
            bool directSellReturn = !record.Order.IsBuyOrder
                && CanDeliverCommodityItemsToInventory(owner, record.Order.Item2Id, record.Order.Quantity, null);
            if (directBuyRefund || directSellReturn)
            {
                if (!PersistCommodityOrderDelete(record))
                    return;

                lock (syncRoot)
                {
                    RemoveCommodityOrderRecord(record);
                }

                if (directBuyRefund)
                    owner.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
                else
                    owner.Inventory.ItemCreate(InventoryLocation.Inventory, record.Order.Item2Id, record.Order.Quantity, ItemUpdateReason.Auction);

                owner.Session?.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved
                {
                    OrderRemoved = CloneOrder(record.Order),
                    Type         = AuctionEventType.Expire
                });
                return;
            }

            if (record.Order.IsBuyOrder)
            {
                if (!PersistCommodityOrderDeleteWithOfflineCredit(record, CurrencyType.Credits, record.Order.Price, owner))
                    return;

                owner?.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
            }
            else
            {
                if (!IsMarketplaceMailAvailable()
                    || !TrySendCommodityAuctionReturnMail(
                        record.OwnerCharacterId,
                        record.Order.Item2Id,
                        record.Order.Quantity,
                        context => RemoveCommodityOrderModel(context, record)))
                {
                    return;
                }
            }

            lock (syncRoot)
            {
                RemoveCommodityOrderRecord(record);
            }

            owner?.Session.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved
            {
                OrderRemoved = CloneOrder(record.Order),
                Type         = AuctionEventType.Expire
            });
        }

        private bool CompleteAuctionSale(
            MarketplaceAuction record,
            ulong buyerCharacterId,
            ulong saleAmount,
            AuctionInfo auctionSnapshot,
            IPlayer buyer,
            bool persistAuctionDeleteWithMail,
            ulong refundCharacterId,
            ulong refundAmount,
            IPlayer onlineRefundOwner,
            out bool persistedAuctionDelete)
        {
            persistedAuctionDelete = false;
            if (buyerCharacterId == 0ul)
                return false;

            ulong? previousCharacterId = record.Item.CharacterId;
            record.Item.CharacterId = buyerCharacterId;
            IPlayer onlineSeller = playerManager?.GetPlayer(record.Auction.OwnerCharacterId);
            bool deliverToInventory = buyer != null
                && buyer.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
            bool delivered = deliverToInventory
                ? TryDeliverAuctionItemToCharacter(buyerCharacterId, record.Item, buyer, TrySendItemAuctionWonMail)
                : persistAuctionDeleteWithMail
                    ? TrySendItemAuctionWonMail(
                        buyerCharacterId,
                        record.Item,
                        context => RemoveAuctionModelAndCreditOfflineParticipants(
                            context,
                            record,
                            saleAmount,
                            saveItem: false,
                            onlineSeller,
                            refundCharacterId,
                            refundAmount,
                            onlineRefundOwner))
                    : TryDeliverAuctionItemToCharacter(buyerCharacterId, record.Item, buyer, TrySendItemAuctionWonMail);
            if (!delivered)
            {
                record.Item.CharacterId = previousCharacterId;
                return false;
            }
            persistedAuctionDelete = !deliverToInventory && persistAuctionDeleteWithMail;

            PayOnlineSeller(record, saleAmount, onlineSeller);
            CreditOnlinePlayer(onlineRefundOwner, CurrencyType.Credits, refundAmount);
            NotifyAuctionWon(buyer, auctionSnapshot);
            return true;
        }

        private bool ReturnExpiredAuctionToOwner(
            MarketplaceAuction record,
            AuctionInfo auctionSnapshot,
            bool persistAuctionDeleteWithMail,
            out bool persistedAuctionDelete)
        {
            persistedAuctionDelete = false;
            ulong? previousCharacterId = record.Item.CharacterId;
            record.Item.CharacterId = record.Auction.OwnerCharacterId;
            IPlayer owner = playerManager?.GetPlayer(record.Auction.OwnerCharacterId);
            bool deliverToInventory = owner != null
                && owner.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
            bool delivered = deliverToInventory
                ? TryDeliverAuctionItemToCharacter(record.Auction.OwnerCharacterId, record.Item, owner, TrySendItemAuctionReturnMail)
                : persistAuctionDeleteWithMail
                    ? TrySendItemAuctionReturnMail(
                        record.Auction.OwnerCharacterId,
                        record.Item,
                        context => RemoveAuctionModel(context, record))
                    : TryDeliverAuctionItemToCharacter(record.Auction.OwnerCharacterId, record.Item, null, TrySendItemAuctionReturnMail);
            if (!delivered)
            {
                record.Item.CharacterId = previousCharacterId;
                return false;
            }
            persistedAuctionDelete = !deliverToInventory && persistAuctionDeleteWithMail;

            owner?.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
            {
                Auction = auctionSnapshot
            });
            return true;
        }

        private bool CanDeliverAuctionItemToCharacter(ulong characterId, IItem item, IPlayer player = null)
        {
            if (characterId == 0ul || item == null)
                return false;

            if (player != null && player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
                return true;

            return IsMarketplaceMailAvailable();
        }

        private bool TryDeliverAuctionItemToCharacter(
            ulong characterId,
            IItem item,
            Func<ulong, IItem, bool> mailDelivery = null)
        {
            return TryDeliverAuctionItemToCharacter(characterId, item, playerManager?.GetPlayer(characterId), mailDelivery);
        }

        private bool TryDeliverAuctionItemToCharacter(
            ulong characterId,
            IItem item,
            IPlayer player,
            Func<ulong, IItem, bool> mailDelivery = null)
        {
            mailDelivery ??= TrySendItemAuctionWonMail;

            if (characterId == 0ul || item == null)
                return false;

            if (player != null && player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
            {
                player.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
                return true;
            }

            return IsMarketplaceMailAvailable()
                && mailDelivery(characterId, item);
        }

        private static void RestoreRemovedAuctionItem(IPlayer player, IItem item)
        {
            if (player == null || item == null)
                return;

            item.CharacterId = player.CharacterId;
            if (item.Location == InventoryLocation.None)
                player.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
        }

        private static void NotifyAuctionWon(IPlayer winner, AuctionInfo auction)
        {
            winner?.Session.EnqueueMessageEncrypted(new ServerAuctionWon
            {
                Auction = auction
            });
        }

        private void PayOnlineSeller(MarketplaceAuction record, ulong grossAmount, IPlayer seller)
        {
            if (seller == null)
                return;

            ulong proceeds = MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(grossAmount, GetGameTableManager());
            seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, proceeds);
        }

        private void NotifyOutbid(ulong outbidCharacterId, AuctionInfo auction)
        {
            IPlayer outbidPlayer = playerManager?.GetPlayer(outbidCharacterId);
            outbidPlayer?.Session.EnqueueMessageEncrypted(new ServerAuctionOutbid
            {
                Auction = auction
            });
        }

        private static void CreditOnlinePlayer(IPlayer player, CurrencyType currencyType, ulong amount)
        {
            if (amount == 0ul || player == null)
                return;

            player.CurrencyManager.CurrencyAddAmount(currencyType, amount);
        }

        private void AddCommodityOrder(MarketplaceCommodityOrder record)
        {
            commodityOrders.Add(record);

            if (!commodityOrderBooks.TryGetValue(record.Order.Item2Id, out CommodityOrderBook book))
            {
                book = new CommodityOrderBook();
                commodityOrderBooks.Add(record.Order.Item2Id, book);
            }

            SortedDictionary<ulong, List<MarketplaceCommodityOrder>> priceLevels = record.Order.IsBuyOrder
                ? book.BuyOrders
                : book.SellOrders;
            if (!priceLevels.TryGetValue(record.Order.PricePerUnit, out List<MarketplaceCommodityOrder> ordersAtPrice))
            {
                ordersAtPrice = [];
                priceLevels.Add(record.Order.PricePerUnit, ordersAtPrice);
            }
            ordersAtPrice.Add(record);
        }

        private bool RemoveCommodityOrderRecord(MarketplaceCommodityOrder record)
        {
            if (!commodityOrders.Remove(record))
                return false;

            if (commodityOrderBooks.TryGetValue(record.Order.Item2Id, out CommodityOrderBook book))
            {
                SortedDictionary<ulong, List<MarketplaceCommodityOrder>> priceLevels = record.Order.IsBuyOrder
                    ? book.BuyOrders
                    : book.SellOrders;
                RemoveCommodityOrderFromPriceLevel(priceLevels, record);
                if (book.IsEmpty)
                    commodityOrderBooks.Remove(record.Order.Item2Id);
            }

            return true;
        }

        private static void RemoveCommodityOrderFromPriceLevel(
            SortedDictionary<ulong, List<MarketplaceCommodityOrder>> priceLevels,
            MarketplaceCommodityOrder record)
        {
            if (!priceLevels.TryGetValue(record.Order.PricePerUnit, out List<MarketplaceCommodityOrder> ordersAtPrice))
                return;

            ordersAtPrice.Remove(record);
            if (ordersAtPrice.Count == 0)
                priceLevels.Remove(record.Order.PricePerUnit);
        }

        private List<MarketplaceCommodityOrder> GetOppositeCommodityOrderCandidates(MarketplaceCommodityOrder incoming)
        {
            if (!commodityOrderBooks.TryGetValue(incoming.Order.Item2Id, out CommodityOrderBook book))
                return [];

            return incoming.Order.IsBuyOrder
                ? GetSellCommodityOrderCandidates(book, incoming.Order.PricePerUnit)
                : GetBuyCommodityOrderCandidates(book, incoming.Order.PricePerUnit);
        }

        private static List<MarketplaceCommodityOrder> GetSellCommodityOrderCandidates(CommodityOrderBook book, ulong maxPricePerUnit)
        {
            List<MarketplaceCommodityOrder> candidates = [];
            foreach (KeyValuePair<ulong, List<MarketplaceCommodityOrder>> priceLevel in book.SellOrders)
            {
                if (priceLevel.Key > maxPricePerUnit)
                    break;

                candidates.AddRange(priceLevel.Value);
            }

            return candidates;
        }

        private List<MarketplaceCommodityOrder> GetBuyCommodityOrderCandidates(uint item2Id, ulong minPricePerUnit)
        {
            if (!commodityOrderBooks.TryGetValue(item2Id, out CommodityOrderBook book))
                return [];

            return GetBuyCommodityOrderCandidates(book, minPricePerUnit);
        }

        private static List<MarketplaceCommodityOrder> GetBuyCommodityOrderCandidates(CommodityOrderBook book, ulong minPricePerUnit)
        {
            List<MarketplaceCommodityOrder> candidates = [];
            foreach (KeyValuePair<ulong, List<MarketplaceCommodityOrder>> priceLevel in book.BuyOrders.Reverse())
            {
                if (priceLevel.Key < minPricePerUnit)
                    break;

                candidates.AddRange(priceLevel.Value);
            }

            return candidates;
        }

        private void TryMatchCommodityOrder(MarketplaceCommodityOrder incoming, IItemManager itemManager)
        {
            foreach (MarketplaceCommodityOrder opposite in GetOppositeCommodityOrderCandidates(incoming))
            {
                if (incoming.Order.Quantity == 0u || opposite.Order.Quantity == 0u)
                    continue;

                if (incoming.Order.IsBuyOrder)
                {
                    if (incoming.Order.PricePerUnit < opposite.Order.PricePerUnit)
                        continue;
                }
                else if (opposite.Order.PricePerUnit < incoming.Order.PricePerUnit)
                    continue;

                uint fillQuantity = Math.Min(incoming.Order.Quantity, opposite.Order.Quantity);

                MarketplaceCommodityOrder buyOrder  = incoming.Order.IsBuyOrder ? incoming : opposite;
                MarketplaceCommodityOrder sellOrder = incoming.Order.IsBuyOrder ? opposite : incoming;

                ulong unitPrice = sellOrder.Order.PricePerUnit;
                ulong proceeds  = unitPrice * fillQuantity;

                ulong buyEscrowBefore     = buyOrder.Order.Price;
                uint buyRemainingQuantity = buyOrder.Order.Quantity - fillQuantity;
                uint sellRemainingQuantity = sellOrder.Order.Quantity - fillQuantity;
                ulong buyEscrowAfter      = buyOrder.Order.PricePerUnit * buyRemainingQuantity;
                ulong paidAmount          = proceeds;
                ulong sellEscrowAfter     = sellOrder.Order.PricePerUnit * sellRemainingQuantity;
                ulong sellerProceeds      = MarketplaceTransactionFee.CalculateCommoditySellerProceeds(proceeds, GetGameTableManager());
                ulong retainedAmount      = paidAmount + buyEscrowAfter;
                ulong buyerRefund         = buyEscrowBefore > retainedAmount ? buyEscrowBefore - retainedAmount : 0ul;

                bool persistedOrderChangesWithDelivery = false;
                IPlayer buyer = playerManager?.GetPlayer(buyOrder.OwnerCharacterId);
                IPlayer seller = playerManager?.GetPlayer(sellOrder.OwnerCharacterId);
                if (CanDeliverCommodityItemsToInventory(buyer, sellOrder.Order.Item2Id, fillQuantity, itemManager))
                {
                    if (!TryPersistCommodityFillOrderChangesWithOfflineCredits(
                        buyOrder,
                        buyRemainingQuantity,
                        buyEscrowAfter,
                        sellOrder,
                        sellRemainingQuantity,
                        sellEscrowAfter,
                        sellerProceeds,
                        seller,
                        buyerRefund,
                        buyer))
                    {
                        continue;
                    }

                    buyer.Inventory.ItemCreate(InventoryLocation.Inventory, sellOrder.Order.Item2Id, fillQuantity, ItemUpdateReason.Auction);
                    persistedOrderChangesWithDelivery = true;
                }
                else if (!TryDeliverCommodityItems(
                    buyOrder.OwnerCharacterId,
                    sellOrder.Order.Item2Id,
                    fillQuantity,
                    itemManager,
                    context => PersistCommodityFillOrderChangesWithOfflineCredits(
                        context,
                        buyOrder,
                        buyRemainingQuantity,
                        buyEscrowAfter,
                        sellOrder,
                        sellRemainingQuantity,
                        sellEscrowAfter,
                        sellerProceeds,
                        seller,
                        buyerRefund,
                        buyer),
                    out persistedOrderChangesWithDelivery))
                {
                    continue;
                }

                CreditOnlinePlayer(seller, CurrencyType.Credits, sellerProceeds);

                buyOrder.Order.Quantity  = buyRemainingQuantity;
                sellOrder.Order.Quantity = sellRemainingQuantity;
                buyOrder.Order.Price     = buyEscrowAfter;
                sellOrder.Order.Price    = sellEscrowAfter;

                CreditOnlinePlayer(buyer, CurrencyType.Credits, buyerRefund);

                NotifyCommodityPartialFill(buyOrder, AuctionEventType.Fill);
                NotifyCommodityPartialFill(sellOrder, AuctionEventType.Fill);

                if (buyOrder.Order.Quantity == 0u)
                    RemoveCommodityOrder(buyOrder, persistedOrderChangesWithDelivery);
                else if (IsRestingCommodityOrder(buyOrder))
                    PersistCommodityOrderUpdate(buyOrder, persistedOrderChangesWithDelivery);

                if (sellOrder.Order.Quantity == 0u)
                    RemoveCommodityOrder(sellOrder, persistedOrderChangesWithDelivery);
                else if (IsRestingCommodityOrder(sellOrder))
                    PersistCommodityOrderUpdate(sellOrder, persistedOrderChangesWithDelivery);
            }
        }

        private uint GetImmediateCommoditySellUnmatchedQuantity(CommodityOrder order, IItemManager itemManager)
        {
            uint remainingQuantity = order.Quantity;

            foreach (MarketplaceCommodityOrder candidate in GetBuyCommodityOrderCandidates(order.Item2Id, order.PricePerUnit))
            {
                if (remainingQuantity == 0u)
                    break;

                if (candidate.Order.PricePerUnit < order.PricePerUnit)
                    continue;

                uint fillQuantity = Math.Min(remainingQuantity, candidate.Order.Quantity);
                if (!CanDeliverCommodityItemsToCharacter(candidate.OwnerCharacterId, order.Item2Id, fillQuantity, itemManager))
                    continue;

                remainingQuantity -= fillQuantity;
            }

            return remainingQuantity;
        }

        private GenericError FinaliseImmediateCommodityOrder(IPlayer player, MarketplaceCommodityOrder record, IItemManager itemManager)
        {
            if (record.Order.Quantity == 0u)
                return GenericError.Ok;

            if (record.Order.IsBuyOrder)
            {
                player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
                ClearCommodityOrderRemainder(record.Order);
                return GenericError.Ok;
            }

            if (CanDeliverCommodityItemsToInventory(player, record.Order.Item2Id, record.Order.Quantity, itemManager))
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, record.Order.Item2Id, record.Order.Quantity, ItemUpdateReason.Auction);
                ClearCommodityOrderRemainder(record.Order);
                return GenericError.Ok;
            }

            if (IsMarketplaceMailAvailable()
                && TrySendCommodityAuctionReturnMail(
                    player.CharacterId,
                    record.Order.Item2Id,
                    record.Order.Quantity,
                    mailItemManager: itemManager))
            {
                ClearCommodityOrderRemainder(record.Order);
                return GenericError.Ok;
            }

            return GenericError.ItemInventoryFull;
        }

        private static void ClearCommodityOrderRemainder(CommodityOrder order)
        {
            order.Quantity = 0u;
            order.Price    = 0ul;
        }

        private bool TryDeliverCommodityItems(ulong characterId, uint item2Id, uint quantity, IItemManager itemManager)
        {
            return TryDeliverCommodityItems(characterId, item2Id, quantity, itemManager, null, out _);
        }

        private bool TryDeliverCommodityItems(
            ulong characterId,
            uint item2Id,
            uint quantity,
            IItemManager itemManager,
            Action<CharacterContext> additionalMailSaveAction,
            out bool persistedAdditionalMailSaveAction)
        {
            if (quantity == 0u)
            {
                persistedAdditionalMailSaveAction = false;
                return true;
            }

            IPlayer player = playerManager?.GetPlayer(characterId);
            if (CanDeliverCommodityItemsToInventory(player, item2Id, quantity, itemManager))
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, item2Id, quantity, ItemUpdateReason.Auction);
                persistedAdditionalMailSaveAction = false;
                return true;
            }

            bool deliveredByMail = IsMarketplaceMailAvailable()
                && TrySendCommodityAuctionFillMail(
                    characterId,
                    item2Id,
                    quantity,
                    additionalMailSaveAction,
                    itemManager);
            persistedAdditionalMailSaveAction = deliveredByMail && additionalMailSaveAction != null;
            return deliveredByMail;
        }

        private bool CanDeliverCommodityItemsToCharacter(ulong characterId, uint item2Id, uint quantity, IItemManager itemManager)
        {
            if (quantity == 0u)
                return true;

            IPlayer player = playerManager?.GetPlayer(characterId);
            return CanDeliverCommodityItemsToInventory(player, item2Id, quantity, itemManager)
                || IsMarketplaceMailAvailable();
        }

        private static bool CanDeliverCommodityItemsToInventory(IPlayer player, uint item2Id, uint quantity, IItemManager itemManager)
        {
            if (player == null)
                return false;

            if (quantity == 0u)
                return true;

            uint maxStackCount = itemManager?.GetItemInfo(item2Id)?.Entry?.MaxStackCount ?? 1u;
            if (maxStackCount == 0u)
                maxStackCount = 1u;

            uint requiredSlots = (uint)(((ulong)quantity + maxStackCount - 1ul) / maxStackCount);
            return player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) >= requiredSlots;
        }

        private void RemoveCommodityOrder(MarketplaceCommodityOrder record)
        {
            RemoveCommodityOrder(record, false);
        }

        private void RemoveCommodityOrder(MarketplaceCommodityOrder record, bool alreadyPersisted)
        {
            if (RemoveCommodityOrderRecord(record))
            {
                if (!alreadyPersisted)
                    PersistCommodityOrderDelete(record);
            }
        }

        private bool IsRestingCommodityOrder(MarketplaceCommodityOrder record)
        {
            return commodityOrders.Contains(record);
        }

        private void NotifyCommodityPartialFill(MarketplaceCommodityOrder record, AuctionEventType type)
        {
            IPlayer player = playerManager?.GetPlayer(record.OwnerCharacterId);
            player?.Session.EnqueueMessageEncrypted(new ServerCommodityAuctionFilledPartial
            {
                OrderFilled = CloneOrder(record.Order),
                Type        = type
            });
        }

        private static AuctionInfo CloneAuction(AuctionInfo auction, ulong? expiresAtUtc = null)
        {
            return new AuctionInfo
            {
                AuctionId                = auction.AuctionId,
                OwnerCharacterId         = auction.OwnerCharacterId,
                MinimumBid               = auction.MinimumBid,
                BuyoutPrice              = auction.BuyoutPrice,
                CurrentBid               = auction.CurrentBid,
                TopBidderCharacterId     = auction.TopBidderCharacterId,
                ExpirationTime           = expiresAtUtc.HasValue ? GetRemainingExpirationSeconds(expiresAtUtc.Value) : auction.ExpirationTime,
                Item2Id                  = auction.Item2Id,
                Quantity                 = auction.Quantity,
                WorldRequirement_Item2Id = auction.WorldRequirement_Item2Id,
                MicrochipIds             = auction.MicrochipIds.ToList(),
                CircuitData              = auction.CircuitData,
                GlyphData                = auction.GlyphData,
                ThresholdData            = auction.ThresholdData,
                Unknown2                 = auction.Unknown2
            };
        }

        private static ulong GetAuctionSortExpiration(MarketplaceAuction auction)
        {
            return auction.ExpiresAtUtc != 0ul ? auction.ExpiresAtUtc : auction.Auction.ExpirationTime;
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

        private int CountOwnedSellAuctions(ulong ownerCharacterId)
        {
            return auctions.Count(a => a.Auction.OwnerCharacterId == ownerCharacterId);
        }

        private int CountActiveAuctionBids(ulong bidderCharacterId)
        {
            return auctions.Count(a =>
                a.Auction.TopBidderCharacterId == bidderCharacterId
                && a.Auction.CurrentBid > 0ul);
        }

        private int CountOwnedCommodityOrders(ulong ownerCharacterId, bool isBuyOrder)
        {
            return commodityOrders.Count(o =>
                o.OwnerCharacterId == ownerCharacterId
                && o.Order.IsBuyOrder == isBuyOrder);
        }

        private static bool TryComputeCommodityEscrowPrice(ulong pricePerUnit, uint quantity, out ulong totalPrice)
        {
            totalPrice = 0ul;
            if (quantity == 0u || pricePerUnit == 0ul)
                return false;

            if (pricePerUnit > ulong.MaxValue / quantity)
                return false;

            totalPrice = pricePerUnit * quantity;
            return true;
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
                case PropertyMinAuctionFilter:
                case PropertyMaxAuctionFilter:
                case RuneSlotAuctionFilter:
                case EquippableByAuctionFilter:
                    throw new InvalidPacketValueException();
            }
        }

        private sealed class MarketplaceAuction
        {
            public AuctionInfo Auction { get; set; }
            public IItem Item { get; init; }
            public ulong ExpiresAtUtc { get; init; }
        }

        private sealed class CommodityOrderBook
        {
            public SortedDictionary<ulong, List<MarketplaceCommodityOrder>> BuyOrders { get; } = [];
            public SortedDictionary<ulong, List<MarketplaceCommodityOrder>> SellOrders { get; } = [];
            public bool IsEmpty => BuyOrders.Count == 0 && SellOrders.Count == 0;
        }

        private sealed class MarketplaceCommodityOrder
        {
            public ulong OwnerCharacterId { get; init; }
            public CommodityOrder Order { get; init; }
        }
    }
}
