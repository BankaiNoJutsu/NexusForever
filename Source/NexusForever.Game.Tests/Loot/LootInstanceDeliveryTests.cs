using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Chat.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootInstanceDeliveryTests
{
    private const uint StaticItemId = 91001u;

    [Fact]
    public void GiveLoot_StaticItemInventoryFull_DoesNotMarkDeliveredAndCanBeRetried()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        var inventory = new TestInventory(0u);
        IPlayer player = CreatePlayer(inventory, out var sessionProxy);
        var lootInstance = new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);

        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 3u);

        bool firstAttempt = lootInstance.GiveLoot(player, lootItem.Id);

        Assert.False(firstAttempt);
        Assert.False(lootItem.Delivered);
        Assert.Empty(inventory.CreatedItems);

        inventory.InventoryBag.SlotsRemaining = 3u;
        bool secondAttempt = lootInstance.GiveLoot(player, lootItem.Id);

        Assert.True(secondAttempt);
        Assert.True(lootItem.Delivered);

        TestInventory.ItemCreateCall itemCreate = Assert.Single(inventory.CreatedItems);
        Assert.Equal(InventoryLocation.Inventory, itemCreate.Location);
        Assert.Equal(StaticItemId, itemCreate.ItemId);
        Assert.Equal(3u, itemCreate.Count);
        Assert.Equal(ItemUpdateReason.Loot, itemCreate.Reason);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Collection(sessionCalls,
            call =>
            {
                var error = Assert.IsType<ServerItemError>(call.Arguments[0]);
                Assert.Equal(GenericError.ItemInventoryFull, error.ErrorCode);
            },
            call =>
            {
                var grant = Assert.IsType<ServerLootGrant>(call.Arguments[0]);
                Assert.Equal(99u, grant.OwnerUnitId);
                Assert.Equal(4242u, grant.LooterUnitId);
                Assert.Equal(lootItem.Id, grant.LootItem.LootUnitId);
                Assert.Equal(StaticItemId, grant.LootItem.ItemId);
                Assert.Equal(3u, grant.LootItem.Amount);
            });
    }

    [Fact]
    public void GiveLoot_Cash_SendsLootGrant()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithCurrencyManager(currencyManager)
            .WithSession(session)
            .WithCharacterId(42ul)
            .WithGuid(4242u)
            .Build();
        var lootInstance = new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);

        LootInstanceItem lootItem = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 17u);

        bool delivered = lootInstance.GiveLoot(player, lootItem.Id);

        Assert.True(delivered);
        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(17ul, currencyCall.Arguments[1]);
        Assert.True((bool)currencyCall.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
        var grant = Assert.IsType<ServerLootGrant>(sessionCall.Arguments[0]);
        Assert.Equal(99u, grant.OwnerUnitId);
        Assert.Equal(4242u, grant.LooterUnitId);
        Assert.Equal(lootItem.Id, grant.LootItem.LootUnitId);
        Assert.Equal(LootItemType.Cash, grant.LootItem.Type);
        Assert.Equal((uint)CurrencyType.Credits, grant.LootItem.ItemId);
        Assert.Equal(17u, grant.LootItem.Amount);
    }

    [Fact]
    public void DeliverAllLoot_MixedSuccessAndFailure_ReturnsFalseAndKeepsFailedItemRetryable()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        var inventory = new TestInventory(0u);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithInventory(inventory)
            .WithCurrencyManager(currencyManager)
            .WithSession(session)
            .WithCharacterId(42ul)
            .WithGuid(4242u)
            .Build();
        var lootInstance = new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);

        LootInstanceItem cash = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 17u);
        LootInstanceItem staticItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);

        bool delivered = lootInstance.DeliverAllLoot(player);

        Assert.False(delivered);
        Assert.True(cash.Delivered);
        Assert.False(staticItem.Delivered);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(17ul, currencyCall.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
        var error = Assert.IsType<ServerItemError>(sessionCall.Arguments[0]);
        Assert.Equal(GenericError.ItemInventoryFull, error.ErrorCode);
    }

    [Fact]
    public void GiveGeneratedLoot_WithGrantedNotify_WhenPartiallyDelivered_DoesNotSendGrantedNotify()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        var inventory = new TestInventory(0u);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithInventory(inventory)
            .WithCurrencyManager(currencyManager)
            .WithSession(session)
            .WithCharacterId(42ul)
            .WithGuid(4242u)
            .Build();

        manager.GiveGeneratedLoot(player, [
            new GeneratedLootItem(LootItemType.Cash, (uint)CurrencyType.Credits, 17u),
            new GeneratedLootItem(LootItemType.StaticItem, StaticItemId, 1u)
        ], ownerUnitId: 99u, sendGrantedNotify: true);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
        Assert.Equal(17ul, currencyCall.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
        var error = Assert.IsType<ServerItemError>(sessionCall.Arguments[0]);
        Assert.Equal(GenericError.ItemInventoryFull, error.ErrorCode);
    }

    [Fact]
    public void GiveLoot_VirtualItemUpdatesVirtualCollectObjective()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo(), CreateVirtualItemInfo(265u)));

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out var questManagerProxy);
        TestPlayerBuilder playerBuilder = TestPlayerBuilder.Create()
            .WithSession(session)
            .WithCharacterId(42ul)
            .WithGuid(4242u);
        playerBuilder.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        IPlayer player = playerBuilder.Build();
        var lootInstance = new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);

        LootInstanceItem lootItem = lootInstance.AddLootItem(265u, LootItemType.VirtualItem, 2u);

        bool delivered = lootInstance.GiveLoot(player, lootItem.Id);

        Assert.True(delivered);
        Assert.True(lootItem.Delivered);

        RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdate.Arguments[0]);
        Assert.Equal(265u, objectiveUpdate.Arguments[1]);
        Assert.Equal(2u, objectiveUpdate.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation grantCall = Assert.Single(sessionCalls);
        var grant = Assert.IsType<ServerLootGrant>(grantCall.Arguments[0]);
        Assert.Equal(99u, grant.OwnerUnitId);
        Assert.Equal(4242u, grant.LooterUnitId);
        Assert.Equal(lootItem.Id, grant.LootItem.LootUnitId);
        Assert.Equal(LootItemType.VirtualItem, grant.LootItem.Type);
        Assert.Equal(265u, grant.LootItem.ItemId);
        Assert.Equal(2u, grant.LootItem.Amount);
    }

    private static IServiceProvider BuildProvider(IItemInfo itemInfo, VirtualItemEntry virtualItem = null)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var lootManager = new GlobalLootManager(groupStateManager);
        var itemManager = new ItemManager();
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        typeof(ItemManager)
            .GetField("item", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(itemManager, ImmutableDictionary<uint, IItemInfo>.Empty.Add(StaticItemId, itemInfo));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id = StaticItemId
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.VirtualItem), CreateGameTable(virtualItem ?? CreateVirtualItemInfo(0u)));

        return new ServiceCollection()
            .AddSingleton(lootManager)
            .AddSingleton(itemManager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = StaticItemId,
            MaxStackCount = 20u
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        return itemInfo;
    }

    private static VirtualItemEntry CreateVirtualItemInfo(uint virtualItemId)
    {
        return new VirtualItemEntry
        {
            Id             = virtualItemId,
            ItemQualityId  = 4u
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static IPlayer CreatePlayer(IInventory inventory, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        return TestPlayerBuilder.Create()
            .WithInventory(inventory)
            .WithSession(session)
            .WithCharacterId(42ul)
            .WithGuid(4242u)
            .Build();
    }

}
