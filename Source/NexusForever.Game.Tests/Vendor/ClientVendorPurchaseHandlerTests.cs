using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public sealed class ClientVendorPurchaseHandlerTests
{
    [Theory]
    [InlineData(false, 1u, 1u, false, null)]
    [InlineData(true, 1u, 2u, false, null)]
    [InlineData(true, 2u, 1u, false, null)]
    [InlineData(true, 1u, 1u, true, null)]
    [InlineData(true, 1u, 1u, true, "session")]
    [InlineData(true, 1u, 1u, true, "inventory")]
    public void PurchaseChecksCapacityAfterTheExactItemPayment(bool payWithItem, uint paymentStack, uint quantity, bool fits, string rebindOnDebit)
    {
        const uint purchasedId = 100;
        const uint tokenId = 101;
        IItemInfo purchase = CreateInfo(purchasedId, stackable: false);
        IItemInfo token = CreateInfo(tokenId, stackable: true);
        IItemManager items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        itemManager.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => (uint)args[0] == purchasedId ? purchase : token);
        ulong nextId = 1000;
        itemManager.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => nextId++);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
        actor.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        actor.SetProperty(nameof(IPlayer.IsLoading), true);
        actor.SetProperty(nameof(IPlayer.Session), session);
        network.SetProperty(nameof(IWorldSession.Player), player);

        ulong credits = 100;
        RecordingDispatchProxy<IInventory> replacementInventory = null;
        ICurrencyManager wallet = RecordingDispatchProxy<ICurrencyManager>.Create(out var balance);
        balance.SetMethodHandler(nameof(ICurrencyManager.CanAfford), args => credits >= (ulong)args[1]);
        balance.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args =>
        {
            Assert.Equal(CurrencyType.Credits, args[0]);
            credits -= (ulong)args[1];
            if (rebindOnDebit == "session")
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            else if (rebindOnDebit == "inventory")
                actor.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out replacementInventory));
            return null;
        });
        actor.SetProperty(nameof(IPlayer.CurrencyManager), wallet);

        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        var bag = new Bag(InventoryLocation.Inventory, 1, items);
        SetInventoryField("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
        SetInventoryField("deletedItems", new List<IItem>());
        SetInventoryField("player", player);
        SetInventoryField("characterId", 42ul);
        SetInventoryField("itemManager", items);
        actor.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventory.ItemCreate(InventoryLocation.Inventory, token, paymentStack);
        actor.SetProperty(nameof(IPlayer.IsLoading), false);

        var stock = new EntityVendorItemModel { ItemId = purchasedId };
        if (payWithItem)
        {
            stock.ExtraCost1Type = ItemExtraCostType.Item;
            stock.ExtraCost1ItemOrCurrencyId = tokenId;
            stock.ExtraCost1Quantity = 1;
            stock.ExtraCost2Type = ItemExtraCostType.Currency;
            stock.ExtraCost2ItemOrCurrencyId = (uint)CurrencyType.Credits;
            stock.ExtraCost2Quantity = 15;
        }
        IVendorInfo vendor = RecordingDispatchProxy<IVendorInfo>.Create(out var offers);
        offers.SetMethodReturn(nameof(IVendorInfo.GetItemAtIndex), stock);
        actor.SetProperty(nameof(IPlayer.SelectedVendorInfo), vendor);

        new ClientVendorPurchaseHandler(items).HandleMessage(session, CreateRequest(quantity));
        object[] messages = network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();

        if (fits)
        {
            Assert.Equal(85ul, credits);
            Assert.Equal(purchasedId, Assert.Single(bag).Id);
            Assert.Single(balance.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            if (rebindOnDebit == null)
            {
                Assert.Single(messages.OfType<ServerItemDelete>());
                Assert.Single(messages.OfType<ServerItemAdd>());
            }
            else
            {
                Assert.Empty(messages.OfType<ServerItemDelete>());
                Assert.Empty(messages.OfType<ServerItemAdd>());
                if (rebindOnDebit == "inventory")
                    Assert.Empty(replacementInventory.GetInvocations(nameof(IInventory.ItemDelete)));
            }
            return;
        }

        Assert.Equal(100ul, credits);
        Assert.Empty(balance.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        IItem remaining = Assert.Single(bag);
        Assert.Equal(tokenId, remaining.Id);
        Assert.Equal(paymentStack, remaining.StackCount);
        var error = Assert.Single(messages.OfType<ServerItemError>());
        Assert.Equal(GenericError.ItemInventoryFull, error.ErrorCode);

        void SetInventoryField(string name, object value)
            => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
    }

    internal static ClientVendorPurchase CreateRequest(uint quantity)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(0u);
            writer.Write(quantity);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var request = new ClientVendorPurchase();
        request.Read(reader);
        return request;
    }

    private static IItemInfo CreateInfo(uint id, bool stackable)
    {
        IItemInfo item = RecordingDispatchProxy<IItemInfo>.Create(out var info);
        info.SetProperty(nameof(IItemInfo.Id), id);
        info.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = id,
            MaxStackCount = stackable ? 10u : 1u,
            BuyFromVendorStackCount = 1,
            CurrencyTypeId = [CurrencyType.Credits, CurrencyType.None]
        });
        info.SetMethodReturn(nameof(IItemInfo.IsStackable), stackable);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorBuyCurrency), args => (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorBuyAmount), args => (byte)args[0] == 0 ? 15u : 0u);
        return item;
    }
}
