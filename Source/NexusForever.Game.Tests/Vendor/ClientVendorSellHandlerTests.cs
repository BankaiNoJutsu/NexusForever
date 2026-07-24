using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public class ClientVendorSellHandlerTests
{
    [Fact]
    public void HandleMessage_ZeroQuantitySellsFullStackForBulkVendorActions()
    {
        IItem item = CreateItem(stackCount: 3u, sellAmount: 7u);
        IWorldSession session = CreateSession(
            item,
            vendorBuyPriceMultiplier: 2.0f,
            out IPlayer player,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out IBuybackManager buybackManager,
            out RecordingDispatchProxy<IBuybackManager> buybackProxy);
        var handler = new ClientVendorSellHandler(buybackManager);

        ClientVendorSell request = CreateRequest(quantity: 0u);

        handler.HandleMessage(session, request);

        RecordingDispatchProxy<IInventory>.Invocation deleteCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Same(request.ItemLocation, deleteCall.Arguments[0]);
        Assert.Equal(3u, deleteCall.Arguments[1]);
        Assert.Equal(ItemUpdateReason.Vendor, deleteCall.Arguments[2]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(42ul, currencyCall.Arguments[1]);

        RecordingDispatchProxy<IBuybackManager>.Invocation buybackCall = Assert.Single(buybackProxy.GetInvocations(nameof(IBuybackManager.AddItem)));
        Assert.Same(player, buybackCall.Arguments[0]);
        Assert.Same(item, buybackCall.Arguments[1]);
        Assert.Equal(3u, buybackCall.Arguments[2]);
        var currencyChange = Assert.IsType<List<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)>>(buybackCall.Arguments[3]);
        (CurrencyType currencyTypeId, ulong currencyAmount) = Assert.Single(currencyChange);
        Assert.Equal(CurrencyType.Credits, currencyTypeId);
        Assert.Equal(42ul, currencyAmount);
    }

    [Fact]
    public void HandleMessage_QuantityAboveStackDoesNotSell()
    {
        IItem item = CreateItem(stackCount: 3u, sellAmount: 7u);
        IWorldSession session = CreateSession(
            item,
            vendorBuyPriceMultiplier: 1.0f,
            out _,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out IBuybackManager buybackManager,
            out RecordingDispatchProxy<IBuybackManager> buybackProxy);
        var handler = new ClientVendorSellHandler(buybackManager);

        handler.HandleMessage(session, CreateRequest(quantity: 4u));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Empty(buybackProxy.GetInvocations(nameof(IBuybackManager.AddItem)));
    }

    private static IWorldSession CreateSession(
        IItem item,
        float vendorBuyPriceMultiplier,
        out IPlayer player,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out IBuybackManager buybackManager,
        out RecordingDispatchProxy<IBuybackManager> buybackProxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), item);

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);

        IVendorInfo vendorInfo = RecordingDispatchProxy<IVendorInfo>.Create(out RecordingDispatchProxy<IVendorInfo> vendorProxy);
        vendorProxy.SetProperty(nameof(IVendorInfo.BuyPriceMultiplier), vendorBuyPriceMultiplier);

        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.SelectedVendorInfo), vendorInfo);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        buybackManager = RecordingDispatchProxy<IBuybackManager>.Create(out buybackProxy);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IItem CreateItem(uint stackCount, uint sellAmount)
    {
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = 123u
        });

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.StackCount), stackCount);
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

    private static ClientVendorSell CreateRequest(uint quantity)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(InventoryLocation.Inventory, 9u);
            writer.Write(5u);
            writer.Write(quantity);
            writer.FlushBits();
        }

        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var request = new ClientVendorSell();
        request.Read(reader);
        return request;
    }
}
