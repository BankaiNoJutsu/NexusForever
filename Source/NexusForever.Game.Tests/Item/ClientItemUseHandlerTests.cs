using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Item;

public class ClientItemUseHandlerTests
{
    [Fact]
    public void HandleMessage_CastFailure_DoesNotConsumeItem()
    {
        ClientItemUseHandler handler = CreateHandler(spell4Id: 777u);
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 1u, maxStackCount: 20u),
            CastResult.CasterCannotBeDead,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, CreateRequest());

        RecordingDispatchProxy<IPlayer>.Invocation castCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(777u, castCall.Arguments[0]);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Equal(["cast"], callOrder);
    }

    [Fact]
    public void HandleMessage_CastSuccess_ConsumesItemAfterCast()
    {
        ClientItemUseHandler handler = CreateHandler(spell4Id: 888u);
        var callOrder = new List<string>();
        IItem item = CreateItem(stackCount: 1u, maxStackCount: 20u);
        IWorldSession session = CreateSession(
            item,
            CastResult.Ok,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, CreateRequest());

        RecordingDispatchProxy<IPlayer>.Invocation castCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(888u, castCall.Arguments[0]);
        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        Assert.Equal(["cast", "consume"], callOrder);
    }

    [Fact]
    public void HandleMessage_EmptyStack_DoesNotCastOrConsumeItem()
    {
        ClientItemUseHandler handler = CreateHandler(spell4Id: 999u);
        var callOrder = new List<string>();
        IWorldSession session = CreateSession(
            CreateItem(stackCount: 0u, maxStackCount: 20u),
            CastResult.Ok,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, CreateRequest());

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(callOrder);
    }

    [Fact]
    public void HandleMessage_StackableChargedConsumableWithZeroCharges_PrimesChargesBeforeCast()
    {
        ClientItemUseHandler handler = CreateHandler(spell4Id: 34985u);
        var callOrder = new List<string>();
        IItem item = CreateItem(stackCount: 1u, maxStackCount: 50u, maxCharges: 1u, charges: 0u);
        IWorldSession session = CreateSession(
            item,
            () => item.Charges > 0u ? CastResult.Ok : CastResult.SpellNoCharges,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, CreateRequest());

        Assert.Equal(1u, item.Charges);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        Assert.Equal(["cast", "consume"], callOrder);
    }

    [Fact]
    public void HandleMessage_CurrencyTreasure_ConsumesAndGrantsCurrency()
    {
        ClientItemUseHandler handler = CreateHandler(spell4Id: 0u);
        var callOrder = new List<string>();
        IItem item = CreateCurrencyTreasure(stackCount: 1u);
        IWorldSession session = CreateSession(
            item,
            CastResult.Ok,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy);

        handler.HandleMessage(session, CreateRequest());

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(1566ul, currencyCall.Arguments[1]);
        Assert.Equal(true, currencyCall.Arguments[2]);
        Assert.Equal(["consume", "currency"], callOrder);
    }

    [Fact]
    public void ContextAction_CurrencyTreasure_ConsumesAndGrantsCurrency()
    {
        ClientItemContextActionHandler handler = CreateContextActionHandler(spell4Id: 0u);
        var callOrder = new List<string>();
        IItem item = CreateCurrencyTreasure(stackCount: 1u);
        IWorldSession session = CreateSession(
            item,
            CastResult.Ok,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy);

        handler.HandleMessage(session, CreateContextActionRequest());

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(1566ul, currencyCall.Arguments[1]);
        Assert.Equal(true, currencyCall.Arguments[2]);
        Assert.Equal(["consume", "currency"], callOrder);
    }

    [Fact]
    public void ContextAction_ActivatedConsumable_PrimesChargesBeforeCast()
    {
        ClientItemContextActionHandler handler = CreateContextActionHandler(spell4Id: 34985u);
        var callOrder = new List<string>();
        IItem item = CreateItem(stackCount: 1u, maxStackCount: 50u, maxCharges: 1u, charges: 0u);
        IWorldSession session = CreateSession(
            item,
            () => item.Charges > 0u ? CastResult.Ok : CastResult.SpellNoCharges,
            callOrder,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, CreateContextActionRequest());

        Assert.Equal(1u, item.Charges);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        RecordingDispatchProxy<IInventory>.Invocation consumeCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(item, consumeCall.Arguments[0]);
        Assert.Equal(["cast", "consume"], callOrder);
    }

    private static ClientItemUseHandler CreateHandler(uint spell4Id)
    {
        return new ClientItemUseHandler(CreateGameTableManager(spell4Id));
    }

    private static ClientItemContextActionHandler CreateContextActionHandler(uint spell4Id)
    {
        return new ClientItemContextActionHandler(
            NullLogger<ClientItemContextActionHandler>.Instance,
            CreateGameTableManager(spell4Id));
    }

    private static IGameTableManager CreateGameTableManager(uint spell4Id)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemSpecial), CreateGameTable(new ItemSpecialEntry
        {
            Id = 123u,
            Spell4IdOnActivate = spell4Id
        }));
        proxy.SetProperty(nameof(IGameTableManager.CurrencyType), CreateGameTable(new CurrencyTypeEntry
        {
            Id = (uint)CurrencyType.Credits
        }));

        return gameTableManager;
    }

    private static IWorldSession CreateSession(
        IItem item,
        CastResult castResult,
        List<string> callOrder,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreateSession(
            item,
            () => castResult,
            callOrder,
            out inventoryProxy,
            out playerProxy,
            out _);
    }

    private static IWorldSession CreateSession(
        IItem item,
        CastResult castResult,
        List<string> callOrder,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy)
    {
        return CreateSession(
            item,
            () => castResult,
            callOrder,
            out inventoryProxy,
            out playerProxy,
            out currencyProxy);
    }

    private static IWorldSession CreateSession(
        IItem item,
        Func<CastResult> castResultFactory,
        List<string> callOrder,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreateSession(
            item,
            castResultFactory,
            callOrder,
            out inventoryProxy,
            out playerProxy,
            out _);
    }

    private static IWorldSession CreateSession(
        IItem item,
        Func<CastResult> castResultFactory,
        List<string> callOrder,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        inventoryProxy.SetMethodHandler(nameof(IInventory.ItemUse), _ =>
        {
            callOrder.Add("consume");
            return true;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        currencyProxy.SetMethodHandler(nameof(ICurrencyManager.CurrencyAddAmount), _ =>
        {
            callOrder.Add("currency");
            return null;
        });

        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryCastSpell), _ =>
        {
            callOrder.Add("cast");
            return castResultFactory();
        });

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetMethodHandler(nameof(IWorldSession.TryConsumeNextClientSpellEvidenceCapture), args =>
        {
            args[0] = false;
            return false;
        });
        return session;
    }

    private static IItem CreateItem(uint stackCount, uint maxStackCount, uint itemSpecialId = 123u, uint maxCharges = 0u, uint charges = 0u)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            ItemSpecialId00 = itemSpecialId,
            MaxStackCount   = maxStackCount,
            MaxCharges      = maxCharges
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.StackCount), stackCount);
        itemProxy.SetProperty(nameof(IItem.Charges), charges);
        return item;
    }

    private static IItem CreateCurrencyTreasure(uint stackCount)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id              = 50806u,
            Item2CategoryId = 94u,
            Item2TypeId     = 200u,
            MaxStackCount   = 100u,
            CurrencyTypeId  = [CurrencyType.Credits, CurrencyType.None],
            CurrencyAmount  = [1566u, 0u]
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.StackCount), stackCount);
        return item;
    }

    private static ClientItemUse CreateRequest()
    {
        return new ClientItemUse
        {
            Location =
            {
                Location = InventoryLocation.Inventory,
                BagIndex = 0u
            }
        };
    }

    private static ClientItemContextAction CreateContextActionRequest()
    {
        var request = new ClientItemContextAction();
        SetAutoProperty(request, nameof(ClientItemContextAction.ItemGuid), 1234ul);
        SetAutoProperty(request, nameof(ClientItemContextAction.SelectedBranch), true);
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
