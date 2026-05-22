using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Marketplace;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;

namespace NexusForever.Game.Tests.Marketplace;

[Collection(LegacyServiceProviderCollection.Name)]
public class MarketplaceAuctionHandlerTests
{
    private const uint ItemId = 7001u;
    private const ulong ItemGuid = 0xABCDEFul;
    private const uint SellerGuid = 101u;
    private const uint BuyerGuid = 202u;
    private const ulong SellerCharacterId = 1001ul;
    private const ulong BuyerCharacterId = 2002ul;
    private const ulong MinimumBid = 100ul;
    private const ulong BuyoutPrice = 250ul;

    [Fact]
    public void AuctionPostSearchAndBuyout_UsesTransientOrderBookAndTransfersItem()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out RecordingDispatchProxy<IItem> itemProxy);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            playerManager.AddPlayer(seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

            var sellHandler = new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance);
            sellHandler.HandleMessage(sellerSession, CreateSellRequest());

            RecordingDispatchProxy<IInventory>.Invocation removeCall =
                Assert.Single(sellerInventoryProxy.GetInvocations(nameof(IInventory.ItemRemove)));
            Assert.Same(item, removeCall.Arguments[0]);
            Assert.Equal(ItemUpdateReason.Auction, removeCall.Arguments[1]);

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            Assert.Equal(GenericError.Ok, postResult.Result);
            Assert.Equal(ItemId, postResult.Auction.Item2Id);
            Assert.Equal(1u, postResult.Auction.Quantity);
            Assert.Equal(MinimumBid, postResult.Auction.MinimumBid);
            Assert.Equal(BuyoutPrice, postResult.Auction.BuyoutPrice);
            Assert.Equal(SellerCharacterId, postResult.Auction.OwnerCharacterId);
            Assert.NotEqual(0ul, postResult.Auction.AuctionId);
            Assert.Single(GetMessages<ServerMarketplaceStatus>(sellerSessionProxy));

            IItemManager itemManager = CreateItemManager(itemInfo);
            var searchHandler = new ClientAuctionsByFilterRequestHandler(
                NullLogger<ClientAuctionsByFilterRequestHandler>.Instance,
                CreateGameTableManager(),
                itemManager);
            IWorldSession searchSession = CreateSession(303u, 3003ul, out _, out _, out RecordingDispatchProxy<IWorldSession> searchSessionProxy, out _);

            searchHandler.HandleMessage(searchSession, CreateSearchRequest());

            ServerAuctionSearchResults searchResults = Assert.Single(GetMessages<ServerAuctionSearchResults>(searchSessionProxy));
            AuctionInfo auction = Assert.Single(searchResults.Auctions);
            Assert.Equal(postResult.Auction.AuctionId, auction.AuctionId);
            Assert.Equal(ItemId, auction.Item2Id);
            Assert.Equal(BuyoutPrice, auction.BuyoutPrice);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out _);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 3u);
            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(
                NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance,
                itemManager);

            buyHandler.HandleMessage(buyerSession, CreateBuyoutRequest(postResult.Auction.AuctionId));

            RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
            Assert.Equal(BuyoutPrice, debit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
                Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, sellerCredit.Arguments[0]);
            Assert.Equal(237ul, sellerCredit.Arguments[1]);

            RecordingDispatchProxy<IInventory>.Invocation addCall =
                Assert.Single(buyerInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Same(item, addCall.Arguments[0]);
            Assert.Equal(InventoryLocation.Inventory, addCall.Arguments[1]);
            Assert.Equal(ItemUpdateReason.Auction, addCall.Arguments[2]);
            Assert.Equal(BuyerCharacterId, itemProxy.GetInvocations("set_" + nameof(IItem.CharacterId)).Last().Arguments[0]);

            ServerAuctionWon won = Assert.Single(GetMessages<ServerAuctionWon>(buyerSessionProxy));
            Assert.Equal(postResult.Auction.AuctionId, won.Auction.AuctionId);
            Assert.Equal(BuyerCharacterId, won.Auction.TopBidderCharacterId);
            Assert.Equal(BuyoutPrice, won.Auction.CurrentBid);

            ServerAuctionBidResult bidResult = Assert.Single(GetMessages<ServerAuctionBidResult>(buyerSessionProxy));
            Assert.Equal(GenericError.Ok, bidResult.Result);
            Assert.Equal(postResult.Auction.AuctionId, bidResult.Auction.AuctionId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionBid_OutbidNotifiesPreviousBidderAndRefundsCredits()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            playerManager.AddPlayer(seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

            var sellHandler = new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance);
            sellHandler.HandleMessage(sellerSession, CreateSellRequest());

            IItemManager itemManager = CreateItemManager(itemInfo);
            const ulong firstBidderCharacterId = 3003ul;
            const ulong secondBidderCharacterId = BuyerCharacterId;
            IWorldSession firstBidderSession = CreateSession(301u, firstBidderCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> firstBidderCurrencyProxy, out RecordingDispatchProxy<IWorldSession> firstBidderSessionProxy, out IPlayer firstBidder);
            IWorldSession secondBidderSession = CreateSession(BuyerGuid, secondBidderCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> secondBidderCurrencyProxy, out _, out IPlayer secondBidder);
            playerManager.AddPlayer(firstBidder);
            playerManager.AddPlayer(secondBidder);
            firstBidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            secondBidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            ulong auctionId = postResult.Auction.AuctionId;

            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, itemManager);
            buyHandler.HandleMessage(firstBidderSession, CreateBidRequest(auctionId, MinimumBid));
            buyHandler.HandleMessage(secondBidderSession, CreateBidRequest(auctionId, MinimumBid + 50ul));

            ServerAuctionOutbid outbid = Assert.Single(GetMessages<ServerAuctionOutbid>(firstBidderSessionProxy));
            Assert.Equal(auctionId, outbid.Auction.AuctionId);
            Assert.Equal(MinimumBid + 50ul, outbid.Auction.CurrentBid);
            Assert.Equal(secondBidderCharacterId, outbid.Auction.TopBidderCharacterId);

            RecordingDispatchProxy<ICurrencyManager>.Invocation refund =
                Assert.Single(firstBidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, refund.Arguments[0]);
            Assert.Equal(MinimumBid, refund.Arguments[1]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionExpire_WithHighBid_CompletesSaleToTopBidder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            playerManager.AddPlayer(seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

            var sellHandler = new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance);
            sellHandler.HandleMessage(sellerSession, CreateSellRequest());

            IItemManager itemManager = CreateItemManager(itemInfo);
            IWorldSession bidderSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> bidderInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out RecordingDispatchProxy<IWorldSession> bidderSessionProxy, out IPlayer bidder);
            playerManager.AddPlayer(bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            bidderInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            ulong auctionId = postResult.Auction.AuctionId;

            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, itemManager);
            buyHandler.HandleMessage(bidderSession, CreateBidRequest(auctionId, MinimumBid));

            ForceAuctionExpiration(GlobalMarketplaceManager.Instance);
            GlobalMarketplaceManager.Instance.Update(2000d);

            RecordingDispatchProxy<IInventory>.Invocation addCall =
                Assert.Single(bidderInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Same(item, addCall.Arguments[0]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
                Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(95ul, sellerCredit.Arguments[1]);

            ServerAuctionWon won = Assert.Single(GetMessages<ServerAuctionWon>(bidderSessionProxy));
            Assert.Equal(auctionId, won.Auction.AuctionId);
            Assert.Empty(GetMessages<ServerAuctionExpired>(sellerSessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionExpire_WithHighBid_OfflineWinner_StillCreditsSeller()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            playerManager.AddPlayer(seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

            var sellHandler = new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance);
            sellHandler.HandleMessage(sellerSession, CreateSellRequest());

            IItemManager itemManager = CreateItemManager(itemInfo);
            IWorldSession bidderSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> bidderInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out RecordingDispatchProxy<IWorldSession> bidderSessionProxy, out IPlayer bidder);
            playerManager.AddPlayer(bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            ulong auctionId = postResult.Auction.AuctionId;

            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, itemManager);
            buyHandler.HandleMessage(bidderSession, CreateBidRequest(auctionId, MinimumBid));

            playerManager.RemovePlayer(bidder);
            ForceAuctionExpiration(GlobalMarketplaceManager.Instance);
            GlobalMarketplaceManager.Instance.Update(2000d);

            RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
                Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(95ul, sellerCredit.Arguments[1]);
            Assert.Empty(bidderInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(GetMessages<ServerAuctionWon>(bidderSessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionBuyout_FullInventoryWithoutMailDelivery_ReturnsInventoryFull()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            playerManager.AddPlayer(seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

            var sellHandler = new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance);
            sellHandler.HandleMessage(sellerSession, CreateSellRequest());

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            ulong auctionId = postResult.Auction.AuctionId;

            IItemManager itemManager = CreateItemManager(itemInfo);
            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out _);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);

            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, itemManager);
            buyHandler.HandleMessage(buyerSession, CreateBuyoutRequest(auctionId));

            ServerAuctionBidResult bidResult = Assert.Single(GetMessages<ServerAuctionBidResult>(buyerSessionProxy));
            Assert.Equal(GenericError.ItemInventoryFull, bidResult.Result);
            Assert.Equal(auctionId, bidResult.Auction.AuctionId);
            Assert.Empty(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(buyerInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(GetMessages<ServerAuctionWon>(buyerSessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommoditySellOrderSubmit_WithInvalidDurationThrows()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        var handler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u, includeListingDurationFormula: true),
            CreateItemManager(itemInfo));
        IWorldSession session = CreateSession(404u, 4004ul, out _, out _, out _, out _);

        ClientCommoditySellOrderSubmit request = CreateCommoditySellRequest(quantity: 1u);
        ulong listTime = (ulong)DateTime.UtcNow.ToFileTimeUtc();
        request.Order.ListTime = listTime;
        request.Order.ExpirationTime = listTime + 9999ul * MarketplaceListingDuration.FileTimeTicksPerSecond;

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, request));
    }

    [Fact]
    public void CommoditySellOrderSubmit_WithValidDurationTierPreservesExpiration()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        IWorldSession session = CreateSession(505u, 5005ul, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);

        var handler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u, includeListingDurationFormula: true),
            CreateItemManager(itemInfo));

        ClientCommoditySellOrderSubmit request = CreateCommoditySellRequest(quantity: 2u);
        ulong listTime = (ulong)DateTime.UtcNow.ToFileTimeUtc();
        const ulong tierSeconds = 7200ul;
        request.Order.ListTime = listTime;
        request.Order.ExpirationTime = listTime + tierSeconds * MarketplaceListingDuration.FileTimeTicksPerSecond;

        handler.HandleMessage(session, request);

        ServerCommodityOrderResult postResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(sessionProxy));
        Assert.Equal(GenericError.Ok, postResult.Result);
        Assert.Equal(listTime, postResult.OrderPosted.ListTime);
        Assert.Equal(request.Order.ExpirationTime, postResult.OrderPosted.ExpirationTime);
    }

    [Fact]
    public void CommoditySellOrderSubmit_WithQuantityAboveClientLimitThrows()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        var handler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u),
            CreateItemManager(itemInfo));
        IWorldSession session = CreateSession(404u, 4004ul, out _, out _, out _, out _);

        ClientCommoditySellOrderSubmit request = CreateCommoditySellRequest(quantity: 201u);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, request));
    }

    [Fact]
    public void CommodityOrderCancel_WhenOrderNotFound_ReturnsItemBadIdResult()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        IItemManager itemManager = CreateItemManager(itemInfo);
        IWorldSession session = CreateSession(606u, 6006ul, out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);

        var cancelHandler = new ClientCommodityOrderCancelHandler(
            NullLogger<ClientCommodityOrderCancelHandler>.Instance,
            itemManager);
        cancelHandler.HandleMessage(session, CreateCommodityCancelRequest(new CommodityOrder
        {
            CommodityOrderId = 999ul,
            Item2Id          = ItemId,
            IsBuyOrder       = true
        }));

        ServerCommodityOrderResult cancelResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(sessionProxy));
        Assert.Equal(GenericError.ItemBadId, cancelResult.Result);
        Assert.Equal(999ul, cancelResult.OrderPosted.CommodityOrderId);
        Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));
    }

    [Fact]
    public void CommodityOrders_CrossMatch_FillsBothPartiesAndNotifiesPartialFill()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var services = new ServiceCollection();
        services.AddSingleton(realmContext);
        services.AddSingleton(playerManager);
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
        IItemInfo itemInfo = CreateItemInfo();
        IItemManager itemManager = CreateItemManager(itemInfo);
        IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
        sellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
        sellerInventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
        sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
        playerManager.AddPlayer(seller);

        IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
        buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
        playerManager.AddPlayer(buyer);

        var submitHandler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u),
            itemManager);

        submitHandler.HandleMessage(sellerSession, CreateCommoditySellRequest(quantity: 5u));
        submitHandler.HandleMessage(buyerSession, CreateCommodityBuyRequest(quantity: 5u, pricePerUnit: 10ul));

        RecordingDispatchProxy<ICurrencyManager>.Invocation buyerDebit =
            Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(50ul, buyerDebit.Arguments[1]);

        RecordingDispatchProxy<IInventory>.Invocation buyerCreate =
            Assert.Single(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(ItemId, buyerCreate.Arguments[1]);
        Assert.Equal(5u, buyerCreate.Arguments[2]);

        ServerCommodityAuctionFilledPartial buyerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));
        Assert.Equal(AuctionEventType.Fill, buyerFill.Type);
        Assert.Equal(0u, buyerFill.OrderFilled.Quantity);

        ServerCommodityAuctionFilledPartial sellerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));
        Assert.Equal(AuctionEventType.Fill, sellerFill.Type);
        Assert.Equal(0u, sellerFill.OrderFilled.Quantity);

        RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
            Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(47ul, sellerCredit.Arguments[1]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityOrderCancel_RemovesTransientBuyOrderAndRefunds()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        IItemManager itemManager = CreateItemManager(itemInfo);
        IWorldSession session = CreateSession(505u, 5005ul, out _, out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        var submitHandler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u),
            itemManager);
        submitHandler.HandleMessage(session, CreateCommodityBuyRequest(quantity: 3u, pricePerUnit: 25ul));

        ServerCommodityOrderResult postResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(sessionProxy));
        Assert.Equal(GenericError.Ok, postResult.Result);
        Assert.Equal(75ul, postResult.OrderPosted.Price);
        Assert.True(postResult.OrderPosted.IsBuyOrder);
        Assert.NotEqual(0ul, postResult.OrderPosted.CommodityOrderId);

        RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
        Assert.Equal(75ul, debit.Arguments[1]);

        var cancelHandler = new ClientCommodityOrderCancelHandler(
            NullLogger<ClientCommodityOrderCancelHandler>.Instance,
            itemManager);
        cancelHandler.HandleMessage(session, CreateCommodityCancelRequest(postResult.OrderPosted));

        RecordingDispatchProxy<ICurrencyManager>.Invocation refund =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, refund.Arguments[0]);
        Assert.Equal(75ul, refund.Arguments[1]);

        ServerCommodityAuctionRemoved removed = Assert.Single(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));
        Assert.Equal(AuctionEventType.Cancel, removed.Type);
        Assert.Equal(postResult.OrderPosted.CommodityOrderId, removed.OrderRemoved.CommodityOrderId);
        Assert.Equal(ItemId, removed.OrderRemoved.Item2Id);
        Assert.Equal(3u, removed.OrderRemoved.Quantity);
        Assert.Equal(25ul, removed.OrderRemoved.PricePerUnit);
        Assert.Equal(75ul, removed.OrderRemoved.Price);
        Assert.True(removed.OrderRemoved.IsBuyOrder);
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), ItemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = ItemId,
            MaxStackCount = 1u
        });
        return itemInfo;
    }

    private static IItem CreateItem(IItemInfo itemInfo, out RecordingDispatchProxy<IItem> itemProxy)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), ItemId);
        itemProxy.SetProperty(nameof(IItem.Guid), ItemGuid);
        itemProxy.SetProperty(nameof(IItem.StackCount), 1u);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static IItemManager CreateItemManager(IItemInfo itemInfo)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), itemInfo);
        return itemManager;
    }

    private static IWorldSession CreateSession(
        uint guid,
        ulong characterId,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out IPlayer player)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = characterId
        });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        accountProxy.SetProperty(nameof(IAccount.Id), guid);
        return session;
    }

    private static GameTableManager CreateGameTableManager(uint maxCommodityOrderQuantity = 200u, bool includeListingDurationFormula = false)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        var formulaEntries = new List<GameFormulaEntry>
        {
            new()
            {
                Id        = 0x439u,
                Dataint02 = maxCommodityOrderQuantity
            }
        };

        if (includeListingDurationFormula)
        {
            formulaEntries.Add(new GameFormulaEntry
            {
                Id        = MarketplaceListingDuration.ListingDurationGameFormulaId,
                Dataint0  = 300u,
                Dataint01 = 1800u,
                Dataint02 = 7200u,
                Dataint03 = 43200u
            });
        }

        SetAutoProperty(gameTableManager, nameof(GameTableManager.GameFormula), CreateGameTable(formulaEntries.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item2Family), CreateGameTable<Item2FamilyEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item2Category), CreateGameTable<Item2CategoryEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item2Type), CreateGameTable<Item2TypeEntry>());
        return gameTableManager;
    }

    private static ClientAuctionSellOrderSubmit CreateSellRequest()
    {
        var request = (ClientAuctionSellOrderSubmit)RuntimeHelpers.GetUninitializedObject(typeof(ClientAuctionSellOrderSubmit));
        SetAutoProperty(request, nameof(ClientAuctionSellOrderSubmit.ItemGuid), ItemGuid);
        SetAutoProperty(request, nameof(ClientAuctionSellOrderSubmit.MinimumBid), MinimumBid);
        SetAutoProperty(request, nameof(ClientAuctionSellOrderSubmit.BuyoutPrice), BuyoutPrice);
        return request;
    }

    private static ClientAuctionsByFilterRequest CreateSearchRequest()
    {
        var request = new ClientAuctionsByFilterRequest();
        request.Item2Ids.Add(ItemId);
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.AuctionSort), AuctionSort.Buyout);
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.Page), 0u);
        return request;
    }

    private static ClientAuctionBuyOrderSubmit CreateBuyoutRequest(ulong auctionId)
    {
        return CreateBidRequest(auctionId, BuyoutPrice);
    }

    private static ClientAuctionBuyOrderSubmit CreateBidRequest(ulong auctionId, ulong amountOffered)
    {
        var request = (ClientAuctionBuyOrderSubmit)RuntimeHelpers.GetUninitializedObject(typeof(ClientAuctionBuyOrderSubmit));
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.Item2Id), ItemId);
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.AuctionId), auctionId);
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.AmountOffered), amountOffered);
        return request;
    }

    private static ClientCommoditySellOrderSubmit CreateCommoditySellRequest(uint quantity)
    {
        var request = new ClientCommoditySellOrderSubmit();
        request.Order.Item2Id       = ItemId;
        request.Order.Quantity      = quantity;
        request.Order.PricePerUnit  = 10ul;
        request.Order.Price         = 10ul * quantity;
        request.Order.IsBuyOrder    = false;
        request.Order.ForceImmediate = false;
        return request;
    }

    private static ClientCommoditySellOrderSubmit CreateCommodityBuyRequest(uint quantity, ulong pricePerUnit)
    {
        var request = new ClientCommoditySellOrderSubmit();
        request.Order.Item2Id        = ItemId;
        request.Order.Quantity       = quantity;
        request.Order.PricePerUnit   = pricePerUnit;
        request.Order.Price          = pricePerUnit * quantity;
        request.Order.IsBuyOrder     = true;
        request.Order.ForceImmediate = false;
        return request;
    }

    private static ClientCommodityOrderCancel CreateCommodityCancelRequest(CommodityOrder order)
    {
        var request = (ClientCommodityOrderCancel)RuntimeHelpers.GetUninitializedObject(typeof(ClientCommodityOrderCancel));
        SetAutoProperty(request, nameof(ClientCommodityOrderCancel.CommodityOrderId), order.CommodityOrderId);
        SetAutoProperty(request, nameof(ClientCommodityOrderCancel.Item2Id), order.Item2Id);
        SetAutoProperty(request, nameof(ClientCommodityOrderCancel.IsBuyOrder), order.IsBuyOrder);
        return request;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static void ForceAuctionExpiration(GlobalMarketplaceManager manager)
    {
        Type auctionType = typeof(GlobalMarketplaceManager).GetNestedType("MarketplaceAuction", BindingFlags.NonPublic)!;
        FieldInfo auctionsField = typeof(GlobalMarketplaceManager).GetField("auctions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var auctions = (System.Collections.IList)auctionsField.GetValue(manager)!;
        PropertyInfo expiresAtUtc = auctionType.GetProperty("ExpiresAtUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        foreach (object record in auctions)
            expiresAtUtc.SetValue(record, 0ul);
    }

    private static ServiceProviderScope UseMarketplaceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        return new ServiceProviderScope(services.BuildServiceProvider());
    }

    private sealed class ServiceProviderScope : IDisposable
    {
        private readonly IServiceProvider previous;

        public ServiceProviderScope(IServiceProvider provider)
        {
            previous               = LegacyServiceProvider.Provider;
            LegacyServiceProvider.Provider = provider;
        }

        public void Dispose()
        {
            LegacyServiceProvider.Provider = previous;
        }
    }
}
