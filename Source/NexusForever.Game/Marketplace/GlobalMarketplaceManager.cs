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
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Marketplace
{
    public sealed class GlobalMarketplaceManager : Singleton<GlobalMarketplaceManager>, IGlobalMarketplaceManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const uint MaxAuctionFilters = 7u;
        private const uint AuctionPageSize = 50u;
        private const uint CommodityOrderQuantityLimitGameFormulaId = 0x439u;
        private const uint DefaultMaxCommodityOrderQuantity = 200u;

        private readonly object syncRoot = new();
        private readonly List<MarketplaceAuction> auctions = [];
        private readonly List<MarketplaceCommodityOrder> commodityOrders = [];
        private ulong nextAuctionId = 1ul;
        private ulong nextCommodityOrderId = 1ul;
        private double expirationCheckTimer;

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
                        Item         = new Item(model.Item),
                        ExpiresAtUtc = model.ExpirationTime
                    });
                }

                foreach (MarketplaceCommodityOrderModel model in database.GetMarketplaceCommodityOrders())
                {
                    commodityOrders.Add(new MarketplaceCommodityOrder
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
            lock (syncRoot)
            {
                List<MarketplaceCommodityOrder> itemOrders = commodityOrders
                    .Where(o => o.Order.Item2Id == item2Id)
                    .ToList();

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
            lock (syncRoot)
            {
                List<MarketplaceAuction> matching = auctions
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

                int skip = checked((int)(request.Page * AuctionPageSize));
                List<AuctionInfo> page = sortedList
                    .Skip(skip)
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
        }

        public GenericError PostAuction(IPlayer player, IItem item, ulong minimumBid, ulong buyoutPrice, out AuctionInfo auction)
        {
            auction = new AuctionInfo
            {
                Item2Id     = item.Id,
                Quantity    = item.StackCount,
                MinimumBid  = minimumBid,
                BuyoutPrice = buyoutPrice
            };

            if (player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                if (CountOwnedSellAuctions(player.CharacterId) >= MarketplaceAccountLimits.GetMaxAuctionSellLots(player))
                    return GenericError.AuctionTooManyOrders;

                ulong expirationSeconds = MarketplaceAuctionDuration.GetDefaultExpirationSeconds();
                ulong expiresAtUtc = (ulong)DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

                auction.AuctionId        = nextAuctionId++;
                auction.OwnerCharacterId = player.CharacterId;
                auction.ExpirationTime   = expirationSeconds;

                player.Inventory.ItemRemove(item, ItemUpdateReason.Auction);

                var record = new MarketplaceAuction
                {
                    Auction      = CloneAuction(auction, expiresAtUtc),
                    Item         = item,
                    ExpiresAtUtc = expiresAtUtc
                };
                auctions.Add(record);
                PersistAuctionInsert(record);
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

            lock (syncRoot)
            {
                MarketplaceAuction record = auctions.FirstOrDefault(a => a.Auction.AuctionId == buyOrderSubmit.AuctionId);
                if (record == null || record.Auction.Item2Id != buyOrderSubmit.Item2Id)
                    return GenericError.ItemBadId;

                if (record.Auction.OwnerCharacterId == player.CharacterId)
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.Params;
                }

                ulong minimumOffer = record.Auction.CurrentBid == 0ul ? record.Auction.MinimumBid : record.Auction.CurrentBid + 1ul;
                bool isBuyout = record.Auction.BuyoutPrice != 0ul && buyOrderSubmit.AmountOffered >= record.Auction.BuyoutPrice;
                bool alreadyTopBidder = record.Auction.TopBidderCharacterId == player.CharacterId;
                if (!isBuyout && !alreadyTopBidder
                    && CountActiveAuctionBids(player.CharacterId) >= MarketplaceAccountLimits.GetMaxAuctionBids(player))
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.AuctionTooManyBids;
                }

                ulong acceptedAmount = isBuyout ? record.Auction.BuyoutPrice : buyOrderSubmit.AmountOffered;
                if (acceptedAmount < minimumOffer)
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.Params;
                }

                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, acceptedAmount))
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.VendorNotEnoughCash;
                }

                bool canDeliverBuyoutToInventory = !isBuyout
                    || player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
                if (isBuyout && !canDeliverBuyoutToInventory && !MarketplaceMailDelivery.IsAvailable)
                {
                    auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    return GenericError.ItemInventoryFull;
                }

                ulong previousBidderId = record.Auction.TopBidderCharacterId;
                ulong previousBid      = record.Auction.CurrentBid;
                RefundTopBidder(record);
                player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, acceptedAmount);
                record.Auction.CurrentBid           = acceptedAmount;
                record.Auction.TopBidderCharacterId = player.CharacterId;

                if (previousBidderId != 0ul && previousBidderId != player.CharacterId)
                    NotifyOutbid(previousBidderId, CloneAuction(record.Auction, record.ExpiresAtUtc));

                if (isBuyout)
                {
                    auctions.Remove(record);
                    AuctionInfo wonAuction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                    CompleteAuctionSale(record, player.CharacterId, acceptedAmount, wonAuction, player);
                    PersistAuctionDelete(record, record.Item);
                }
                else
                {
                    PersistAuctionUpdate(record);
                }

                auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                return GenericError.Ok;
            }
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

            lock (syncRoot)
            {
                MarketplaceAuction record = auctions.FirstOrDefault(a => a.Auction.AuctionId == auctionId);
                if (record == null || record.Auction.Item2Id != item2Id)
                    return GenericError.ItemBadId;

                auction = CloneAuction(record.Auction, record.ExpiresAtUtc);
                if (record.Auction.OwnerCharacterId != player.CharacterId)
                    return GenericError.Params;

                bool canReturnToInventory = player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u;
                if (!canReturnToInventory && !MarketplaceMailDelivery.IsAvailable)
                    return GenericError.ItemInventoryFull;

                record.Item.CharacterId = player.CharacterId;
                if (!canReturnToInventory
                    && !MarketplaceMailDelivery.TrySendItemAuctionReturnMail(player.CharacterId, record.Item))
                {
                    return GenericError.ItemInventoryFull;
                }

                auctions.Remove(record);
                RefundTopBidder(record);

                if (canReturnToInventory)
                    player.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);

                PersistAuctionDelete(record, record.Item);
                return GenericError.Ok;
            }
        }

        public GenericError PostCommodityOrder(IPlayer player, CommodityOrder order, out CommodityOrder postedOrder)
        {
            postedOrder = CloneOrder(order);
            if (player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                int maxOrders = order.IsBuyOrder
                    ? MarketplaceAccountLimits.GetMaxCommodityBuyOrders(player)
                    : MarketplaceAccountLimits.GetMaxCommoditySellOrders(player);
                if (CountOwnedCommodityOrders(player.CharacterId, order.IsBuyOrder) >= maxOrders)
                    return GenericError.AuctionTooManyOrders;

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

                    player.Inventory.ItemDelete(order.Item2Id, order.Quantity, ItemUpdateReason.Auction);
                }

                postedOrder.CommodityOrderId = nextCommodityOrderId++;
                postedOrder.ListTime         = MarketplaceCommodityDuration.ResolveListFileTime(order.ListTime);
                postedOrder.ExpirationTime   = MarketplaceCommodityDuration.ResolveExpirationFileTime(order.ListTime, order.ExpirationTime);

                var record = new MarketplaceCommodityOrder
                {
                    OwnerCharacterId = player.CharacterId,
                    Order            = CloneOrder(postedOrder)
                };
                commodityOrders.Add(record);
                PersistCommodityOrderInsert(record);
                TryMatchCommodityOrder(record);
            }

            return GenericError.Ok;
        }

        public GenericError CancelCommodityOrder(IPlayer player, ulong orderId, uint item2Id, bool isBuyOrder, out CommodityOrder cancelledOrder)
        {
            cancelledOrder = new CommodityOrder
            {
                CommodityOrderId = orderId,
                Item2Id          = item2Id,
                IsBuyOrder       = isBuyOrder
            };

            if (player == null)
                return GenericError.Params;

            lock (syncRoot)
            {
                MarketplaceCommodityOrder record = commodityOrders.FirstOrDefault(o =>
                    o.OwnerCharacterId == player.CharacterId &&
                    o.Order.CommodityOrderId == orderId &&
                    o.Order.Item2Id == item2Id &&
                    o.Order.IsBuyOrder == isBuyOrder);
                if (record == null)
                    return GenericError.ItemBadId;

                cancelledOrder = CloneOrder(record.Order);

                bool returnByMail = !record.Order.IsBuyOrder
                    && player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) == 0u;
                if (returnByMail)
                {
                    if (!MarketplaceMailDelivery.IsAvailable
                        || !MarketplaceMailDelivery.TrySendCommodityAuctionReturnMail(
                            player.CharacterId,
                            record.Order.Item2Id,
                            record.Order.Quantity))
                    {
                        return GenericError.ItemInventoryFull;
                    }
                }

                commodityOrders.Remove(record);
                if (record.Order.IsBuyOrder)
                    player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, record.Order.Price);
                else if (!returnByMail)
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, record.Order.Item2Id, record.Order.Quantity, ItemUpdateReason.Auction);

                PersistCommodityOrderDelete(record);
                return GenericError.Ok;
            }
        }

        public void ValidateAuctionSearch(IGameTableManager gameTableManager, IItemManager itemManager, ClientAuctionsByFilterRequest request)
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

        private static void PersistAuctionInsert(MarketplaceAuction record)
        {
            Persist(context =>
            {
                if (record.Item is Item item)
                    item.Save(context);

                context.MarketplaceAuction.Add(ToAuctionModel(record));
            });
        }

        private static void PersistAuctionUpdate(MarketplaceAuction record)
        {
            Persist(context =>
            {
                MarketplaceAuctionModel model = context.MarketplaceAuction.Single(a => a.Id == record.Auction.AuctionId);
                ApplyAuctionModel(model, record.Auction, record.ExpiresAtUtc);
            });
        }

        private static void PersistCommodityOrderUpdate(MarketplaceCommodityOrder record)
        {
            Persist(context =>
            {
                MarketplaceCommodityOrderModel model = context.MarketplaceCommodityOrder
                    .Single(o => o.Id == record.Order.CommodityOrderId);
                model.Quantity     = record.Order.Quantity;
                model.Price        = record.Order.Price;
                model.PricePerUnit = record.Order.PricePerUnit;
            });
        }

        private static void PersistAuctionDelete(MarketplaceAuction record, IItem item)
        {
            Persist(context =>
            {
                MarketplaceAuctionModel model = context.MarketplaceAuction.SingleOrDefault(a => a.Id == record.Auction.AuctionId);
                if (model != null)
                    context.MarketplaceAuction.Remove(model);

                if (item is Item itemEntity)
                    itemEntity.Save(context);
            });
        }

        private static void PersistCommodityOrderInsert(MarketplaceCommodityOrder record)
        {
            Persist(context => context.MarketplaceCommodityOrder.Add(ToCommodityOrderModel(record)));
        }

        private static void PersistCommodityOrderDelete(MarketplaceCommodityOrder record)
        {
            Persist(context =>
            {
                MarketplaceCommodityOrderModel model = context.MarketplaceCommodityOrder
                    .SingleOrDefault(o => o.Id == record.Order.CommodityOrderId);
                if (model != null)
                    context.MarketplaceCommodityOrder.Remove(model);
            });
        }

        private static void Persist(Action<CharacterContext> action)
        {
            CharacterDatabase database = TryGetCharacterDatabase();
            if (database == null)
            {
                log.Warn("GlobalMarketplaceManager.Persist skipped: CharacterDatabase is null. In-memory state may diverge from database.");
                return;
            }

            database.Save(action).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static CharacterDatabase TryGetCharacterDatabase()
        {
            if (LegacyServiceProvider.Provider?.GetService<DatabaseManager>() is not DatabaseManager manager)
                return null;

            return manager.GetDatabase<CharacterDatabase>();
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
            model.UnknownArray          = SerializeUnknownArray(auction.UnknownArray);
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
                UnknownArray             = DeserializeUnknownArray(model.UnknownArray)
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

        private static string SerializeUnknownArray(List<uint> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            return string.Join(',', values);
        }

        private static List<uint> DeserializeUnknownArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return [];

            try
            {
                return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => uint.Parse(v, CultureInfo.InvariantCulture))
                    .ToList();
            }
            catch (FormatException ex)
            {
                log.Error(ex, "GlobalMarketplaceManager.DeserializeUnknownArray failed to parse corrupt DB value '{0}'. Returning empty array.",
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

            lock (syncRoot)
            {
                foreach (MarketplaceAuction record in auctions.Where(a => a.ExpiresAtUtc <= now).ToList())
                    ExpireAuction(record);
            }
        }

        private void ExpireAuction(MarketplaceAuction record)
        {
            auctions.Remove(record);
            AuctionInfo auctionSnapshot = CloneAuction(record.Auction, record.ExpiresAtUtc);

            if (record.Auction.TopBidderCharacterId != 0ul && record.Auction.CurrentBid != 0ul)
            {
                ulong winningBidderId = record.Auction.TopBidderCharacterId;
                IPlayer winningBidder = PlayerManager.Instance.GetPlayer(winningBidderId);
                CompleteAuctionSale(record, winningBidderId, record.Auction.CurrentBid, auctionSnapshot, winningBidder);
            }
            else
                ReturnExpiredAuctionToOwner(record, auctionSnapshot);

            PersistAuctionDelete(record, record.Item);
        }

        private void ProcessExpiredCommodityOrders()
        {
            long nowFileTime = DateTime.UtcNow.ToFileTimeUtc();

            lock (syncRoot)
            {
                foreach (MarketplaceCommodityOrder record in commodityOrders
                    .Where(o => o.Order.ExpirationTime != 0ul && (long)o.Order.ExpirationTime <= nowFileTime)
                    .ToList())
                {
                    ExpireCommodityOrder(record);
                }
            }
        }

        private void ExpireCommodityOrder(MarketplaceCommodityOrder record)
        {
            commodityOrders.Remove(record);

            IPlayer owner = PlayerManager.Instance.GetPlayer(record.OwnerCharacterId);
            if (record.Order.IsBuyOrder)
            {
                CreditCharacter(record.OwnerCharacterId, CurrencyType.Credits, record.Order.Price);
            }
            else
            {
                DeliverCommodityItems(record.OwnerCharacterId, record.Order.Item2Id, record.Order.Quantity);
            }

            owner?.Session.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved
            {
                OrderRemoved = CloneOrder(record.Order),
                Type         = AuctionEventType.Expire
            });

            PersistCommodityOrderDelete(record);
        }

        private void CompleteAuctionSale(
            MarketplaceAuction record,
            ulong buyerCharacterId,
            ulong saleAmount,
            AuctionInfo auctionSnapshot,
            IPlayer buyer = null)
        {
            if (buyerCharacterId == 0ul)
                return;

            PaySeller(record, saleAmount);
            record.Item.CharacterId = buyerCharacterId;

            if (buyer != null && buyer.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
                buyer.Inventory.AddItem(record.Item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
            else
                DeliverAuctionItemToCharacter(buyerCharacterId, record.Item, MarketplaceMailDelivery.TrySendItemAuctionWonMail);

            NotifyAuctionWon(buyer, auctionSnapshot);
        }

        private static void ReturnExpiredAuctionToOwner(MarketplaceAuction record, AuctionInfo auctionSnapshot)
        {
            record.Item.CharacterId = record.Auction.OwnerCharacterId;
            DeliverAuctionItemToCharacter(record.Auction.OwnerCharacterId, record.Item, MarketplaceMailDelivery.TrySendItemAuctionReturnMail);

            IPlayer owner = PlayerManager.Instance.GetPlayer(record.Auction.OwnerCharacterId);
            owner?.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
            {
                Auction = auctionSnapshot
            });
        }

        private static void DeliverAuctionItemToCharacter(
            ulong characterId,
            IItem item,
            Func<ulong, IItem, bool> mailDelivery = null)
        {
            mailDelivery ??= MarketplaceMailDelivery.TrySendItemAuctionWonMail;

            IPlayer player = PlayerManager.Instance.GetPlayer(characterId);
            if (player != null && player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
            {
                player.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Auction);
                return;
            }

            mailDelivery(characterId, item);
        }

        private static void NotifyAuctionWon(IPlayer winner, AuctionInfo auction)
        {
            winner?.Session.EnqueueMessageEncrypted(new ServerAuctionWon
            {
                Auction = auction
            });
        }

        private static void RefundTopBidder(MarketplaceAuction record)
        {
            if (record.Auction.TopBidderCharacterId == 0ul || record.Auction.CurrentBid == 0ul)
                return;

            CreditCharacter(record.Auction.TopBidderCharacterId, CurrencyType.Credits, record.Auction.CurrentBid);
        }

        private static void PaySeller(MarketplaceAuction record, ulong grossAmount)
        {
            ulong proceeds = MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(grossAmount);
            CreditCharacter(record.Auction.OwnerCharacterId, CurrencyType.Credits, proceeds);
        }

        private static void NotifyOutbid(ulong outbidCharacterId, AuctionInfo auction)
        {
            IPlayer outbidPlayer = PlayerManager.Instance.GetPlayer(outbidCharacterId);
            outbidPlayer?.Session.EnqueueMessageEncrypted(new ServerAuctionOutbid
            {
                Auction = auction
            });
        }

        private static void CreditCharacter(ulong characterId, CurrencyType currencyType, ulong amount)
        {
            if (amount == 0ul)
                return;

            IPlayer player = PlayerManager.Instance.GetPlayer(characterId);
            if (player != null)
            {
                player.CurrencyManager.CurrencyAddAmount(currencyType, amount);
                return;
            }

            if (currencyType == CurrencyType.Credits
                && MarketplaceMailDelivery.TrySendMarketplaceCreditMail(
                    characterId,
                    amount,
                    "Marketplace",
                    "Credits from the marketplace are enclosed."))
                return;

            TryGetCharacterDatabase()?.CreditCharacterCurrency(characterId, (byte)currencyType, amount);
        }

        /// <summary>
        /// Attempts to match an incoming commodity order against all opposite-side orders.
        /// </summary>
        /// <remarks>
        /// NOTE: This performs a full scan over all opposite orders for every new post,
        /// resulting in O(n²) behaviour under load. A deeper refactor (e.g. price-indexed
        /// order book) is needed to bring this to O(log n) per match.
        /// </remarks>
        private void TryMatchCommodityOrder(MarketplaceCommodityOrder incoming)
        {
            IEnumerable<MarketplaceCommodityOrder> candidates = commodityOrders
                .Where(o => o.Order.Item2Id == incoming.Order.Item2Id && o.Order.IsBuyOrder != incoming.Order.IsBuyOrder);

            candidates = incoming.Order.IsBuyOrder
                ? candidates.OrderBy(o => o.Order.PricePerUnit)
                : candidates.OrderByDescending(o => o.Order.PricePerUnit);

            foreach (MarketplaceCommodityOrder opposite in candidates.ToList())
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

                CreditCharacter(sellOrder.OwnerCharacterId, CurrencyType.Credits, MarketplaceTransactionFee.CalculateCommoditySellerProceeds(proceeds));
                DeliverCommodityItems(buyOrder.OwnerCharacterId, sellOrder.Order.Item2Id, fillQuantity);

                ulong buyPriceBefore = buyOrder.Order.Price;
                buyOrder.Order.Quantity  -= fillQuantity;
                sellOrder.Order.Quantity -= fillQuantity;
                buyOrder.Order.Price     = buyOrder.Order.PricePerUnit * buyOrder.Order.Quantity;
                sellOrder.Order.Price    = sellOrder.Order.PricePerUnit * sellOrder.Order.Quantity;

                if (buyPriceBefore > buyOrder.Order.Price)
                    CreditCharacter(buyOrder.OwnerCharacterId, CurrencyType.Credits, buyPriceBefore - buyOrder.Order.Price);

                NotifyCommodityPartialFill(buyOrder, AuctionEventType.Fill);
                NotifyCommodityPartialFill(sellOrder, AuctionEventType.Fill);

                if (buyOrder.Order.Quantity == 0u)
                    RemoveCommodityOrder(buyOrder);
                else
                    PersistCommodityOrderUpdate(buyOrder);

                if (sellOrder.Order.Quantity == 0u)
                    RemoveCommodityOrder(sellOrder);
                else
                    PersistCommodityOrderUpdate(sellOrder);
            }
        }

        private static void DeliverCommodityItems(ulong characterId, uint item2Id, uint quantity)
        {
            if (quantity == 0u)
                return;

            IPlayer player = PlayerManager.Instance.GetPlayer(characterId);
            if (player != null && player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, item2Id, quantity, ItemUpdateReason.Auction);
                return;
            }

            MarketplaceMailDelivery.TrySendCommodityAuctionFillMail(characterId, item2Id, quantity);
        }

        private void RemoveCommodityOrder(MarketplaceCommodityOrder record)
        {
            commodityOrders.Remove(record);
            PersistCommodityOrderDelete(record);
        }

        private static void NotifyCommodityPartialFill(MarketplaceCommodityOrder record, AuctionEventType type)
        {
            IPlayer player = PlayerManager.Instance.GetPlayer(record.OwnerCharacterId);
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
                UnknownArray             = auction.UnknownArray.ToList(),
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
                case PropertyMinAuctionFilter property when float.IsNaN(property.Minimum) || float.IsInfinity(property.Minimum):
                    throw new InvalidPacketValueException();
                case PropertyMaxAuctionFilter property when float.IsNaN(property.Maximum) || float.IsInfinity(property.Maximum):
                    throw new InvalidPacketValueException();
            }
        }

        private sealed class MarketplaceAuction
        {
            public AuctionInfo Auction { get; set; }
            public IItem Item { get; init; }
            public ulong ExpiresAtUtc { get; init; }
        }

        private sealed class MarketplaceCommodityOrder
        {
            public ulong OwnerCharacterId { get; init; }
            public CommodityOrder Order { get; init; }
        }
    }
}
