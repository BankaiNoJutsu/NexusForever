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

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), CreateLootBag());

        return session;
    }

    private static IItem CreateLootBag()
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);

        itemProxy.SetProperty(nameof(IItem.Guid), ItemGuid);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id              = 80875u,
            Item2CategoryId = 138u
        });

        return item;
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
