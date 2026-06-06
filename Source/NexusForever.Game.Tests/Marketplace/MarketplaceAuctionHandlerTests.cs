using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using NexusForever.Database;
using NexusForever.Database.Character;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
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
using NexusForever.Network.World.Message.Model.Marketplace.Filter;
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
    private const uint SecondSellerGuid = 102u;
    private const uint BuyerGuid = 202u;
    private const ulong SellerCharacterId = 1001ul;
    private const ulong SecondSellerCharacterId = 1002ul;
    private const ulong BuyerCharacterId = 2002ul;
    private const ulong MinimumBid = 100ul;
    private const ulong BuyoutPrice = 250ul;

    [Fact]
    public void PostAuction_UsesInjectedGameTableForDefaultExpiration()
    {
        using LegacyServiceProviderScope scope = new(new ServiceCollection().BuildServiceProvider());
        var manager = new GlobalMarketplaceManager(
            null,
            CreateGameTableManager(defaultAuctionDurationHours: 1u));
        IItemInfo itemInfo = CreateItemInfo();
        IItem item = CreateItem(itemInfo, out _);
        CreateSession(SellerGuid, SellerCharacterId, out _, out _, out _, out IPlayer seller);

        GenericError result = manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo auction);

        Assert.Equal(GenericError.Ok, result);
        Assert.Equal(3600ul, auction.ExpirationTime);
    }

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
    public void SearchAuctions_HugePage_ReturnsEmptyPageWithoutOverflow()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();

        ServerAuctionSearchResults results = manager.SearchAuctions(
            CreateItemManager(CreateItemInfo()),
            CreateSearchRequest(uint.MaxValue));

        Assert.Equal(uint.MaxValue, results.CurrentPage);
        Assert.Empty(results.Auctions);
    }

    [Fact]
    public void PostAuction_SoulboundItem_ReturnsAuctionCannotFillOrderWithoutRemovingItem()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItem item = CreateItem(CreateItemInfo(), out _, soulbound: true);
        IWorldSession session = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out _, out _);

        GenericError result = manager.PostAuction(session.Player, item, MinimumBid, BuyoutPrice, out AuctionInfo auction);

        Assert.Equal(GenericError.AuctionCannotFillOrder, result);
        Assert.Equal(ItemId, auction.Item2Id);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemRemove)));
        Assert.Empty(manager.GetOwnedItemAuctions(session.Player));
    }

    [Fact]
    public void PostAuction_EquippableBag_ReturnsAuctionCannotFillOrderWithoutRemovingItem()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItem item = CreateItem(CreateItemInfo(equippableBag: true), out _);
        IWorldSession session = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out _, out _);

        GenericError result = manager.PostAuction(session.Player, item, MinimumBid, BuyoutPrice, out AuctionInfo auction);

        Assert.Equal(GenericError.AuctionCannotFillOrder, result);
        Assert.Equal(ItemId, auction.Item2Id);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemRemove)));
        Assert.Empty(manager.GetOwnedItemAuctions(session.Player));
    }

    [Fact]
    public void PostAuction_WhenPersistFails_RestoresInventoryAndDoesNotKeepAuction()
    {
        using ServiceProviderScope scope = UseMarketplaceProviderWithFailingCharacterDatabase();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItem item = CreateItem(CreateItemInfo(), out RecordingDispatchProxy<IItem> itemProxy);
        IWorldSession session = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out _, out _);
        inventoryProxy.SetMethodHandler(nameof(IInventory.ItemRemove), _ =>
        {
            itemProxy.SetProperty(nameof(IItem.CharacterId), null);
            itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.None);
            return null;
        });
        inventoryProxy.SetMethodHandler(nameof(IInventory.AddItem), args =>
        {
            itemProxy.SetProperty(nameof(IItem.CharacterId), SellerCharacterId);
            itemProxy.SetProperty(nameof(IItem.Location), args[1]);
            return null;
        });

        GenericError result = manager.PostAuction(session.Player, item, MinimumBid, BuyoutPrice, out AuctionInfo auction);

        Assert.Equal(GenericError.DbFailure, result);
        Assert.Equal(ItemId, auction.Item2Id);
        Assert.Empty(manager.GetOwnedItemAuctions(session.Player));
        Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemRemove)));
        RecordingDispatchProxy<IInventory>.Invocation restore =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
        Assert.Same(item, restore.Arguments[0]);
        Assert.Equal(InventoryLocation.Inventory, restore.Arguments[1]);
        Assert.Equal(ItemUpdateReason.Auction, restore.Arguments[2]);
        Assert.Equal(SellerCharacterId, item.CharacterId);
        Assert.Equal(InventoryLocation.Inventory, item.Location);
    }

    [Fact]
    public void ValidateAuctionSearch_RejectsUnsupportedAuctionFilters()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItemManager itemManager = CreateItemManager(CreateItemInfo());

        IAuctionFilter[] unsupportedFilters =
        [
            new PropertyMinAuctionFilter(),
            new PropertyMaxAuctionFilter(),
            new RuneSlotAuctionFilter(),
            new EquippableByAuctionFilter()
        ];

        foreach (IAuctionFilter filter in unsupportedFilters)
        {
            var request = new ClientAuctionsByFilterRequest();
            request.Filters.Add(filter);

            Assert.Throws<InvalidPacketValueException>(() =>
                manager.ValidateAuctionSearch(CreateGameTableManager(), itemManager, request));
        }
    }

    [Fact]
    public void ValidateAuctionSearch_RejectsPropertySort()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItemManager itemManager = CreateItemManager(CreateItemInfo());

        var request = new ClientAuctionsByFilterRequest();
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.AuctionSort), AuctionSort.Property);
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.SortPropertyId), Property.Strength);

        Assert.Throws<InvalidPacketValueException>(() =>
            manager.ValidateAuctionSearch(CreateGameTableManager(), itemManager, request));
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
    public void AuctionBid_SameTopBidderIncreaseChargesOnlyDelta()
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
            IWorldSession bidderSession = CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out RecordingDispatchProxy<IWorldSession> bidderSessionProxy, out IPlayer bidder);
            playerManager.AddPlayer(bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            ServerAuctionPostResult postResult = Assert.Single(GetMessages<ServerAuctionPostResult>(sellerSessionProxy));
            ulong auctionId = postResult.Auction.AuctionId;

            var buyHandler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, itemManager);
            buyHandler.HandleMessage(bidderSession, CreateBidRequest(auctionId, MinimumBid));
            buyHandler.HandleMessage(bidderSession, CreateBidRequest(auctionId, MinimumBid + 50ul));

            IReadOnlyList<RecordingDispatchProxy<ICurrencyManager>.Invocation> affordChecks =
                bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford));
            Assert.Collection(affordChecks,
                check =>
                {
                    Assert.Equal(CurrencyType.Credits, check.Arguments[0]);
                    Assert.Equal(MinimumBid, check.Arguments[1]);
                },
                check =>
                {
                    Assert.Equal(CurrencyType.Credits, check.Arguments[0]);
                    Assert.Equal(50ul, check.Arguments[1]);
                });

            IReadOnlyList<RecordingDispatchProxy<ICurrencyManager>.Invocation> debits =
                bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount));
            Assert.Collection(debits,
                debit =>
                {
                    Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
                    Assert.Equal(MinimumBid, debit.Arguments[1]);
                },
                debit =>
                {
                    Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
                    Assert.Equal(50ul, debit.Arguments[1]);
                });
            Assert.Empty(bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));

            IReadOnlyList<ServerAuctionBidResult> bidResults = GetMessages<ServerAuctionBidResult>(bidderSessionProxy);
            Assert.Equal(2, bidResults.Count);
            Assert.All(bidResults, result => Assert.Equal(GenericError.Ok, result.Result));
            Assert.Equal(MinimumBid, bidResults[0].Auction.CurrentBid);
            Assert.Equal(MinimumBid + 50ul, bidResults[1].Auction.CurrentBid);
            Assert.Equal(BuyerCharacterId, bidResults[1].Auction.TopBidderCharacterId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionBid_WhenPersistFails_RefundsBidderAndRestoresAuctionState()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out _, out _, out _, out IPlayer seller);
            Assert.Equal(GenericError.Ok, manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo postedAuction));

            IWorldSession bidderSession = CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out _, out IPlayer bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.BuyAuction(bidder, CreateBidRequest(postedAuction.AuctionId, MinimumBid), out AuctionInfo bidAuction);

            Assert.Equal(GenericError.DbFailure, result);
            Assert.Equal(0ul, bidAuction.CurrentBid);
            Assert.Equal(0ul, bidAuction.TopBidderCharacterId);

            RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
                Assert.Single(bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
            Assert.Equal(MinimumBid, debit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation refund =
                Assert.Single(bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, refund.Arguments[0]);
            Assert.Equal(MinimumBid, refund.Arguments[1]);

            AuctionInfo activeAuction = Assert.Single(manager.GetOwnedItemAuctions(seller));
            Assert.Equal(0ul, activeAuction.CurrentBid);
            Assert.Equal(0ul, activeAuction.TopBidderCharacterId);
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
    public void AuctionExpire_WithOnlineWinnerInventoryDeliveryDeletePersistFails_DoesNotCreditDeliverNotifyOrRemoveAuction()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            CreateSession(SellerGuid, SellerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out _, out IPlayer seller);
            playerManager.AddPlayer(seller);
            Assert.Equal(GenericError.Ok, manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo postedAuction));

            CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> bidderInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out RecordingDispatchProxy<IWorldSession> bidderSessionProxy, out IPlayer bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            bidderInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);
            playerManager.AddPlayer(bidder);
            Assert.Equal(GenericError.Ok, manager.BuyAuction(bidder, CreateBidRequest(postedAuction.AuctionId, MinimumBid), out _));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            ForceAuctionExpiration(manager);
            manager.Update(2000d);

            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(bidderInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(GetMessages<ServerAuctionWon>(bidderSessionProxy));

            AuctionInfo activeAuction = Assert.Single(manager.GetOwnedItemAuctions(seller));
            Assert.Equal(postedAuction.AuctionId, activeAuction.AuctionId);
            Assert.Equal(MinimumBid, activeAuction.CurrentBid);
            Assert.Equal(BuyerCharacterId, activeAuction.TopBidderCharacterId);
            Assert.Equal(SellerCharacterId, item.CharacterId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionExpire_WithHighBidOfflineWinnerAndNoMailDelivery_DoesNotCreditSellerOrDeleteAuction()
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

            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(bidderInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(GetMessages<ServerAuctionWon>(bidderSessionProxy));
            AuctionInfo activeAuction = Assert.Single(GlobalMarketplaceManager.Instance.GetOwnedItemAuctions(seller));
            Assert.Equal(auctionId, activeAuction.AuctionId);
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
    public void AuctionBuyout_WhenMailPersistFails_DoesNotChargeBuyerOrRemoveAuction()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out _, out IPlayer seller);
            Assert.Equal(GenericError.Ok, manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo postedAuction));
            playerManager.AddPlayer(seller);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out _, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);
            playerManager.AddPlayer(buyer);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.BuyAuction(buyer, CreateBuyoutRequest(postedAuction.AuctionId), out AuctionInfo buyoutAuction);

            Assert.Equal(GenericError.ItemInventoryFull, result);
            Assert.Equal(postedAuction.AuctionId, buyoutAuction.AuctionId);
            Assert.Empty(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));

            AuctionInfo activeAuction = Assert.Single(manager.GetOwnedItemAuctions(seller));
            Assert.Equal(postedAuction.AuctionId, activeAuction.AuctionId);
            Assert.Equal(SellerCharacterId, item.CharacterId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionBuyout_WhenInventoryDeliveryDeletePersistFails_DoesNotChargeCreditDeliverOrRemoveAuction()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out _);
            CreateSession(SellerGuid, SellerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out _, out IPlayer seller);
            playerManager.AddPlayer(seller);
            Assert.Equal(GenericError.Ok, manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo postedAuction));

            CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);
            playerManager.AddPlayer(buyer);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.BuyAuction(buyer, CreateBuyoutRequest(postedAuction.AuctionId), out AuctionInfo buyoutAuction);

            Assert.Equal(GenericError.DbFailure, result);
            Assert.Equal(0ul, buyoutAuction.CurrentBid);
            Assert.Equal(0ul, buyoutAuction.TopBidderCharacterId);
            Assert.Empty(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(buyerInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(GetMessages<ServerAuctionWon>(buyerSessionProxy));

            AuctionInfo activeAuction = Assert.Single(manager.GetOwnedItemAuctions(seller));
            Assert.Equal(postedAuction.AuctionId, activeAuction.AuctionId);
            Assert.Equal(0ul, activeAuction.CurrentBid);
            Assert.Equal(0ul, activeAuction.TopBidderCharacterId);
            Assert.Equal(SellerCharacterId, item.CharacterId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AuctionCancel_WhenInventoryReturnDeletePersistFails_DoesNotRefundReturnOrRemoveAuction()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out RecordingDispatchProxy<IItem> itemProxy);
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out _, out _, out IPlayer seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);
            sellerInventoryProxy.SetMethodHandler(nameof(IInventory.ItemRemove), _ =>
            {
                itemProxy.SetProperty(nameof(IItem.CharacterId), null);
                itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.None);
                return null;
            });
            Assert.Equal(GenericError.Ok, manager.PostAuction(seller, item, MinimumBid, BuyoutPrice, out AuctionInfo postedAuction));

            CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> bidderCurrencyProxy, out _, out IPlayer bidder);
            bidderCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            playerManager.AddPlayer(bidder);
            Assert.Equal(GenericError.Ok, manager.BuyAuction(bidder, CreateBidRequest(postedAuction.AuctionId, MinimumBid), out _));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.CancelAuction(seller, postedAuction.AuctionId, ItemId, out AuctionInfo cancelAuction);

            Assert.Equal(GenericError.DbFailure, result);
            Assert.Equal(postedAuction.AuctionId, cancelAuction.AuctionId);
            Assert.Empty(sellerInventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
            Assert.Empty(bidderCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));

            AuctionInfo activeAuction = Assert.Single(manager.GetOwnedItemAuctions(seller));
            Assert.Equal(postedAuction.AuctionId, activeAuction.AuctionId);
            Assert.Equal(MinimumBid, activeAuction.CurrentBid);
            Assert.Equal(BuyerCharacterId, activeAuction.TopBidderCharacterId);
            Assert.Null(item.CharacterId);
            Assert.Equal(InventoryLocation.None, item.Location);
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
    public void CommodityBuyOrderExpire_WhenDeletePersistFails_DoesNotRefundNotifyOrRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(CreateItemInfo());
            CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy, out IPlayer buyer);
            currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            playerManager.AddPlayer(buyer);

            CommodityOrder order = CreateCommodityBuyRequest(quantity: 3u, pricePerUnit: 25ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(buyer, order, out CommodityOrder postedOrder, itemManager));
            ForceCommodityOrderExpiration(manager);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            manager.Update(1d);

            Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(buyer));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(75ul, activeOrder.Price);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommoditySellOrderExpire_WhenInventoryReturnDeletePersistFails_DoesNotReturnNotifyOrRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(CreateItemInfo());
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out IPlayer seller);
            inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
            playerManager.AddPlayer(seller);

            CommodityOrder order = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, order, out CommodityOrder postedOrder, itemManager));
            ForceCommodityOrderExpiration(manager);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            manager.Update(1d);

            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
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
    public void CommodityBuyOrderSubmit_WhenPersistFails_RefundsEscrowAndDoesNotKeepOrder()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProviderWithFailingCharacterDatabase();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)marketplaceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IWorldSession session = CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out _, out IPlayer player);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        CommodityOrder order = CreateCommodityBuyRequest(quantity: 2u, pricePerUnit: 25ul).Order;
        GenericError result = manager.PostCommodityOrder(player, order, out CommodityOrder postedOrder, CreateItemManager(CreateItemInfo()));

        Assert.Equal(GenericError.DbFailure, result);
        Assert.Equal(50ul, postedOrder.Price);
        Assert.Empty(manager.GetOwnedCommodityOrders(player));

        RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
        Assert.Equal(50ul, debit.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation refund =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, refund.Arguments[0]);
        Assert.Equal(50ul, refund.Arguments[1]);
    }

    [Fact]
    public void CommoditySellOrderSubmit_WhenPersistFails_RestoresItemsAndDoesNotKeepOrder()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProviderWithFailingCharacterDatabase();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)marketplaceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IWorldSession session = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out _, out IPlayer player);
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);

        CommodityOrder order = CreateCommoditySellRequest(quantity: 3u, pricePerUnit: 10ul).Order;
        GenericError result = manager.PostCommodityOrder(player, order, out CommodityOrder postedOrder, CreateItemManager(CreateItemInfo()));

        Assert.Equal(GenericError.DbFailure, result);
        Assert.Equal(30ul, postedOrder.Price);
        Assert.Empty(manager.GetOwnedCommodityOrders(player));

        RecordingDispatchProxy<IInventory>.Invocation delete =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Equal(ItemId, delete.Arguments[0]);
        Assert.Equal(3u, delete.Arguments[1]);
        Assert.Equal(ItemUpdateReason.Auction, delete.Arguments[2]);

        RecordingDispatchProxy<IInventory>.Invocation restore =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, restore.Arguments[0]);
        Assert.Equal(ItemId, restore.Arguments[1]);
        Assert.Equal(3u, restore.Arguments[2]);
        Assert.Equal(ItemUpdateReason.Auction, restore.Arguments[3]);
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
        buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 5u);
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
    public void CommodityOrderForceImmediateBuy_FillsMultipleSellOrdersByBestPriceAndRefundsImprovement()
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
            IWorldSession firstSellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> firstSellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> firstSellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> firstSellerSessionProxy, out IPlayer firstSeller);
            firstSellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            firstSellerInventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
            playerManager.AddPlayer(firstSeller);

            IWorldSession secondSellerSession = CreateSession(SecondSellerGuid, SecondSellerCharacterId, out RecordingDispatchProxy<IInventory> secondSellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> secondSellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> secondSellerSessionProxy, out IPlayer secondSeller);
            secondSellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            secondSellerInventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
            playerManager.AddPlayer(secondSeller);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 5u);
            playerManager.AddPlayer(buyer);

            var submitHandler = new ClientCommoditySellOrderSubmitHandler(
                NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
                CreateGameTableManager(maxCommodityOrderQuantity: 200u),
                itemManager);

            submitHandler.HandleMessage(firstSellerSession, CreateCommoditySellRequest(quantity: 3u, pricePerUnit: 25ul));
            submitHandler.HandleMessage(secondSellerSession, CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 20ul));

            ClientCommoditySellOrderSubmit buyRequest = CreateCommodityBuyRequest(quantity: 5u, pricePerUnit: 25ul);
            buyRequest.Order.ForceImmediate = true;
            submitHandler.HandleMessage(buyerSession, buyRequest);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerDebit =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, buyerDebit.Arguments[0]);
            Assert.Equal(125ul, buyerDebit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerRefund =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, buyerRefund.Arguments[0]);
            Assert.Equal(10ul, buyerRefund.Arguments[1]);

            Assert.Collection(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)),
                firstFill =>
                {
                    Assert.Equal(InventoryLocation.Inventory, firstFill.Arguments[0]);
                    Assert.Equal(ItemId, firstFill.Arguments[1]);
                    Assert.Equal(2u, firstFill.Arguments[2]);
                    Assert.Equal(ItemUpdateReason.Auction, firstFill.Arguments[3]);
                },
                secondFill =>
                {
                    Assert.Equal(InventoryLocation.Inventory, secondFill.Arguments[0]);
                    Assert.Equal(ItemId, secondFill.Arguments[1]);
                    Assert.Equal(3u, secondFill.Arguments[2]);
                    Assert.Equal(ItemUpdateReason.Auction, secondFill.Arguments[3]);
                });

            RecordingDispatchProxy<ICurrencyManager>.Invocation firstSellerCredit =
                Assert.Single(firstSellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, firstSellerCredit.Arguments[0]);
            Assert.Equal(71ul, firstSellerCredit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation secondSellerCredit =
                Assert.Single(secondSellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, secondSellerCredit.Arguments[0]);
            Assert.Equal(38ul, secondSellerCredit.Arguments[1]);

            Assert.Collection(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy),
                firstFill =>
                {
                    Assert.Equal(AuctionEventType.Fill, firstFill.Type);
                    Assert.Equal(3u, firstFill.OrderFilled.Quantity);
                    Assert.Equal(75ul, firstFill.OrderFilled.Price);
                },
                secondFill =>
                {
                    Assert.Equal(AuctionEventType.Fill, secondFill.Type);
                    Assert.Equal(0u, secondFill.OrderFilled.Quantity);
                    Assert.Equal(0ul, secondFill.OrderFilled.Price);
                });

            ServerCommodityAuctionFilledPartial firstSellerFill =
                Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(firstSellerSessionProxy));
            Assert.Equal(0u, firstSellerFill.OrderFilled.Quantity);
            Assert.Equal(0ul, firstSellerFill.OrderFilled.Price);

            ServerCommodityAuctionFilledPartial secondSellerFill =
                Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(secondSellerSessionProxy));
            Assert.Equal(0u, secondSellerFill.OrderFilled.Quantity);
            Assert.Equal(0ul, secondSellerFill.OrderFilled.Price);

            ServerCommodityOrderResult buyResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(buyerSessionProxy));
            Assert.Equal(GenericError.Ok, buyResult.Result);
            Assert.True(buyResult.OrderPosted.ForceImmediate);
            Assert.Equal(0u, buyResult.OrderPosted.Quantity);
            Assert.Equal(0ul, buyResult.OrderPosted.Price);

            GlobalMarketplaceManager manager = (GlobalMarketplaceManager)LegacyServiceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
            Assert.Empty(manager.GetOwnedCommodityOrders(firstSeller));
            Assert.Empty(manager.GetOwnedCommodityOrders(secondSeller));
            Assert.Empty(manager.GetOwnedCommodityOrders(buyer));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityOrders_CrossMatch_InsufficientBuyerSlotsWithoutMailLeavesOrdersUnfilled()
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
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 5u);
            playerManager.AddPlayer(seller);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);
            playerManager.AddPlayer(buyer);

            var submitHandler = new ClientCommoditySellOrderSubmitHandler(
                NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
                CreateGameTableManager(maxCommodityOrderQuantity: 200u),
                itemManager);

            submitHandler.HandleMessage(sellerSession, CreateCommoditySellRequest(quantity: 5u));
            submitHandler.HandleMessage(buyerSession, CreateCommodityBuyRequest(quantity: 5u, pricePerUnit: 10ul));

            Assert.Empty(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));

            GlobalMarketplaceManager manager = (GlobalMarketplaceManager)LegacyServiceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
            Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Single(manager.GetOwnedCommodityOrders(buyer));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityOrderForceImmediateBuy_NoMatchSkipsSlotLimitAndRefundsWithoutResting()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        IItemManager itemManager = CreateItemManager(itemInfo);
        IWorldSession session = CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy, out IPlayer player);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        var submitHandler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u),
            itemManager);

        submitHandler.HandleMessage(session, CreateCommodityBuyRequest(quantity: 1u, pricePerUnit: 25ul));
        submitHandler.HandleMessage(session, CreateCommodityBuyRequest(quantity: 1u, pricePerUnit: 25ul));
        submitHandler.HandleMessage(session, CreateCommodityBuyRequest(quantity: 1u, pricePerUnit: 25ul));

        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)marketplaceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        Assert.Equal(3, manager.GetOwnedCommodityOrders(player).Count);

        ClientCommoditySellOrderSubmit immediateOrder = CreateCommodityBuyRequest(quantity: 2u, pricePerUnit: 25ul);
        immediateOrder.Order.ForceImmediate = true;
        submitHandler.HandleMessage(session, immediateOrder);

        ServerCommodityOrderResult immediateResult = GetMessages<ServerCommodityOrderResult>(sessionProxy).Last();
        Assert.Equal(GenericError.Ok, immediateResult.Result);
        Assert.True(immediateResult.OrderPosted.ForceImmediate);
        Assert.Equal(0u, immediateResult.OrderPosted.Quantity);
        Assert.Equal(0ul, immediateResult.OrderPosted.Price);
        Assert.Equal(3, manager.GetOwnedCommodityOrders(player).Count);

        IReadOnlyList<RecordingDispatchProxy<ICurrencyManager>.Invocation> debits =
            currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount));
        Assert.Equal(4, debits.Count);
        Assert.Equal(50ul, debits.Last().Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation refund =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, refund.Arguments[0]);
        Assert.Equal(50ul, refund.Arguments[1]);
        Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(sessionProxy));
    }

    [Fact]
    public void CommodityOrderForceImmediateBuy_PartialFillRefundsPriceImprovementAndUnmatchedEscrow()
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
            playerManager.AddPlayer(seller);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 5u);
            playerManager.AddPlayer(buyer);

            var submitHandler = new ClientCommoditySellOrderSubmitHandler(
                NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
                CreateGameTableManager(maxCommodityOrderQuantity: 200u),
                itemManager);

            submitHandler.HandleMessage(sellerSession, CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 20ul));
            ClientCommoditySellOrderSubmit buyRequest = CreateCommodityBuyRequest(quantity: 5u, pricePerUnit: 25ul);
            buyRequest.Order.ForceImmediate = true;
            submitHandler.HandleMessage(buyerSession, buyRequest);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerDebit =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, buyerDebit.Arguments[0]);
            Assert.Equal(125ul, buyerDebit.Arguments[1]);

            IReadOnlyList<RecordingDispatchProxy<ICurrencyManager>.Invocation> buyerRefunds =
                buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount));
            Assert.Collection(buyerRefunds,
                priceImprovement =>
                {
                    Assert.Equal(CurrencyType.Credits, priceImprovement.Arguments[0]);
                    Assert.Equal(10ul, priceImprovement.Arguments[1]);
                },
                unmatchedEscrow =>
                {
                    Assert.Equal(CurrencyType.Credits, unmatchedEscrow.Arguments[0]);
                    Assert.Equal(75ul, unmatchedEscrow.Arguments[1]);
                });

            RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
                Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, sellerCredit.Arguments[0]);
            Assert.Equal(38ul, sellerCredit.Arguments[1]);

            RecordingDispatchProxy<IInventory>.Invocation buyerCreate =
                Assert.Single(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(ItemId, buyerCreate.Arguments[1]);
            Assert.Equal(2u, buyerCreate.Arguments[2]);

            ServerCommodityAuctionFilledPartial buyerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));
            Assert.Equal(3u, buyerFill.OrderFilled.Quantity);
            Assert.Equal(75ul, buyerFill.OrderFilled.Price);

            ServerCommodityAuctionFilledPartial sellerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));
            Assert.Equal(0u, sellerFill.OrderFilled.Quantity);
            Assert.Equal(0ul, sellerFill.OrderFilled.Price);

            ServerCommodityOrderResult buyResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(buyerSessionProxy));
            Assert.Equal(GenericError.Ok, buyResult.Result);
            Assert.True(buyResult.OrderPosted.ForceImmediate);
            Assert.Equal(0u, buyResult.OrderPosted.Quantity);
            Assert.Equal(0ul, buyResult.OrderPosted.Price);

            GlobalMarketplaceManager manager = (GlobalMarketplaceManager)LegacyServiceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
            Assert.Empty(manager.GetOwnedCommodityOrders(seller));
            Assert.Empty(manager.GetOwnedCommodityOrders(buyer));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityOrderForceImmediateSell_FullMatchDoesNotRequireReturnSlots()
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
            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
            playerManager.AddPlayer(buyer);

            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);
            playerManager.AddPlayer(seller);

            var submitHandler = new ClientCommoditySellOrderSubmitHandler(
                NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
                CreateGameTableManager(maxCommodityOrderQuantity: 200u),
                itemManager);

            submitHandler.HandleMessage(buyerSession, CreateCommodityBuyRequest(quantity: 2u, pricePerUnit: 25ul));
            ClientCommoditySellOrderSubmit sellRequest = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 20ul);
            sellRequest.Order.ForceImmediate = true;
            submitHandler.HandleMessage(sellerSession, sellRequest);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerDebit =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(50ul, buyerDebit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerRefund =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, buyerRefund.Arguments[0]);
            Assert.Equal(10ul, buyerRefund.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation sellerCredit =
                Assert.Single(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, sellerCredit.Arguments[0]);
            Assert.Equal(38ul, sellerCredit.Arguments[1]);

            RecordingDispatchProxy<IInventory>.Invocation buyerCreate =
                Assert.Single(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(ItemId, buyerCreate.Arguments[1]);
            Assert.Equal(2u, buyerCreate.Arguments[2]);

            Assert.Single(sellerInventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
            Assert.Empty(sellerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));

            ServerCommodityOrderResult sellResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(sellerSessionProxy));
            Assert.Equal(GenericError.Ok, sellResult.Result);
            Assert.True(sellResult.OrderPosted.ForceImmediate);
            Assert.Equal(0u, sellResult.OrderPosted.Quantity);
            Assert.Equal(0ul, sellResult.OrderPosted.Price);

            ServerCommodityAuctionFilledPartial buyerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));
            Assert.Equal(0u, buyerFill.OrderFilled.Quantity);
            Assert.Equal(0ul, buyerFill.OrderFilled.Price);

            ServerCommodityAuctionFilledPartial sellerFill = Assert.Single(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));
            Assert.Equal(0u, sellerFill.OrderFilled.Quantity);
            Assert.Equal(0ul, sellerFill.OrderFilled.Price);

            GlobalMarketplaceManager manager = (GlobalMarketplaceManager)LegacyServiceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
            Assert.Empty(manager.GetOwnedCommodityOrders(seller));
            Assert.Empty(manager.GetOwnedCommodityOrders(buyer));
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

    [Fact]
    public void CommodityBuyOrderCancel_WhenDeletePersistFails_DoesNotRefundOrRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(CreateItemInfo());
            CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out _, out IPlayer buyer);
            currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            CommodityOrder order = CreateCommodityBuyRequest(quantity: 3u, pricePerUnit: 25ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(buyer, order, out CommodityOrder postedOrder, itemManager));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.CancelCommodityOrder(
                buyer,
                postedOrder.CommodityOrderId,
                postedOrder.Item2Id,
                postedOrder.IsBuyOrder,
                out CommodityOrder cancelledOrder,
                itemManager);

            Assert.Equal(GenericError.DbFailure, result);
            Assert.Equal(postedOrder.CommodityOrderId, cancelledOrder.CommodityOrderId);
            Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(buyer));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(75ul, activeOrder.Price);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommoditySellOrderCancel_WhenInventoryReturnDeletePersistFails_DoesNotReturnItemsOrRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(CreateItemInfo());
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out _, out IPlayer seller);
            inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);

            CommodityOrder order = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, order, out CommodityOrder postedOrder, itemManager));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.CancelCommodityOrder(
                seller,
                postedOrder.CommodityOrderId,
                postedOrder.Item2Id,
                postedOrder.IsBuyOrder,
                out CommodityOrder cancelledOrder,
                itemManager);

            Assert.Equal(GenericError.DbFailure, result);
            Assert.Equal(postedOrder.CommodityOrderId, cancelledOrder.CommodityOrderId);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommoditySellOrderCancel_WhenMailReturnDeletePersistFails_DoesNotRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            ItemManager itemManagerSingleton = CreatePrimedItemManager(itemInfo);
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(itemInfo);
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out IPlayer seller);
            inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);
            playerManager.AddPlayer(seller);

            CommodityOrder order = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, order, out CommodityOrder postedOrder, itemManager));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            GenericError result = manager.CancelCommodityOrder(
                seller,
                postedOrder.CommodityOrderId,
                postedOrder.Item2Id,
                postedOrder.IsBuyOrder,
                out CommodityOrder cancelledOrder,
                itemManager);

            Assert.Equal(GenericError.ItemInventoryFull, result);
            Assert.Equal(postedOrder.CommodityOrderId, cancelledOrder.CommodityOrderId);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommoditySellOrderExpire_WhenMailReturnDeletePersistFails_DoesNotNotifyOrRemoveOrder()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            ItemManager itemManagerSingleton = CreatePrimedItemManager(itemInfo);
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(itemInfo);
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out IPlayer seller);
            inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);
            playerManager.AddPlayer(seller);

            CommodityOrder order = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, order, out CommodityOrder postedOrder, itemManager));
            ForceCommodityOrderExpiration(manager);

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            manager.Update(1d);

            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityForceImmediateBuy_WhenDirectFillOrderPersistFails_LeavesSellOrderUnfilled()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(CreateItemInfo());
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> sellerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
            playerManager.AddPlayer(seller);

            CommodityOrder sellOrder = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, sellOrder, out CommodityOrder postedSellOrder, itemManager));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 2u);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            playerManager.AddPlayer(buyer);

            CommodityOrder buyOrder = CreateCommodityBuyRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            buyOrder.ForceImmediate = true;

            GenericError result = manager.PostCommodityOrder(buyer, buyOrder, out CommodityOrder postedBuyOrder, itemManager);

            Assert.Equal(GenericError.Ok, result);
            Assert.Equal(0u, postedBuyOrder.Quantity);
            Assert.Equal(0ul, postedBuyOrder.Price);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerDebit =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, buyerDebit.Arguments[0]);
            Assert.Equal(20ul, buyerDebit.Arguments[1]);

            RecordingDispatchProxy<ICurrencyManager>.Invocation buyerRefund =
                Assert.Single(buyerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, buyerRefund.Arguments[0]);
            Assert.Equal(20ul, buyerRefund.Arguments[1]);

            Assert.Empty(buyerInventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Empty(sellerCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedSellOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
            Assert.Equal(20ul, activeOrder.Price);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityForceImmediateBuy_WhenFillMailOrderPersistFails_LeavesSellOrderUnfilled()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), (ushort)1);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager());
        var manager = new GlobalMarketplaceManager();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            ItemManager itemManagerSingleton = CreatePrimedItemManager(itemInfo);
            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .BuildServiceProvider();

            IItemManager itemManager = CreateItemManager(itemInfo);
            CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy, out IPlayer seller);
            sellerInventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
            playerManager.AddPlayer(seller);

            CommodityOrder sellOrder = CreateCommoditySellRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(seller, sellOrder, out CommodityOrder postedSellOrder, itemManager));

            LegacyServiceProvider.Provider = new ServiceCollection()
                .AddSingleton(realmContext)
                .AddSingleton(playerManager)
                .AddSingleton(itemManagerSingleton)
                .AddSingleton(CreateFailingDatabaseManager())
                .BuildServiceProvider();

            CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy, out IPlayer buyer);
            buyerInventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 0u);
            buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
            playerManager.AddPlayer(buyer);

            CommodityOrder buyOrder = CreateCommodityBuyRequest(quantity: 2u, pricePerUnit: 10ul).Order;
            buyOrder.ForceImmediate = true;

            GenericError result = manager.PostCommodityOrder(buyer, buyOrder, out CommodityOrder postedBuyOrder, itemManager);

            Assert.Equal(GenericError.Ok, result);
            Assert.Equal(0u, postedBuyOrder.Quantity);
            Assert.Equal(0ul, postedBuyOrder.Price);
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(sellerSessionProxy));
            Assert.Empty(GetMessages<ServerCommodityAuctionFilledPartial>(buyerSessionProxy));

            CommodityOrder activeOrder = Assert.Single(manager.GetOwnedCommodityOrders(seller));
            Assert.Equal(postedSellOrder.CommodityOrderId, activeOrder.CommodityOrderId);
            Assert.Equal(2u, activeOrder.Quantity);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CommodityOrderCancel_SellOrderInsufficientSlotsWithoutMailReturnsInventoryFullWithoutRemoving()
    {
        using ServiceProviderScope marketplaceProvider = UseMarketplaceProvider();

        IItemInfo itemInfo = CreateItemInfo();
        IItemManager itemManager = CreateItemManager(itemInfo);
        IWorldSession session = CreateSession(707u, 7007ul, out RecordingDispatchProxy<IInventory> inventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);

        var submitHandler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(maxCommodityOrderQuantity: 200u),
            itemManager);
        submitHandler.HandleMessage(session, CreateCommoditySellRequest(quantity: 5u));

        ServerCommodityOrderResult postResult = Assert.Single(GetMessages<ServerCommodityOrderResult>(sessionProxy));
        Assert.Equal(GenericError.Ok, postResult.Result);

        var cancelHandler = new ClientCommodityOrderCancelHandler(
            NullLogger<ClientCommodityOrderCancelHandler>.Instance,
            itemManager);
        cancelHandler.HandleMessage(session, CreateCommodityCancelRequest(postResult.OrderPosted));

        ServerCommodityOrderResult cancelResult = GetMessages<ServerCommodityOrderResult>(sessionProxy).Last();
        Assert.Equal(GenericError.ItemInventoryFull, cancelResult.Result);
        Assert.Equal(postResult.OrderPosted.CommodityOrderId, cancelResult.OrderPosted.CommodityOrderId);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(GetMessages<ServerCommodityAuctionRemoved>(sessionProxy));

        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)marketplaceProvider.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        Assert.Single(manager.GetOwnedCommodityOrders(session.Player));
    }

    private static IItemInfo CreateItemInfo(bool equippableBag = false)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), ItemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = ItemId,
            MaxStackCount = 1u
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippableBag), equippableBag);
        return itemInfo;
    }

    private static IItem CreateItem(
        IItemInfo itemInfo,
        out RecordingDispatchProxy<IItem> itemProxy,
        ulong ownerCharacterId = SellerCharacterId,
        InventoryLocation location = InventoryLocation.Inventory,
        bool soulbound = false)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), ItemId);
        itemProxy.SetProperty(nameof(IItem.Guid), ItemGuid);
        itemProxy.SetProperty(nameof(IItem.CharacterId), ownerCharacterId);
        itemProxy.SetProperty(nameof(IItem.Location), location);
        itemProxy.SetProperty(nameof(IItem.StackCount), 1u);
        itemProxy.SetProperty(nameof(IItem.Soulbound), soulbound);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static IItemManager CreateItemManager(IItemInfo itemInfo)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), itemInfo);
        return itemManager;
    }

    private static ItemManager CreatePrimedItemManager(IItemInfo itemInfo)
    {
        var itemManager = new ItemManager();
        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(itemInfo.Id, itemInfo));
        SetPrivateField(itemManager, "nextItemId", 1ul);
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

    private static GameTableManager CreateGameTableManager(
        uint maxCommodityOrderQuantity = 200u,
        bool includeListingDurationFormula = false,
        uint? defaultAuctionDurationHours = null)
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

        if (defaultAuctionDurationHours.HasValue)
        {
            formulaEntries.Add(new GameFormulaEntry
            {
                Id       = 821u,
                Dataint0 = defaultAuctionDurationHours.Value
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

    private static ClientAuctionsByFilterRequest CreateSearchRequest(uint page = 0u)
    {
        var request = new ClientAuctionsByFilterRequest();
        request.Item2Ids.Add(ItemId);
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.AuctionSort), AuctionSort.Buyout);
        SetAutoProperty(request, nameof(ClientAuctionsByFilterRequest.Page), page);
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

    private static ClientCommoditySellOrderSubmit CreateCommoditySellRequest(uint quantity, ulong pricePerUnit = 10ul)
    {
        var request = new ClientCommoditySellOrderSubmit();
        request.Order.Item2Id       = ItemId;
        request.Order.Quantity      = quantity;
        request.Order.PricePerUnit  = pricePerUnit;
        request.Order.Price         = pricePerUnit * quantity;
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

    private static void ForceCommodityOrderExpiration(GlobalMarketplaceManager manager)
    {
        Type commodityOrderType = typeof(GlobalMarketplaceManager).GetNestedType("MarketplaceCommodityOrder", BindingFlags.NonPublic)!;
        FieldInfo commodityOrdersField = typeof(GlobalMarketplaceManager).GetField("commodityOrders", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var commodityOrders = (System.Collections.IList)commodityOrdersField.GetValue(manager)!;
        PropertyInfo orderProperty = commodityOrderType.GetProperty("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        foreach (object record in commodityOrders)
        {
            var order = (CommodityOrder)orderProperty.GetValue(record)!;
            order.ExpirationTime = 0ul;
        }
    }

    private static ServiceProviderScope UseMarketplaceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        return new ServiceProviderScope(services.BuildServiceProvider());
    }

    private static ServiceProviderScope UseMarketplaceProviderWithFailingCharacterDatabase()
    {
        var services = new ServiceCollection();
        services.AddSingleton(CreateFailingDatabaseManager());
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        return new ServiceProviderScope(services.BuildServiceProvider());
    }

    private static DatabaseManager CreateFailingDatabaseManager()
    {
        var manager = (DatabaseManager)RuntimeHelpers.GetUninitializedObject(typeof(DatabaseManager));
        SetPrivateField(
            manager,
            "databases",
            ImmutableDictionary<Type, IDatabase>.Empty.Add(typeof(CharacterDatabase), new CharacterDatabase()));
        return manager;
    }

    [Fact]
    public void ValidateCommodityOrder_RejectsPriceMismatch()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();

        var order = new CommodityOrder
        {
            Item2Id      = ItemId,
            Quantity     = 10u,
            PricePerUnit = 100ul,
            Price        = 1ul,
            IsBuyOrder   = true
        };

        IItemInfo itemInfo = CreateItemInfo();
        Assert.Throws<InvalidPacketValueException>(() =>
            manager.ValidateCommodityOrder(CreateGameTableManager(), CreateItemManager(itemInfo), order));
    }

    [Fact]
    public void DeserializeMicrochipIds_OverflowDbValue_ReturnsEmpty()
    {
        MethodInfo method = typeof(GlobalMarketplaceManager)
            .GetMethod("DeserializeMicrochipIds", BindingFlags.Static | BindingFlags.NonPublic)!;

        var result = Assert.IsType<List<uint>>(method.Invoke(null, new object[] { "1,4294967296" }));

        Assert.Empty(result);
    }

    [Fact]
    public void SaveSettledAuctionItem_SavesWithoutEnqueueingDelete()
    {
        MethodInfo method = typeof(GlobalMarketplaceManager)
            .GetMethod("SaveSettledAuctionItem", BindingFlags.Static | BindingFlags.NonPublic)!;
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);

        method.Invoke(null, new object[] { null, item });

        Assert.Single(itemProxy.GetInvocations(nameof(IDatabaseCharacter.Save)));
        Assert.Empty(itemProxy.GetInvocations(nameof(IDatabaseState.EnqueueDelete)));
    }

    [Fact]
    public void PersistedCommodityOrderValidation_RejectsUnsafeRows()
    {
        MethodInfo method = typeof(GlobalMarketplaceManager)
            .GetMethod("IsValidPersistedCommodityOrder", BindingFlags.Static | BindingFlags.NonPublic)!;

        GameTableManager gameTableManager = CreateGameTableManager();
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id            = ItemId,
            MaxStackCount = 1u
        }));

        bool IsValid(MarketplaceCommodityOrderModel model) =>
            (bool)method.Invoke(null, new object[] { model, gameTableManager });

        Assert.True(IsValid(CreatePersistedCommodityOrderModel()));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(price: 1ul)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(pricePerUnit: ulong.MaxValue, quantity: 2u, price: ulong.MaxValue)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(forceImmediate: true)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(item2Id: ItemId + 1u)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(listTime: 200ul, expirationTime: 100ul)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(ownerCharacterId: 0ul)));
        Assert.False(IsValid(CreatePersistedCommodityOrderModel(quantity: 0u, price: 0ul)));
    }

    [Fact]
    public void PostCommodityBuyOrder_RejectsEscrowPriceMismatch()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        IItemInfo itemInfo = CreateItemInfo();
        IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out _, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out _, out _);
        buyerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        var sellHandler = new ClientCommoditySellOrderSubmitHandler(
            NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance,
            CreateGameTableManager(),
            CreateItemManager(itemInfo));
        ClientCommoditySellOrderSubmit request = CreateCommodityBuyRequest(10u, 100ul);
        request.Order.Price = 1ul;

        Assert.Throws<InvalidPacketValueException>(() => sellHandler.HandleMessage(buyerSession, request));
        Assert.Empty(manager.GetOwnedCommodityOrders(buyerSession.Player));
    }

    private static MarketplaceCommodityOrderModel CreatePersistedCommodityOrderModel(
        ulong id = 1ul,
        ulong ownerCharacterId = SellerCharacterId,
        uint item2Id = ItemId,
        uint quantity = 2u,
        ulong pricePerUnit = 25ul,
        ulong? price = null,
        bool isBuyOrder = true,
        bool forceImmediate = false,
        ulong listTime = 0ul,
        ulong expirationTime = 0ul)
    {
        return new MarketplaceCommodityOrderModel
        {
            Id               = id,
            OwnerCharacterId = ownerCharacterId,
            Item2Id          = item2Id,
            Quantity         = quantity,
            PricePerUnit     = pricePerUnit,
            Price            = price ?? pricePerUnit * quantity,
            IsBuyOrder       = isBuyOrder,
            ForceImmediate   = forceImmediate,
            ListTime         = listTime,
            ExpirationTime   = expirationTime
        };
    }

    private sealed class ServiceProviderScope : IDisposable
    {
        private readonly IServiceProvider previous;

        public ServiceProviderScope(IServiceProvider provider)
        {
            Provider               = provider;
            previous               = LegacyServiceProvider.Provider;
            LegacyServiceProvider.Provider = provider;
        }

        public IServiceProvider Provider { get; }

        public void Dispose()
        {
            LegacyServiceProvider.Provider = previous;
        }
    }
}
