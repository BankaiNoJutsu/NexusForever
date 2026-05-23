using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using ItemLocation = NexusForever.Network.World.Message.Model.Shared.ItemLocation;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootInstanceResolutionTests
{
    private const uint StaticItemId = 91002u;
    private const ushort RealmId = (ushort)1;

    [Fact]
    public void RollWinnerOffline_RemainsLootableForWinnerWhenTheyReturn()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateItemInfo());

        try
        {
            TestPlayer winner = CreatePlayer(characterId: 42ul, guid: 4242u, accountId: 1001u, slotsRemaining: 1u);
            TestPlayer loser = CreatePlayer(characterId: 43ul, guid: 4343u, accountId: 1002u, slotsRemaining: 1u);

            PlayerManager.Instance.AddPlayer(winner.Player);
            PlayerManager.Instance.AddPlayer(loser.Player);

            LootInstance lootInstance = CreateLootInstance(winner, loser);
            LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
            lootItem.ConfigureRoll([winner.Identity, loser.Identity]);

            Assert.True(lootInstance.RollLoot(winner.Player, lootItem.Id, LootRollAction.Need));

            PlayerManager.Instance.RemovePlayer(winner.Player);
            Assert.True(lootInstance.RollLoot(loser.Player, lootItem.Id, LootRollAction.Pass));

            Assert.False(lootItem.Delivered);
            Assert.True(lootItem.CanLoot(winner.CharacterId));
            Assert.False(lootItem.CanLoot(loser.CharacterId));

            PlayerManager.Instance.AddPlayer(winner.Player);

            RecordingDispatchProxy<IGameSession>.Invocation notifyCall = CaptureSingleSessionCall(
                winner.SessionProxy,
                () => lootInstance.SendLootNotify(winner.Player));

            ServerLootNotify notify = Assert.IsType<ServerLootNotify>(notifyCall.Arguments[0]);
            NexusForever.Network.World.Message.Model.Loot.LootItem networkItem = Assert.Single(notify.LootItems);
            Assert.True(networkItem.CanLoot);
            Assert.False(networkItem.RequiresRoll);
            Assert.False(networkItem.OnlyMasterLootable);

            Assert.True(lootInstance.GiveLoot(winner.Player, lootItem.Id));
            Assert.True(lootItem.Delivered);
            Assert.Single(winner.Inventory.CreatedItems);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AssignMasterLoot_DeferredDeliveryLeavesResolvedLootForAssignee()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateItemInfo());

        try
        {
            TestPlayer master = CreatePlayer(characterId: 52ul, guid: 5252u, accountId: 2001u, slotsRemaining: 1u);
            TestPlayer assignee = CreatePlayer(characterId: 53ul, guid: 5353u, accountId: 2002u, slotsRemaining: 0u);

            PlayerManager.Instance.AddPlayer(master.Player);
            PlayerManager.Instance.AddPlayer(assignee.Player);

            LootInstance lootInstance = CreateLootInstance(master, assignee);
            LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
            lootItem.ConfigureMaster(
                [master.Identity],
                [master.Identity, assignee.Identity],
                [master.Identity, assignee.Identity]);

            Assert.True(lootInstance.AssignMasterLoot(master.Player, lootItem.Id, assignee.Identity));
            Assert.False(lootItem.Delivered);
            Assert.True(lootItem.CanLoot(assignee.CharacterId));
            Assert.False(lootItem.CanLoot(master.CharacterId));

            RecordingDispatchProxy<IGameSession>.Invocation notifyCall = CaptureSingleSessionCall(
                assignee.SessionProxy,
                () => lootInstance.SendLootNotify(assignee.Player));

            ServerLootNotify notify = Assert.IsType<ServerLootNotify>(notifyCall.Arguments[0]);
            NexusForever.Network.World.Message.Model.Loot.LootItem networkItem = Assert.Single(notify.LootItems);
            Assert.True(networkItem.CanLoot);
            Assert.False(networkItem.RequiresRoll);
            Assert.False(networkItem.OnlyMasterLootable);

            assignee.Inventory.InventoryBag.SlotsRemaining = 1u;

            Assert.True(lootInstance.GiveLoot(assignee.Player, lootItem.Id));
            Assert.True(lootItem.Delivered);
            Assert.Single(assignee.Inventory.CreatedItems);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static LootInstance CreateLootInstance(params TestPlayer[] players)
    {
        return new LootInstance(
            ownerUnitId: 99u,
            looterIds: players.ToDictionary(player => player.CharacterId, player => player.Guid),
            looterType: LooterType.Group,
            lootEntityType: LootEntityType.Creature);
    }

    private static RecordingDispatchProxy<IGameSession>.Invocation CaptureSingleSessionCall(
        RecordingDispatchProxy<IGameSession> sessionProxy,
        Action action)
    {
        int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;
        action();

        return Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Skip(beforeCount));
    }

    private static IServiceProvider BuildProvider(IItemInfo itemInfo)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out _);

        var lootManager = new GlobalLootManager(groupStateManager);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, characterManager);
        var itemManager = new ItemManager();
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        var realmContext = (RealmContext)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));

        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(StaticItemId, itemInfo));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id = StaticItemId
        }));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), RealmId);

        return new ServiceCollection()
            .AddSingleton(lootManager)
            .AddSingleton(playerManager)
            .AddSingleton(itemManager)
            .AddSingleton(gameTableManager)
            .AddSingleton(realmContext)
            .BuildServiceProvider();
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = StaticItemId,
            MaxStackCount = 1u
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        return itemInfo;
    }

    private static TestPlayer CreatePlayer(ulong characterId, uint guid, uint accountId, uint slotsRemaining)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);

        Identity identity = new()
        {
            Id      = characterId,
            RealmId = RealmId
        };

        var inventory = new TestInventory(slotsRemaining);

        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty("Guid", guid);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);

        return new TestPlayer(player, sessionProxy, inventory, identity, guid);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
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

    private sealed record TestPlayer(
        IPlayer Player,
        RecordingDispatchProxy<IGameSession> SessionProxy,
        TestInventory Inventory,
        Identity Identity,
        uint Guid)
    {
        public ulong CharacterId => Identity.Id;
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
