using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
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
public class MarketplaceOwnedListHandlerTests
{
    private const uint ItemId = 7001u;
    private const ulong ItemGuid = 0xABCDEFul;
    private const uint PlayerGuid = 606u;
    private const ulong CharacterId = 6006ul;

    [Fact]
    public void OwnedMarketplaceRequests_WithNoRows_SendEmptyOwnedListPackets()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        ClearMarketplaceState((GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>());

        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _, out _);
        var commodityHandler = new ClientRequestOwnedCommodityOrdersHandler(NullLogger<ClientRequestOwnedCommodityOrdersHandler>.Instance);
        var auctionHandler = new ClientRequestOwnedItemAuctionsHandler(NullLogger<ClientRequestOwnedItemAuctionsHandler>.Instance);

        commodityHandler.HandleMessage(session, new ClientRequestOwnedCommodityOrders());
        auctionHandler.HandleMessage(session, new ClientRequestOwnedItemAuctions());

        Assert.Equal(2, GetMessages<ServerMarketplaceStatus>(sessionProxy).Count);
        Assert.Empty(Assert.Single(GetMessages<ServerOwnedCommodityOrders>(sessionProxy)).Orders);
        Assert.Empty(Assert.Single(GetMessages<ServerOwnedItemAuctions>(sessionProxy)).Auctions);
    }

    [Fact]
    public void OwnedMarketplaceRequests_WithRows_SendOwnedListPackets()
    {
        using ServiceProviderScope scope = UseMarketplaceProvider();
        GlobalMarketplaceManager manager = (GlobalMarketplaceManager)scope.Provider.GetRequiredService<IGlobalMarketplaceManager>();
        ClearMarketplaceState(manager);

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy);
        IItem item = CreateItem();
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), true);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        Assert.Equal(GenericError.Ok, manager.PostAuction(session.Player, item, 10ul, 20ul, out AuctionInfo auction));
        Assert.Equal(GenericError.Ok, manager.PostCommodityOrder(session.Player, CreateCommodityOrder(), out CommodityOrder commodityOrder));

        var commodityHandler = new ClientRequestOwnedCommodityOrdersHandler(NullLogger<ClientRequestOwnedCommodityOrdersHandler>.Instance);
        var auctionHandler = new ClientRequestOwnedItemAuctionsHandler(NullLogger<ClientRequestOwnedItemAuctionsHandler>.Instance);
        commodityHandler.HandleMessage(session, new ClientRequestOwnedCommodityOrders());
        auctionHandler.HandleMessage(session, new ClientRequestOwnedItemAuctions());

        ServerOwnedCommodityOrders ownedOrders = Assert.Single(GetMessages<ServerOwnedCommodityOrders>(sessionProxy));
        ServerOwnedItemAuctions ownedAuctions = Assert.Single(GetMessages<ServerOwnedItemAuctions>(sessionProxy));
        Assert.Equal(commodityOrder.CommodityOrderId, Assert.Single(ownedOrders.Orders).CommodityOrderId);
        Assert.Equal(auction.AuctionId, Assert.Single(ownedAuctions.Auctions).AuctionId);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), PlayerGuid);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        accountProxy.SetProperty(nameof(IAccount.Id), PlayerGuid);
        return session;
    }

    private static IItem CreateItem()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), ItemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = ItemId,
            MaxStackCount = 1u
        });

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), ItemId);
        itemProxy.SetProperty(nameof(IItem.Guid), ItemGuid);
        itemProxy.SetProperty(nameof(IItem.CharacterId), CharacterId);
        itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Inventory);
        itemProxy.SetProperty(nameof(IItem.StackCount), 1u);
        itemProxy.SetProperty(nameof(IItem.Soulbound), false);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static CommodityOrder CreateCommodityOrder()
    {
        return new CommodityOrder
        {
            Item2Id         = ItemId,
            Quantity        = 1u,
            PricePerUnit    = 10ul,
            Price           = 10ul,
            IsBuyOrder      = false,
            ForceImmediate  = false
        };
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(invocation => invocation.Arguments.Length == 1)
            .Select(invocation => invocation.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static void ClearMarketplaceState(GlobalMarketplaceManager manager)
    {
        FieldInfo auctionsField = typeof(GlobalMarketplaceManager).GetField("auctions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo commodityField = typeof(GlobalMarketplaceManager).GetField("commodityOrders", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((System.Collections.IList)auctionsField.GetValue(manager)!).Clear();
        ((System.Collections.IList)commodityField.GetValue(manager)!).Clear();
        SetPrivateField(manager, "nextAuctionId", 1ul);
        SetPrivateField(manager, "nextCommodityOrderId", 1ul);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
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
            Provider = provider;
            previous = LegacyServiceProvider.Provider;
            LegacyServiceProvider.Provider = provider;
        }

        public IServiceProvider Provider { get; }

        public void Dispose()
        {
            LegacyServiceProvider.Provider = previous;
        }
    }
}
