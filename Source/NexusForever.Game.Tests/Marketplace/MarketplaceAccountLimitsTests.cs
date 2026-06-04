using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entitlement;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Database.Auth;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Marketplace;

public class MarketplaceAccountLimitsTests
{
    private const ulong ItemGuid = 0xABCDEFul;

    [Fact]
    public void FreeAccount_HasRetailThreeSlotLimits()
    {
        IPlayer player = CreatePlayer(signatureEnabled: false);

        Assert.Equal(3, MarketplaceAccountLimits.GetMaxAuctionSellLots(player));
        Assert.Equal(3, MarketplaceAccountLimits.GetMaxAuctionBids(player));
        Assert.Equal(3, MarketplaceAccountLimits.GetMaxCommoditySellOrders(player));
        Assert.Equal(3, MarketplaceAccountLimits.GetMaxCommodityBuyOrders(player));
    }

    [Fact]
    public void SignatureAccount_HasRetailThirtySlotLimits()
    {
        IPlayer player = CreatePlayer(signatureEnabled: true);

        Assert.Equal(30, MarketplaceAccountLimits.GetMaxAuctionSellLots(player));
        Assert.Equal(30, MarketplaceAccountLimits.GetMaxAuctionBids(player));
        Assert.Equal(30, MarketplaceAccountLimits.GetMaxCommoditySellOrders(player));
        Assert.Equal(30, MarketplaceAccountLimits.GetMaxCommodityBuyOrders(player));
    }

    [Fact]
    public void ExtraAuctionEntitlement_AddsFiftySlotsPerStack()
    {
        IPlayer player = CreatePlayer(signatureEnabled: false, extraAuctionStacks: 1u);

        Assert.Equal(53, MarketplaceAccountLimits.GetMaxAuctionSellLots(player));
        Assert.Equal(3, MarketplaceAccountLimits.GetMaxCommoditySellOrders(player));
    }

    [Fact]
    public void PostAuction_EnforcesRetailFreeSellLimit()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var services = new ServiceCollection();
        services.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();
        GlobalMarketplaceManager manager = GlobalMarketplaceManager.Instance;

        try
        {
            IPlayer player = CreatePlayer(signatureEnabled: false);
            RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
            playerProxy.SetProperty(nameof(IPlayer.CharacterId), 9001ul);
            playerProxy.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));

            IItemInfo itemInfo = CreateItemInfo();
            for (int i = 0; i < 3; i++)
            {
                IItem item = CreateItem(itemInfo, ItemGuid + (ulong)i, player.CharacterId);
                GenericError result = manager.PostAuction(player, item, 10ul, 20ul, out _);
                Assert.Equal(GenericError.Ok, result);
            }

            IItem fourthItem = CreateItem(itemInfo, ItemGuid + 3ul, player.CharacterId);
            GenericError blocked = manager.PostAuction(player, fourthItem, 10ul, 20ul, out _);
            Assert.Equal(GenericError.AuctionTooManyOrders, blocked);
        }
        finally
        {
            ClearMarketplaceState(manager);
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ExtraCommodityEntitlement_AddsFiftySlotsPerStack()
    {
        IPlayer player = CreatePlayer(signatureEnabled: false, extraCommodityStacks: 2u);

        Assert.Equal(103, MarketplaceAccountLimits.GetMaxCommodityBuyOrders(player));
        Assert.Equal(3, MarketplaceAccountLimits.GetMaxAuctionBids(player));
    }

    private static IPlayer CreatePlayer(
        bool signatureEnabled,
        uint extraAuctionStacks = 0u,
        uint extraCommodityStacks = 0u)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        var entitlementManager = new StubAccountEntitlementManager();
        if (extraAuctionStacks > 0u)
            entitlementManager.Set(EntitlementType.ExtraAuctions, extraAuctionStacks);
        if (extraCommodityStacks > 0u)
            entitlementManager.Set(EntitlementType.ExtraCommodityOrders, extraCommodityStacks);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        playerProxy.SetProperty(nameof(IPlayer.SignatureEnabled), signatureEnabled);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        return player;
    }

    private sealed class StubAccountEntitlementManager : IAccountEntitlementManager
    {
        private readonly Dictionary<EntitlementType, uint> amounts = new();

        public void Set(EntitlementType type, uint amount) => amounts[type] = amount;

        public IAccountEntitlement GetEntitlement(EntitlementType type) =>
            amounts.TryGetValue(type, out uint amount) ? new StubAccountEntitlement(type, amount) : null;

        public void UpdateEntitlement(EntitlementType type, int value) => throw new NotSupportedException();

        public void Save(AuthContext context) { }

        public IEnumerator<IAccountEntitlement> GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StubAccountEntitlement : IAccountEntitlement
    {
        public StubAccountEntitlement(EntitlementType type, uint amount)
        {
            Type   = type;
            Amount = amount;
        }

        public EntitlementEntry Entry => null;
        public EntitlementType Type { get; }
        public uint Amount { get; set; }

        public ServerAccountEntitlement Build() => throw new NotSupportedException();

        public void Save(AuthContext context) { }
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001u, MaxStackCount = 1u });
        return itemInfo;
    }

    private static void ClearMarketplaceState(GlobalMarketplaceManager manager)
    {
        FieldInfo auctionsField = typeof(GlobalMarketplaceManager).GetField("auctions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo commodityField = typeof(GlobalMarketplaceManager).GetField("commodityOrders", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((System.Collections.IList)auctionsField.GetValue(manager)!).Clear();
        ((System.Collections.IList)commodityField.GetValue(manager)!).Clear();
    }

    private static IItem CreateItem(IItemInfo itemInfo, ulong guid, ulong ownerCharacterId)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), itemInfo.Id);
        itemProxy.SetProperty(nameof(IItem.Guid), guid);
        itemProxy.SetProperty(nameof(IItem.CharacterId), ownerCharacterId);
        itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Inventory);
        itemProxy.SetProperty(nameof(IItem.StackCount), 1u);
        itemProxy.SetProperty(nameof(IItem.Soulbound), false);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }
}
