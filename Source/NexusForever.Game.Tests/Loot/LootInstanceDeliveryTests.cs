using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
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
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateItemInfo());

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiveLoot_Cash_SendsLootGrant()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateItemInfo());

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiveLoot_VirtualItemUpdatesVirtualCollectObjective()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateItemInfo(), CreateVirtualItemInfo(265u));

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
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

    private sealed class TestInventory : IInventory
    {
        public sealed record ItemCreateCall(InventoryLocation Location, uint ItemId, uint Count, ItemUpdateReason Reason);

        public TestBag InventoryBag { get; }
        public List<ItemCreateCall> CreatedItems { get; } = [];

        public TestInventory(uint slotsRemaining)
        {
            InventoryBag = new TestBag(slotsRemaining);
        }

        public void Save(Database.Character.CharacterContext context)
        {
        }

        public void Update(double lastTick)
        {
        }

        public bool IsVisualItemSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public bool IsEquippableBagSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public bool IsEquippableBankBagSlot(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public bool IsInventoryFull(InventoryLocation location) => InventoryBag.SlotsRemaining == 0u;
        public uint GetInventorySlotsRemaining(InventoryLocation location) => InventoryBag.SlotsRemaining;
        public bool HasItemCount(uint itemId, uint count) => throw new NotSupportedException();
        public uint GetItemCount(uint itemId) => throw new NotSupportedException();
        public IItem GetItem(ItemLocation itemLocation) => throw new NotSupportedException();
        public IItem GetItem(InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public IItem GetItem(ulong guid) => throw new NotSupportedException();
        public IEnumerable<IItemVisual> GetItemVisuals() => throw new NotSupportedException();
        public IItem SpellCreate(Spell4BaseEntry spell4BaseEntry, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();

        public void ItemCreate(InventoryLocation location, uint itemId, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            CreatedItems.Add(new ItemCreateCall(location, itemId, count, reason));
            if (InventoryBag.SlotsRemaining > 0u)
                InventoryBag.SlotsRemaining--;
        }

        public void ItemCreate(InventoryLocation location, IItemInfo info, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            throw new NotSupportedException();
        }

        public GenericError? CanMoveItem(IItem item, ItemLocation location) => throw new NotSupportedException();
        public GenericError? CanMoveItem(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public void ItemMove(IItem item, ItemLocation location) => throw new NotSupportedException();
        public void ItemMove(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public void ItemSplit(ulong itemGuid, ItemLocation newItemLocation, uint count) => throw new NotSupportedException();
        public IItem ItemDelete(ItemLocation from, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
        public IItem ItemDelete(ItemLocation from, uint count, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
        public void ItemDelete(uint itemId, uint count = 1, ItemUpdateReason reason = ItemUpdateReason.Loot) => throw new NotSupportedException();
        public void ItemRemove(IItem item, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();
        public void AddItem(IItem item, InventoryLocation location, ItemUpdateReason reason = ItemUpdateReason.NoReason) => throw new NotSupportedException();
        public void LoadItem(IItem item, InventoryLocation location, uint bagIndex) => throw new NotSupportedException();
        public bool ItemUse(IItem item) => throw new NotSupportedException();
        public void ItemMoveToSupplySatchel(IItem item, uint amount) => throw new NotSupportedException();

        public IEnumerator<IBag> GetEnumerator()
        {
            yield return InventoryBag;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class TestBag : IBag
    {
        public InventoryLocation Location => InventoryLocation.Inventory;
        public uint Slots => SlotsRemaining;
        public uint SlotsRemaining { get; set; }

        public TestBag(uint slotsRemaining)
        {
            SlotsRemaining = slotsRemaining;
        }

        public void Save(Database.Character.CharacterContext context)
        {
        }

        public IItem GetItem(ulong guid) => throw new NotSupportedException();
        public IItem GetItem(uint bagIndex) => throw new NotSupportedException();
        public uint? GetFirstAvailableBagIndex() => throw new NotSupportedException();
        public uint? GetFirstAvailableBagIndex(ItemSlot slot) => throw new NotSupportedException();
        public void AddItem(IItem item, uint bagIndex) => throw new NotSupportedException();
        public void RemoveItem(IItem item) => throw new NotSupportedException();
        public void MoveItem(IItem item, uint bagIndex) => throw new NotSupportedException();
        public void SwapItem(IItem item, IItem item2) => throw new NotSupportedException();
        public void Resize(int capacityChange) => throw new NotSupportedException();
        public IItem[] CreateSnapshot() => throw new NotSupportedException();
        public void RestoreSnapshot(IItem[] snapshot) => throw new NotSupportedException();

        public IEnumerator<IItem> GetEnumerator()
        {
            yield break;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
