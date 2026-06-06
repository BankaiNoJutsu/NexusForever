using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Account.Inventory;

public class ClientItemGenericUnlockHandlerTests
{
    [Fact]
    public void HandleMessage_WithMissingItemReturnsInvalid()
    {
        IWorldSession session = CreateSession(item: null, out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy, out RecordingDispatchProxy<IInventory> inventoryProxy);
        var handler = new ClientItemGenericUnlockHandler(
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

        handler.HandleMessage(session, new ClientItemGenericUnlock());

        AssertUnlockResult(unlockProxy, GenericUnlockResult.Invalid);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void HandleMessage_WithMissingItemAndNullLoggerReturnsInvalid()
    {
        IWorldSession session = CreateSession(item: null, out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy, out RecordingDispatchProxy<IInventory> inventoryProxy);
        var handler = new ClientItemGenericUnlockHandler(
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            null);

        handler.HandleMessage(session, new ClientItemGenericUnlock());

        AssertUnlockResult(unlockProxy, GenericUnlockResult.Invalid);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void HandleMessage_WithMissingItemInfoReturnsInvalid()
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        IWorldSession session = CreateSession(item, out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy, out RecordingDispatchProxy<IInventory> inventoryProxy);
        var handler = new ClientItemGenericUnlockHandler(
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

        handler.HandleMessage(session, new ClientItemGenericUnlock());

        AssertUnlockResult(unlockProxy, GenericUnlockResult.Invalid);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void HandleMessage_WithNonGenericUnlockItemReturnsInvalid()
    {
        IItem item = CreateItem(new Item2Entry { GenericUnlockSetId = 0u });
        IWorldSession session = CreateSession(item, out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy, out RecordingDispatchProxy<IInventory> inventoryProxy);
        var handler = new ClientItemGenericUnlockHandler(
            RecordingDispatchProxy<IGameTableManager>.Create(out _),
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

        handler.HandleMessage(session, new ClientItemGenericUnlock());

        AssertUnlockResult(unlockProxy, GenericUnlockResult.Invalid);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    private static IWorldSession CreateSession(
        IItem item,
        out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        IGenericUnlockManager unlockManager = RecordingDispatchProxy<IGenericUnlockManager>.Create(out unlockProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.GenericUnlockManager), unlockManager);

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IItem CreateItem(Item2Entry entry)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), entry);

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static void AssertUnlockResult(RecordingDispatchProxy<IGenericUnlockManager> unlockProxy, GenericUnlockResult result)
    {
        RecordingDispatchProxy<IGenericUnlockManager>.Invocation invocation =
            Assert.Single(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.SendUnlockResult)));
        Assert.Equal(result, invocation.Arguments[0]);
    }
}
