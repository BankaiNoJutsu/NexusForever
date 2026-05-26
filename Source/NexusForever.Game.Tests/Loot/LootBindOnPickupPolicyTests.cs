using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.World.Chat.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootBindOnPickupPolicyTests
{
    private const uint BindOnPickupItemId = 91003u;
    private const uint NormalItemId       = 91004u;

    [Fact]
    public void GiveLoot_BindOnPickupItem_FirstCollectSendsBindcheckWithoutDelivering()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateBindOnPickupItemInfo(), CreateNormalItemInfo());

        try
        {
            IPlayer player = CreatePlayer(slotsRemaining: 1u, out RecordingDispatchProxy<IGameSession> sessionProxy);
            var lootInstance = CreateLootInstance(player);
            LootInstanceItem lootItem = lootInstance.AddLootItem(BindOnPickupItemId, LootItemType.StaticItem, 1u);

            bool delivered = lootInstance.GiveLoot(player, lootItem.Id);

            Assert.False(delivered);
            Assert.False(lootItem.Delivered);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls =
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            RecordingDispatchProxy<IGameSession>.Invocation bindCall = Assert.Single(sessionCalls);
            var bindPacket = Assert.IsType<ServerLootBindOnPickup>(bindCall.Arguments[0]);
            Assert.Equal(99u, bindPacket.OwnerUnitId);
            Assert.Equal(lootItem.Id, bindPacket.LootUnitId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiveLoot_BindOnPickupItem_SecondCollectDeliversAndSoulbinds()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateBindOnPickupItemInfo(), CreateNormalItemInfo());

        try
        {
            IPlayer player = CreatePlayer(slotsRemaining: 1u, out RecordingDispatchProxy<IGameSession> sessionProxy);
            var lootInstance = CreateLootInstance(player);
            LootInstanceItem lootItem = lootInstance.AddLootItem(BindOnPickupItemId, LootItemType.StaticItem, 1u);

            Assert.False(lootInstance.GiveLoot(player, lootItem.Id));
            Assert.True(lootInstance.GiveLoot(player, lootItem.Id));

            Assert.True(lootItem.Delivered);
            IItem createdItem = Assert.Single(player.Inventory.Single(bag => bag.Location == InventoryLocation.Inventory));
            Assert.True(createdItem.Soulbound);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls =
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Collection(sessionCalls,
                call => Assert.IsType<ServerLootBindOnPickup>(call.Arguments[0]),
                call => Assert.IsType<ServerLootGrant>(call.Arguments[0]),
                call =>
                {
                    var chat = Assert.IsType<ServerChat>(call.Arguments[0]);
                    AssertStaticItemLootChat(chat, BindOnPickupItemId, "You receive [I].");
                });
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiveLoot_NonBindOnPickupItem_DeliversOnFirstCollect()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateBindOnPickupItemInfo(), CreateNormalItemInfo());

        try
        {
            IPlayer player = CreatePlayer(slotsRemaining: 1u, out RecordingDispatchProxy<IGameSession> sessionProxy);
            var lootInstance = CreateLootInstance(player);
            LootInstanceItem lootItem = lootInstance.AddLootItem(NormalItemId, LootItemType.StaticItem, 1u);

            Assert.True(lootInstance.GiveLoot(player, lootItem.Id));
            Assert.True(lootItem.Delivered);
            Assert.DoesNotContain(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)),
                call => call.Arguments[0] is ServerLootBindOnPickup);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static void AssertStaticItemLootChat(ServerChat chat, uint itemId, string expectedText)
    {
        Assert.Equal(ChatChannelType.Loot, chat.Channel.ChatChannelId);
        Assert.Equal(expectedText, chat.Text);

        ChatFormat format = Assert.Single(chat.Formats);
        Assert.Equal(ChatFormatType.ItemId, format.Type);
        Assert.Equal(12, format.StartIndex);
        Assert.Equal(15, format.StopIndex);

        var itemFormat = Assert.IsType<ChatFormatItemId>(format.Model);
        Assert.Equal(itemId, itemFormat.Item2Id);
    }

    private static LootInstance CreateLootInstance(IPlayer player)
    {
        return new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);
    }

    private static IPlayer CreatePlayer(uint slotsRemaining, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        var inventory = new TestInventory(slotsRemaining);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty("Guid", 4242u);

        return player;
    }

    private static IServiceProvider BuildProvider(params IItemInfo[] itemInfos)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var lootManager = new GlobalLootManager(groupStateManager);
        var itemManager = new ItemManager();
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        ImmutableDictionary<uint, IItemInfo> items = itemInfos
            .ToImmutableDictionary(info => info.Entry.Id);

        typeof(ItemManager)
            .GetField("item", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(itemManager, items);

        Item2Entry[] entries = itemInfos.Select(info => info.Entry).ToArray();
        GameTable<Item2Entry> itemTable = CreateGameTable(entries);
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), itemTable);

        return new ServiceCollection()
            .AddSingleton(lootManager)
            .AddSingleton(itemManager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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

    private static IItemInfo CreateBindOnPickupItemInfo()
    {
        return CreateItemInfo(BindOnPickupItemId, ItemBindFlags.BindOnPickup);
    }

    private static IItemInfo CreateNormalItemInfo()
    {
        return CreateItemInfo(NormalItemId, ItemBindFlags.None);
    }

    private static IItemInfo CreateItemInfo(uint itemId, ItemBindFlags bindFlags)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = itemId,
            MaxStackCount = 1u,
            BindFlags     = bindFlags
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.CanBindOnPickup), (bindFlags & ItemBindFlags.BindOnPickup) != 0);
        return itemInfo;
    }

    private sealed class TestInventory : IInventory
    {
        public TestBag InventoryBag { get; }

        public TestInventory(uint slotsRemaining)
        {
            InventoryBag = new TestBag(slotsRemaining);
        }

        public void Save(Database.Character.CharacterContext context) { }
        public void Update(double lastTick) { }
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
            IItemInfo info = ItemManager.Instance.GetItemInfo(itemId)
                ?? throw new InvalidOperationException($"Missing item info for {itemId}.");
            ItemCreate(location, info, count, reason, charges);
        }

        public void ItemCreate(InventoryLocation location, IItemInfo info, uint count, ItemUpdateReason reason = ItemUpdateReason.NoReason, uint charges = 0)
        {
            var item = new NexusForever.Game.Entity.Item(42ul, info, count, charges);
            InventoryBag.AddItem(item, 0u);
            if (InventoryBag.SlotsRemaining > 0u)
                InventoryBag.SlotsRemaining--;
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TestBag : IBag
    {
        private readonly List<IItem> items = [];

        public InventoryLocation Location => InventoryLocation.Inventory;
        public uint Slots => SlotsRemaining;
        public uint SlotsRemaining { get; set; }

        public TestBag(uint slotsRemaining)
        {
            SlotsRemaining = slotsRemaining;
        }

        public void Save(Database.Character.CharacterContext context) { }
        public IItem GetItem(ulong guid) => items.SingleOrDefault(item => item.Guid == guid);
        public IItem GetItem(uint bagIndex) => items.SingleOrDefault(item => item.BagIndex == bagIndex);
        public uint? GetFirstAvailableBagIndex() => items.Count < Slots ? (uint)items.Count : null;
        public uint? GetFirstAvailableBagIndex(ItemSlot slot) => GetFirstAvailableBagIndex();
        public void AddItem(IItem item, uint bagIndex)
        {
            item.BagIndex = bagIndex;
            items.Add(item);
        }
        public void RemoveItem(IItem item) => items.Remove(item);
        public void MoveItem(IItem item, uint bagIndex) => throw new NotSupportedException();
        public void SwapItem(IItem item, IItem item2) => throw new NotSupportedException();
        public void Resize(int capacityChange) => throw new NotSupportedException();
        public IItem[] CreateSnapshot() => throw new NotSupportedException();
        public void RestoreSnapshot(IItem[] snapshot) => throw new NotSupportedException();

        public IEnumerator<IItem> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
