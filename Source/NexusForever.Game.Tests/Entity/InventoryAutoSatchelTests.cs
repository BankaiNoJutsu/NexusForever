using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public class InventoryAutoSatchelTests
{
    private const uint ItemId = 9001;
    private const ushort MaterialId = 12;

    [Fact]
    public void CapacityQueryAndPreflightArePureAndAllowFullBagWhenSatchelFits()
    {
        var f = new Fixture(slots: 0);
        Assert.True(f.Satchel.TryGetAutoStoreCapacity(f.Info, out uint capacity));
        Assert.Equal(100u, capacity);
        Assert.True(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 100));
        Assert.True(f.CanLoot(100, out bool full));
        Assert.False(full);
        Assert.Empty(f.Satchel);
        Assert.Empty(f.Messages);
        Assert.Empty(f.ItemManagerProxy.GetInvocations("get_NextItemId"));
    }

    [Fact]
    public void NormalMaterialCreationUsesSatchelBeforeAnyBagSlot()
    {
        var f = new Fixture(slots: 0);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, ItemId, 70, ItemUpdateReason.Loot);
        Assert.Equal(70u, f.Stored);
        Assert.Empty(f.Bag);
        Assert.Single(f.Messages.OfType<ServerSupplySatchelUpdate>());
        Assert.Empty(f.Messages.OfType<ServerItemAdd>());
        Assert.Empty(f.Messages.OfType<ServerItemError>());
    }

    [Fact]
    public void ActualLootDeliveryIntoFullBagUsesSatchelAndGrantsExactlyOnce()
    {
        var f = new Fixture(slots: 0);
        var loot = new LootInstance(99, new Dictionary<ulong, uint> { [42] = 4242 }, LooterType.Player, LootEntityType.Creature, itemManager: f.ItemManager);
        LootInstanceItem item = loot.AddLootItem(ItemId, LootItemType.StaticItem, 20);
        Assert.True(loot.GiveLoot(f.Player, item.Id));
        Assert.Equal(20u, f.Stored);
        Assert.Empty(f.Bag);
        Assert.Single(f.Messages.OfType<ServerLootGrant>());
        Assert.False(loot.GiveLoot(f.Player, item.Id));
        Assert.Equal(20u, f.Stored);
        Assert.Single(f.Messages.OfType<ServerLootGrant>());
    }

    [Fact]
    public void FailedLootDeliveryRemainsRetryableWithoutPartialSatchelAddition()
    {
        var f = new Fixture(slots: 0, stored: 90);
        var loot = new LootInstance(99, new Dictionary<ulong, uint> { [42] = 4242 }, LooterType.Player, LootEntityType.Creature, itemManager: f.ItemManager);
        LootInstanceItem item = loot.AddLootItem(ItemId, LootItemType.StaticItem, 11);
        Assert.False(loot.GiveLoot(f.Player, item.Id));
        Assert.Equal(90u, f.Stored);
        Assert.Empty(f.Messages.OfType<ServerSupplySatchelUpdate>());
        Assert.Empty(f.Messages.OfType<ServerLootGrant>());
        f.Bags[InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, 1, f.ItemManager);
        Assert.True(loot.GiveLoot(f.Player, item.Id));
        Assert.Equal(100u, f.Stored);
        Assert.Equal(1u, Assert.Single(f.Bag).StackCount);
        Assert.Single(f.Messages.OfType<ServerLootGrant>());
    }

    [Theory]
    [InlineData((ushort)100)]
    [InlineData((ushort)105)]
    public void FullOrLegacyOverfullSatchelHasZeroAvailableCapacity(ushort stored)
    {
        var f = new Fixture(slots: 0, stored: stored);
        Assert.True(f.Satchel.TryGetAutoStoreCapacity(f.Info, out uint capacity));
        Assert.Equal(0u, capacity);
        Assert.False(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 1));
        Assert.Equal((uint)stored, f.Stored);
        Assert.Empty(f.Messages);
    }

    [Fact]
    public void PartialSatchelRemainderCreatesCorrectlySizedBagStacks()
    {
        var f = new Fixture(slots: 2, stored: 95);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 20, ItemUpdateReason.Crafting);
        Assert.Equal(100u, f.Stored);
        Assert.Equal(new uint[] { 10, 5 }, f.Bag.Select(i => i.StackCount));
        Assert.Equal(115u, f.Stored + f.BagAmount);
        Assert.Equal(2, f.Messages.OfType<ServerItemAdd>().Count());
    }

    [Fact]
    public void FullSatchelFallsBackToExistingStackAndNewBagSlot()
    {
        var f = new Fixture(slots: 2, stored: 100);
        f.AddExistingStack(5);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 15);
        Assert.Equal(100u, f.Stored);
        Assert.Equal(new uint[] { 10, 10 }, f.Bag.Select(i => i.StackCount));
        Assert.Empty(f.Messages.OfType<ServerSupplySatchelUpdate>());
    }

    [Fact]
    public void InsufficientCombinedCapacityDoesNotPartiallyStoreOrNotifySuccess()
    {
        var f = new Fixture(slots: 1, stored: 95);
        Assert.False(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 16));
        Assert.False(f.CanLoot(16, out bool full));
        Assert.True(full);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 16);
        Assert.Equal(95u, f.Stored);
        Assert.Empty(f.Bag);
        Assert.Equal(GenericError.ItemInventoryFull, Assert.Single(f.Messages.OfType<ServerItemError>()).ErrorCode);
        Assert.Empty(f.Messages.OfType<ServerSupplySatchelUpdate>());
        Assert.Empty(f.Messages.OfType<ServerItemAdd>());
        Assert.Empty(f.ItemManagerProxy.GetInvocations("get_NextItemId"));
    }

    [Fact]
    public void ExactCombinedBoundaryFitsWithoutLosingOrDuplicatingQuantity()
    {
        var f = new Fixture(slots: 1, stored: 95);
        Assert.True(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 15));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 15);
        Assert.Equal(100u, f.Stored);
        Assert.Equal(10u, Assert.Single(f.Bag).StackCount);
    }

    [Fact]
    public void RemainderCanUsePartialStackWithoutAnyFreeSlots()
    {
        var f = new Fixture(slots: 1, stored: 95);
        f.AddExistingStack(5);
        Assert.True(f.CanLoot(10, out _));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 10);
        Assert.Equal(100u, f.Stored);
        Assert.Equal(10u, Assert.Single(f.Bag).StackCount);
        Assert.Empty(f.Messages.OfType<ServerItemAdd>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrUnmappedMaterialTableKeepsBagRouting(bool missingTable)
    {
        var f = new Fixture(slots: 1, mapped: false, missingTable: missingTable);
        Assert.False(f.Satchel.TryGetAutoStoreCapacity(f.Info, out _));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4);
        Assert.Empty(f.Satchel);
        Assert.Equal(4u, Assert.Single(f.Bag).StackCount);
    }

    [Theory]
    [InlineData(511u, true)]
    [InlineData(512u, false)]
    [InlineData(65536u, false)]
    public void AutomaticRoutingHonorsSatchelNetworkMaterialBoundary(uint materialId, bool expected)
    {
        var f = new Fixture(slots: 1, materialId: materialId);
        Assert.Equal(expected, f.Satchel.TryGetAutoStoreCapacity(f.Info, out _));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4);
        Assert.Equal(expected ? 4u : 0u, f.Stored);
        Assert.Equal(expected ? 0u : 4u, f.BagAmount);
    }

    [Theory]
    [InlineData(InventoryLocation.PlayerBank)]
    [InlineData(InventoryLocation.RealmBank)]
    public void OtherInventoryScopesDoNotRedirectMaterials(InventoryLocation location)
    {
        var f = new Fixture(slots: 1);
        f.Inventory.ItemCreate(location, f.Info, 4);
        Assert.Empty(f.Satchel);
        Assert.Equal(4u, Assert.Single(f.Bags[location]).StackCount);
        Assert.Empty(f.Messages.OfType<ServerSupplySatchelUpdate>());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    public void SatchelWithdrawalRemainsBagOnlyAndRefundsAnyRemainder(uint existingAmount)
    {
        var f = new Fixture(slots: 1, stored: 20);
        f.AddExistingStack(existingAmount == 0 ? 10u : 9u);
        f.Satchel.MoveToInventory(MaterialId, 5);
        Assert.Equal(existingAmount == 0 ? 20u : 19u, f.Stored);
        Assert.Equal(10u, Assert.Single(f.Bag).StackCount);
        Assert.Empty(f.Messages.OfType<ServerItemError>());
    }

    [Fact]
    public void ResourceConversionCreationDoesNotLoopBackIntoSatchel()
    {
        var f = new Fixture(slots: 1, stored: 20);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4, ItemUpdateReason.ResourceConversion);
        Assert.Equal(20u, f.Stored);
        Assert.Equal(4u, Assert.Single(f.Bag).StackCount);
        Assert.Empty(f.Messages.OfType<ServerSupplySatchelUpdate>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonstackableOrEquippableItemsRetainBagRepresentation(bool equippable)
    {
        var f = new Fixture(slots: 1);
        var infoProxy = (RecordingDispatchProxy<IItemInfo>)(object)f.Info;
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), equippable);
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), equippable);
        Assert.False(f.Satchel.TryGetAutoStoreCapacity(f.Info, out _));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 1);
        Assert.Empty(f.Satchel);
        Assert.Single(f.Bag);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void InstancePropertiesAndLoadingDisableAutomaticRouting(int reason)
    {
        var f = new Fixture(slots: 1);
        uint charges = 0;
        if (reason == 1) f.Entry.MaxCharges = 5;
        if (reason == 2) f.Entry.ExpirationTimeMinutes = 30;
        if (reason == 3) charges = 3;
        if (reason == 4) f.PlayerProxy.SetProperty(nameof(IPlayer.IsLoading), true);
        Assert.False(f.Satchel.TryGetAutoStoreCapacity(f.Info, out _, charges));
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4, charges: charges);
        IItem item = Assert.Single(f.Bag);
        Assert.Equal(4u, item.StackCount);
        Assert.Equal(reason == 1 ? 5u : charges, item.Charges);
        Assert.Equal(reason == 2 ? 1800u : 0u, item.ExpirationTimeLeft);
        Assert.Empty(f.Satchel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActiveCollectItemMaterialsStayInBagWhileInactiveQuestDoesNotBlock(bool active)
    {
        var f = new Fixture(slots: 1);
        f.SetQuest(QuestObjectiveType.CollectItem, ItemId, active);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4);
        Assert.Equal(active ? 0u : 4u, f.Stored);
        Assert.Equal(active ? 4u : 0u, f.BagAmount);
    }

    [Theory]
    [InlineData(QuestObjectiveType.CollectItem, ItemId + 1)]
    [InlineData(QuestObjectiveType.VirtualCollect, ItemId)]
    public void UnrelatedActiveQuestDoesNotDisableMaterialRouting(QuestObjectiveType type, uint data)
    {
        var f = new Fixture(slots: 1);
        f.SetQuest(type, data, active: true);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4);
        Assert.Equal(4u, f.Stored);
        Assert.Empty(f.Bag);
    }

    [Fact]
    public void ExistingRewardBasedCapacityRemainsAuthoritative()
    {
        var f = new Fixture(slots: 1, stored: 195, maximum: 200);
        Assert.True(f.Satchel.TryGetAutoStoreCapacity(f.Info, out uint capacity));
        Assert.Equal(5u, capacity);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 15);
        Assert.Equal(200u, f.Stored);
        Assert.Equal(10u, Assert.Single(f.Bag).StackCount);
    }

    [Fact]
    public void ZeroQuantityDoesNotCreateMaterialOrBagItem()
    {
        var f = new Fixture(slots: 1);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 0);
        Assert.Empty(f.Satchel);
        Assert.Empty(f.Bag);
        Assert.Empty(f.Messages);
    }

    private sealed class Fixture
    {
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public RecordingDispatchProxy<IGameSession> SessionProxy { get; }
        public RecordingDispatchProxy<IItemManager> ItemManagerProxy { get; }
        public IItemManager ItemManager { get; }
        public IItemInfo Info { get; }
        public Item2Entry Entry { get; } = new() { Id = ItemId, MaxStackCount = 10 };
        public Inventory Inventory { get; } = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        public SupplySatchelManager Satchel { get; }
        public Dictionary<InventoryLocation, IBag> Bags { get; }
        public IBag Bag => Bags[InventoryLocation.Inventory];
        public uint Stored => (uint)Satchel.Sum(m => m.Amount);
        public uint BagAmount => (uint)Bag.Sum(i => i.StackCount);
        public IEnumerable<object> Messages => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(i => i.Arguments[0]);

        public Fixture(uint slots, ushort stored = 0, bool mapped = true, bool missingTable = false, uint materialId = MaterialId, uint maximum = 100)
        {
            Player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Guid), 4242u);
            pp.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 1, Id = 42 });
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp);
            SessionProxy = sp;
            pp.SetProperty(nameof(IPlayer.Session), session);
            Info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
            ip.SetProperty(nameof(IItemInfo.Id), ItemId);
            ip.SetProperty(nameof(IItemInfo.Entry), Entry);
            ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
            ItemManager = RecordingDispatchProxy<IItemManager>.Create(out var imp);
            ItemManagerProxy = imp;
            ulong nextId = 1;
            imp.SetMethodReturnFactory("get_NextItemId", () => nextId++);
            imp.SetMethodReturn(nameof(IItemManager.GetItemInfo), Info);
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            if (!missingTable)
            {
                var table = (GameTable<TradeskillMaterialEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<TradeskillMaterialEntry>));
                TradeskillMaterialEntry[] entries = mapped ? [new() { Id = materialId, Item2IdStatRevolution = ItemId }] : [];
                typeof(GameTable<TradeskillMaterialEntry>).GetProperty(nameof(GameTable<TradeskillMaterialEntry>.Entries))!.SetValue(table, entries);
                typeof(GameTable<TradeskillMaterialEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = materialId + 1 });
                int[] lookup = Enumerable.Repeat(-1, (int)materialId + 1).ToArray();
                if (mapped) lookup[materialId] = 0;
                typeof(GameTable<TradeskillMaterialEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
                tp.SetProperty(nameof(IGameTableManager.TradeskillMaterial), table);
            }
            if (maximum != 100)
            {
                var account = RecordingDispatchProxy<IAccount>.Create(out var ap);
                var rewards = RecordingDispatchProxy<IRewardPropertyManager>.Create(out var rp);
                var reward = RecordingDispatchProxy<IRewardProperty>.Create(out var rvp);
                rvp.SetMethodReturn(nameof(IRewardProperty.GetValue), (float)maximum);
                rp.SetMethodReturn(nameof(IRewardPropertyManager.GetRewardProperty), reward);
                ap.SetProperty(nameof(IAccount.RewardPropertyManager), rewards);
                pp.SetProperty(nameof(IPlayer.Account), account);
            }
            var model = new CharacterModel();
            if (stored != 0) model.TradeskillMaterials.Add(new CharacterTradeskillMaterialModel { Id = 42, MaterialId = (ushort)materialId, Amount = stored });
            Satchel = new SupplySatchelManager(Player, model, tables);
            pp.SetProperty(nameof(IPlayer.SupplySatchelManager), Satchel);
            Bags = new Dictionary<InventoryLocation, IBag>
            {
                [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, slots, ItemManager),
                [InventoryLocation.PlayerBank] = new Bag(InventoryLocation.PlayerBank, 1, ItemManager),
                [InventoryLocation.RealmBank] = new Bag(InventoryLocation.RealmBank, 1, ItemManager)
            };
            SetField("bags", Bags);
            SetField("deletedItems", new List<IItem>());
            SetField("player", Player);
            SetField("characterId", 42ul);
            SetField("itemManager", ItemManager);
            SetField("gameTableManager", tables);
            pp.SetProperty(nameof(IPlayer.Inventory), Inventory);
        }

        public bool CanLoot(uint amount, out bool full) => LootInstanceItem.CanDeliverStaticItem(Player, ItemId, amount, ItemManager, out full);
        public void AddExistingStack(uint amount) => Bag.AddItem(new NexusForever.Game.Entity.Item(42, Info, amount, itemManager: ItemManager), 0);
        private void SetField(string field, object value) => typeof(Inventory).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Inventory, value);

        public void SetQuest(QuestObjectiveType type, uint data, bool active)
        {
            var questManager = RecordingDispatchProxy<IQuestManager>.Create(out var qp);
            var quest = RecordingDispatchProxy<IQuest>.Create(out var q);
            var objective = RecordingDispatchProxy<IQuestObjective>.Create(out var op);
            var info = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out var ip);
            ip.SetProperty(nameof(IQuestObjectiveInfo.Type), type);
            ip.SetProperty(nameof(IQuestObjectiveInfo.Entry), new QuestObjectiveEntry { Data = data });
            op.SetProperty(nameof(IQuestObjective.ObjectiveInfo), info);
            q.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IQuestObjective>)new[] { objective }).GetEnumerator());
            qp.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), active ? new[] { quest } : Array.Empty<IQuest>());
            PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        }
    }
}
