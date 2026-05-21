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
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Marketplace;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
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
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(realmContext)
            .AddSingleton(new PlayerManager(NullLogger<PlayerManager>.Instance, new CharacterManager()))
            .BuildServiceProvider();

        try
        {
            IItemInfo itemInfo = CreateItemInfo();
            IItem item = CreateItem(itemInfo, out RecordingDispatchProxy<IItem> itemProxy);
            IWorldSession sellerSession = CreateSession(SellerGuid, SellerCharacterId, out RecordingDispatchProxy<IInventory> sellerInventoryProxy, out _, out RecordingDispatchProxy<IWorldSession> sellerSessionProxy);
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
            IWorldSession searchSession = CreateSession(303u, 3003ul, out _, out _, out RecordingDispatchProxy<IWorldSession> searchSessionProxy);

            searchHandler.HandleMessage(searchSession, CreateSearchRequest());

            ServerAuctionSearchResults searchResults = Assert.Single(GetMessages<ServerAuctionSearchResults>(searchSessionProxy));
            AuctionInfo auction = Assert.Single(searchResults.Auctions);
            Assert.Equal(postResult.Auction.AuctionId, auction.AuctionId);
            Assert.Equal(ItemId, auction.Item2Id);
            Assert.Equal(BuyoutPrice, auction.BuyoutPrice);

            IWorldSession buyerSession = CreateSession(BuyerGuid, BuyerCharacterId, out RecordingDispatchProxy<IInventory> buyerInventoryProxy, out RecordingDispatchProxy<ICurrencyManager> buyerCurrencyProxy, out RecordingDispatchProxy<IWorldSession> buyerSessionProxy);
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
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        return session;
    }

    private static GameTableManager CreateGameTableManager()
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

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
        var request = (ClientAuctionBuyOrderSubmit)RuntimeHelpers.GetUninitializedObject(typeof(ClientAuctionBuyOrderSubmit));
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.Item2Id), ItemId);
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.AuctionId), auctionId);
        SetAutoProperty(request, nameof(ClientAuctionBuyOrderSubmit.AmountOffered), BuyoutPrice);
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
}
