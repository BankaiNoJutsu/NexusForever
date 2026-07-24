using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public class ClientVendorSellJunkHandlerTests
{
    [Fact]
    public void HandleMessage_SellsAllBackpackJunkStacks()
    {
        IItem hide = CreateItem(categoryId: 94u, stackCount: 11u, sellAmount: 4u, InventoryLocation.Inventory, bagIndex: 2u);
        IItem coinCache = CreateItem(categoryId: 94u, stackCount: 4u, sellAmount: 31u, InventoryLocation.Inventory, bagIndex: 3u);
        IItem nonJunk = CreateItem(categoryId: 12u, stackCount: 5u, sellAmount: 99u, InventoryLocation.Inventory, bagIndex: 4u);
        IItem equippedJunk = CreateItem(categoryId: 94u, stackCount: 1u, sellAmount: 99u, InventoryLocation.Equipped, bagIndex: 5u);

        IWorldSession session = CreateSession(
            [hide, coinCache, nonJunk],
            [equippedJunk],
            selectedVendor: true,
            out IPlayer player,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out IBuybackManager buybackManager,
            out RecordingDispatchProxy<IBuybackManager> buybackProxy,
            out _);
        var handler = new ClientRepairVendorStatusRequestHandler(
            NullLogger<ClientRepairVendorStatusRequestHandler>.Instance,
            buybackManager);

        handler.HandleMessage(session, new ClientRepairVendorStatusRequest());

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> deleteCalls = inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete));
        Assert.Equal(2, deleteCalls.Count);
        AssertDelete(deleteCalls[0], InventoryLocation.Inventory, 2u, 11u);
        AssertDelete(deleteCalls[1], InventoryLocation.Inventory, 3u, 4u);

        IReadOnlyList<RecordingDispatchProxy<ICurrencyManager>.Invocation> currencyCalls = currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount));
        Assert.Equal(2, currencyCalls.Count);
        Assert.Equal(44ul, currencyCalls[0].Arguments[1]);
        Assert.Equal(124ul, currencyCalls[1].Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IBuybackManager>.Invocation> buybackCalls = buybackProxy.GetInvocations(nameof(IBuybackManager.AddItem));
        Assert.Equal(2, buybackCalls.Count);
        Assert.Same(player, buybackCalls[0].Arguments[0]);
        Assert.Same(hide, buybackCalls[0].Arguments[1]);
        Assert.Equal(11u, buybackCalls[0].Arguments[2]);
        Assert.Same(coinCache, buybackCalls[1].Arguments[1]);
        Assert.Equal(4u, buybackCalls[1].Arguments[2]);
    }

    [Fact]
    public void HandleMessage_NoSelectedVendorRejectsWithoutSelling()
    {
        IItem junk = CreateItem(categoryId: 94u, stackCount: 1u, sellAmount: 4u, InventoryLocation.Inventory, bagIndex: 2u);
        IWorldSession session = CreateSession(
            [junk],
            [],
            selectedVendor: false,
            out _,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out IBuybackManager buybackManager,
            out RecordingDispatchProxy<IBuybackManager> buybackProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientRepairVendorStatusRequestHandler(
            NullLogger<ClientRepairVendorStatusRequestHandler>.Instance,
            buybackManager);

        handler.HandleMessage(session, new ClientRepairVendorStatusRequest());

        RecordingDispatchProxy<IPlayer>.Invocation errorCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.VendorNoVendor, errorCall.Arguments[0]);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Empty(buybackProxy.GetInvocations(nameof(IBuybackManager.AddItem)));
    }

    private static IWorldSession CreateSession(
        IReadOnlyList<IItem> inventoryItems,
        IReadOnlyList<IItem> equippedItems,
        bool selectedVendor,
        out IPlayer player,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out IBuybackManager buybackManager,
        out RecordingDispatchProxy<IBuybackManager> buybackProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IBag inventoryBag = CreateBag(InventoryLocation.Inventory, inventoryItems);
        IBag equippedBag = CreateBag(InventoryLocation.Equipped, equippedItems);
        IBag[] bags = [inventoryBag, equippedBag];
        Dictionary<(InventoryLocation Location, uint BagIndex), IItem> itemsByLocation = inventoryItems
            .Concat(equippedItems)
            .ToDictionary(i => (i.Location, i.BagIndex));

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IEnumerable<IBag>.GetEnumerator), _ => ((IEnumerable<IBag>)bags).GetEnumerator());
        inventoryProxy.SetMethodHandler(nameof(IInventory.ItemDelete), args =>
        {
            var itemLocation = (ItemLocation)args[0];
            return itemsByLocation[(itemLocation.Location, itemLocation.BagIndex)];
        });

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IBuybackManager buyback = RecordingDispatchProxy<IBuybackManager>.Create(out buybackProxy);

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        if (selectedVendor)
        {
            IVendorInfo vendorInfo = RecordingDispatchProxy<IVendorInfo>.Create(out RecordingDispatchProxy<IVendorInfo> vendorProxy);
            vendorProxy.SetProperty(nameof(IVendorInfo.BuyPriceMultiplier), 1.0f);
            playerProxy.SetProperty(nameof(IPlayer.SelectedVendorInfo), vendorInfo);
        }

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        buybackManager = buyback;
        return session;
    }

    private static IBag CreateBag(InventoryLocation location, IReadOnlyList<IItem> items)
    {
        IBag bag = RecordingDispatchProxy<IBag>.Create(out RecordingDispatchProxy<IBag> bagProxy);
        bagProxy.SetProperty(nameof(IBag.Location), location);
        bagProxy.SetMethodHandler(nameof(IEnumerable<IItem>.GetEnumerator), _ => items.GetEnumerator());
        return bag;
    }

    private static IItem CreateItem(uint categoryId, uint stackCount, uint sellAmount, InventoryLocation location, uint bagIndex)
    {
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = 123u,
            Item2CategoryId = categoryId
        });

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.StackCount), stackCount);
        itemProxy.SetProperty(nameof(IItem.Location), location);
        itemProxy.SetProperty(nameof(IItem.BagIndex), bagIndex);
        itemProxy.SetMethodHandler(nameof(IItem.GetVendorSellCurrency), args =>
        {
            return (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None;
        });
        itemProxy.SetMethodHandler(nameof(IItem.GetVendorSellAmount), args =>
        {
            return (byte)args[0] == 0 ? sellAmount : 0u;
        });
        return item;
    }

    private static void AssertDelete(RecordingDispatchProxy<IInventory>.Invocation invocation, InventoryLocation location, uint bagIndex, uint count)
    {
        ItemLocation itemLocation = Assert.IsType<ItemLocation>(invocation.Arguments[0]);
        Assert.Equal(location, itemLocation.Location);
        Assert.Equal(bagIndex, itemLocation.BagIndex);
        Assert.Equal(count, invocation.Arguments[1]);
        Assert.Equal(ItemUpdateReason.Vendor, invocation.Arguments[2]);
    }
}
