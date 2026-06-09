using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Item;

public class ClientItemUseDecorHandlerTests
{
    [Fact]
    public void HandleMessage_BlockedResidence_DoesNotConsumeDecorItem()
    {
        ClientItemUseDecorHandler handler = CreateHandler(out _);
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 1u, maxStackCount: 20u),
            residenceHandler: _ =>
            {
                callOrder.Add("residence");
                throw new HousingException();
            },
            itemUseResult: true,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);

        handler.HandleMessage(session, CreateRequest(12345ul));

        Assert.Single(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.GetOrCreateResidence)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.DecorCreate)));
        Assert.Equal(["residence"], callOrder);
    }

    [Fact]
    public void HandleMessage_EmptyStack_DoesNotResolveResidenceOrConsume()
    {
        ClientItemUseDecorHandler handler = CreateHandler(out _);
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 0u, maxStackCount: 20u),
            residenceHandler: args =>
            {
                callOrder.Add("residence");
                return RecordingDispatchProxy<IResidence>.Create(out var _);
            },
            itemUseResult: true,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);

        handler.HandleMessage(session, CreateRequest(12345ul));

        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.GetOrCreateResidence)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.DecorCreate)));
        Assert.Empty(callOrder);
    }

    [Fact]
    public void HandleMessage_ItemUseFailure_DoesNotCreateDecor()
    {
        ClientItemUseDecorHandler handler = CreateHandler(out _);
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 1u, maxStackCount: 20u),
            residenceHandler: args =>
            {
                callOrder.Add("residence");
                return RecordingDispatchProxy<IResidence>.Create(out var _);
            },
            itemUseResult: false,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);

        handler.HandleMessage(session, CreateRequest(12345ul));

        Assert.Single(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.GetOrCreateResidence)));
        Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.DecorCreate)));
        Assert.Equal(["residence", "consume"], callOrder);
    }

    [Fact]
    public void HandleMessage_MissingDecorInfoTable_ThrowsBeforeResidenceOrConsume()
    {
        ClientItemUseDecorHandler handler = CreateHandlerWithoutDecorInfo();
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 1u, maxStackCount: 20u),
            residenceHandler: args =>
            {
                callOrder.Add("residence");
                return RecordingDispatchProxy<IResidence>.Create(out var _);
            },
            itemUseResult: true,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(12345ul)));

        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.GetOrCreateResidence)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.DecorCreate)));
        Assert.Empty(callOrder);
    }

    [Fact]
    public void HandleMessage_Success_ConsumesAfterResidencePreflightBeforeDecor()
    {
        ClientItemUseDecorHandler handler = CreateHandler(out HousingDecorInfoEntry decorEntry);
        var callOrder = new List<string>();
        IItem item = CreateItem(stackCount: 1u, maxStackCount: 20u);
        IWorldSession session = CreateSession(
            item,
            residenceHandler: args =>
            {
                callOrder.Add("residence");
                return RecordingDispatchProxy<IResidence>.Create(out var _);
            },
            itemUseResult: true,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);

        handler.HandleMessage(session, CreateRequest(12345ul));

        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        RecordingDispatchProxy<IResidenceManager>.Invocation decorCall = Assert.Single(residenceManagerProxy.GetInvocations(nameof(IResidenceManager.DecorCreate)));
        Assert.Same(decorEntry, decorCall.Arguments[0]);
        Assert.Equal(["residence", "consume", "decor"], callOrder);
    }

    private static ClientItemUseDecorHandler CreateHandler(out HousingDecorInfoEntry decorEntry)
    {
        decorEntry = new HousingDecorInfoEntry
        {
            Id = 321u
        };

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.HousingDecorInfo), CreateGameTable(decorEntry));

        return new ClientItemUseDecorHandler(NullLogger<ClientItemUseDecorHandler>.Instance, gameTableManager);
    }

    private static ClientItemUseDecorHandler CreateHandlerWithoutDecorInfo()
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        return new ClientItemUseDecorHandler(NullLogger<ClientItemUseDecorHandler>.Instance, gameTableManager);
    }

    private static IWorldSession CreateSession(
        IItem item,
        Func<object[], object> residenceHandler,
        bool itemUseResult,
        List<string> callOrder,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        inventoryProxy.SetMethodHandler(nameof(IInventory.ItemUse), _ =>
        {
            callOrder.Add("consume");
            return itemUseResult;
        });

        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out residenceManagerProxy);
        residenceManagerProxy.SetMethodHandler(nameof(IResidenceManager.GetOrCreateResidence), residenceHandler);
        residenceManagerProxy.SetMethodHandler(nameof(IResidenceManager.DecorCreate), _ =>
        {
            callOrder.Add("decor");
            return null;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IItem CreateItem(uint stackCount, uint maxStackCount)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            HousingDecorInfoId = 321u,
            MaxStackCount = maxStackCount
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.StackCount), stackCount);
        return item;
    }

    private static ClientItemUseDecor CreateRequest(ulong itemGuid)
    {
        var request = new ClientItemUseDecor();
        SetAutoProperty(request, nameof(ClientItemUseDecor.ItemGuid), itemGuid);
        return request;
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
