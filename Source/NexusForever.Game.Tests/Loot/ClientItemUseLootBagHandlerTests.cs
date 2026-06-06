using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Loot;

public class ClientItemUseLootBagHandlerTests
{
    private const ulong ItemGuid = 12345ul;

    [Fact]
    public void HandleMessage_WithValidLootBagButMissingLoot_DoesNotDisconnect()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy, out _);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        lootManagerProxy.SetMethodReturn(nameof(IGlobalLootManager.HasLoot), false);
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        RecordingDispatchProxy<IPlayer>.Invocation error = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.ItemNoItems, error.Arguments[0]);
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TryUseLootBag)));
    }

    [Fact]
    public void HandleMessage_WhenLootBagDeliveryFindsInventoryFull_SendsInventoryFullError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy, out _);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        lootManagerProxy.SetMethodReturn(nameof(IGlobalLootManager.HasLoot), true);
        lootManagerProxy.SetMethodHandler(nameof(IGlobalLootManager.TryUseLootBag), args =>
        {
            args[2] = "inventory-full";
            return false;
        });
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        RecordingDispatchProxy<IPlayer>.Invocation error = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.ItemInventoryFull, error.Arguments[0]);
    }

    [Fact]
    public void HandleMessage_WithInvalidItemLocationSendsItemBadId()
    {
        IWorldSession session = CreateSession(
            CreateLootBag(),
            out _,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), _ => throw new ArgumentException("invalid item location"));
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        AssertItemError(sessionProxy, ItemGuid, GenericError.ItemBadId);
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.HasLoot)));
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TryUseLootBag)));
    }

    [Fact]
    public void HandleMessage_WithNonLootBagSendsCannotBeSalvaged()
    {
        IItem item = CreateItem(item2CategoryId: 1u);
        IWorldSession session = CreateSession(
            item,
            out _,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        lootManagerProxy.SetMethodHandler(nameof(IGlobalLootManager.TrySalvageItem), args =>
        {
            args[2] = "missing-item-salvage:80875";
            return false;
        });
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        AssertItemError(sessionProxy, ItemGuid, GenericError.ItemCannotBeSalvaged);
        RecordingDispatchProxy<IGlobalLootManager>.Invocation salvageCall = Assert.Single(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TrySalvageItem)));
        Assert.Same(session.Player, salvageCall.Arguments[0]);
        Assert.Same(item, salvageCall.Arguments[1]);
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.HasLoot)));
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TryUseLootBag)));
    }

    [Fact]
    public void HandleMessage_WithSalvageableNonLootBagUsesSalvageManager()
    {
        IItem item = CreateItem(item2CategoryId: 1u);
        IWorldSession session = CreateSession(
            item,
            out _,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        lootManagerProxy.SetMethodHandler(nameof(IGlobalLootManager.TrySalvageItem), args =>
        {
            args[2] = string.Empty;
            return true;
        });
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        RecordingDispatchProxy<IGlobalLootManager>.Invocation salvageCall = Assert.Single(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TrySalvageItem)));
        Assert.Same(session.Player, salvageCall.Arguments[0]);
        Assert.Same(item, salvageCall.Arguments[1]);
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.HasLoot)));
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TryUseLootBag)));
    }

    [Fact]
    public void HandleMessage_WhenSalvageFindsInventoryFullSendsInventoryFullError()
    {
        IWorldSession session = CreateSession(
            CreateItem(item2CategoryId: 1u),
            out _,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        lootManagerProxy.SetMethodHandler(nameof(IGlobalLootManager.TrySalvageItem), args =>
        {
            args[2] = "inventory-full";
            return false;
        });
        var handler = new ClientItemUseLootBagHandler(lootManager);

        handler.HandleMessage(session, CreateRequest());

        AssertItemError(sessionProxy, ItemGuid, GenericError.ItemInventoryFull);
        Assert.Single(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TrySalvageItem)));
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.HasLoot)));
        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.TryUseLootBag)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        return CreateSession(CreateLootBag(), out playerProxy, out inventoryProxy, out _);
    }

    private static IWorldSession CreateSession(
        IItem item,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

        return session;
    }

    private static IItem CreateLootBag()
    {
        return CreateItem(138u);
    }

    private static IItem CreateItem(uint item2CategoryId)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);

        itemProxy.SetProperty(nameof(IItem.Guid), ItemGuid);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id              = 80875u,
            Item2CategoryId = item2CategoryId
        });

        return item;
    }

    private static void AssertItemError(RecordingDispatchProxy<IWorldSession> sessionProxy, ulong itemGuid, GenericError errorCode)
    {
        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        var error = Assert.IsType<ServerItemError>(invocation.Arguments[0]);
        Assert.Equal(itemGuid, error.ItemGuid);
        Assert.Equal(errorCode, error.ErrorCode);
    }

    private static ClientItemUseLootBag CreateRequest()
    {
        var request = new ClientItemUseLootBag
        {
            ItemLocation =
            {
                Location = InventoryLocation.Inventory,
                BagIndex = 0u
            }
        };
        typeof(ClientItemUseLootBag)
            .GetProperty(nameof(ClientItemUseLootBag.Guid), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(request, ItemGuid);
        return request;
    }
}
