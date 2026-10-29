using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Group;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Loot;

public class UpstreamLootReconciliationTests
{
    [Fact]
    public void ContributorsGenerateOneSharedDropPerGroupAndIndependentSoloLoot()
    {
        var f = new Fixture();
        var alice = f.Player(1, group: 77);
        var bob = f.Player(2, group: 77);
        var solo = f.Player(3, group: 0);
        f.Group(alice, bob);
        f.AddTable();

        f.Manager.DistributeLoot([alice, bob, alice, solo], f.Corpse());

        Assert.Equal(2, f.Instances.Count);
        Assert.All(f.Instances, i => Assert.Equal(1u, Assert.Single(i).Amount));
    }

    [Fact]
    public void ClassConditionKeepsEligibleGroupMemberEvenWhenTaggerIsIneligible()
    {
        var f = new Fixture();
        var alice = f.Player(1, group: 77, playerClass: 1);
        var bob = f.Player(2, group: 77, playerClass: 2);
        f.Group(alice, bob);
        f.AddTable(LootConditionType.IsClass, 2);

        f.Manager.DistributeLoot([alice, bob], f.Corpse());

        var item = Assert.Single(Assert.Single(f.Instances));
        Assert.True(item.CanLoot(bob.CharacterId));
        Assert.False(item.CanLoot(alice.CharacterId));
        Assert.False(item.RequiresRoll);
    }

    [Fact]
    public void ConditionalRollDoesNotAdmitOtherGroupMembers()
    {
        var f = new Fixture();
        var alice = f.Player(1, group: 77, playerClass: 1);
        var bob = f.Player(2, group: 77, playerClass: 2);
        var carol = f.Player(3, group: 77, playerClass: 2);
        f.Group(alice, bob, carol);
        f.AddTable(LootConditionType.IsClass, 2);
        f.Manager.DistributeLoot([alice, bob, carol], f.Corpse());
        var instance = Assert.Single(f.Instances);
        var item = Assert.Single(instance);

        Assert.True(item.RequiresRoll);
        Assert.False(instance.RollLoot(alice, item.Id, LootRollAction.Need));
        Assert.True(instance.RollLoot(bob, item.Id, LootRollAction.Pass));
        Assert.True(instance.RollLoot(carol, item.Id, LootRollAction.Pass));
        Assert.True(item.Delivered);
    }

    [Fact]
    public void IdenticalRewardsWithDifferentEligibilityDoNotMergeQuantities()
    {
        var f = new Fixture();
        var alice = f.Player(1);
        var bob = f.Player(2);
        var instance = new LootInstance(900, new() { [1] = 11, [2] = 12 }, LooterType.Group, LootEntityType.Creature);
        var first = instance.AddLootItem(10, LootItemType.StaticItem, 1, [alice.Identity]);
        var second = instance.AddLootItem(10, LootItemType.StaticItem, 3, [bob.Identity]);

        Assert.Same(first, instance.AddLootItem(10, LootItemType.StaticItem, 2, [alice.Identity]));
        Assert.Equal(2, instance.Count());
        Assert.Equal(3u, first.Amount);
        Assert.Equal(3u, second.Amount);
        Assert.False(first.CanLoot(2));
        Assert.False(second.CanLoot(1));
    }

    [Fact]
    public void NonEquipmentRewardsDoNotRequireEquipmentRollsOrMasterAssignment()
    {
        var f = new Fixture();
        var alice = f.Player(1, group: 77);
        var bob = f.Player(2, group: 77);
        f.Group([alice, bob], LootRule.Master);
        f.AddTable(type: LootItemType.Cash, staticId: (uint)CurrencyType.Credits);

        f.Manager.DistributeLoot([alice, bob], f.Corpse());

        var item = Assert.Single(Assert.Single(f.Instances));
        Assert.False(item.RequiresRoll);
        Assert.False(item.OnlyMasterLootable);
        Assert.True(item.CanLoot(1));
        Assert.True(item.CanLoot(2));
    }

    [Fact]
    public void MapScopedRemovalDoesNotDeleteAnotherMapsSameOwnerGuid()
    {
        var f = new Fixture();
        var alice = f.Player(1);
        var mapB = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var bob = f.Player(2, map: mapB);
        f.AddTable();
        f.Manager.DropLoot(alice, f.Corpse());
        f.Manager.DropLoot(bob, f.Corpse(mapB));

        f.Manager.RemoveLootForOwner(900, f.Map, sendRemove: false);

        Assert.Same(mapB, Assert.Single(f.Instances).OwnerMap);
        Assert.False(f.Manager.TryGetLootRuntimeSnapshot(alice, 900, out _));
        Assert.True(f.Manager.TryGetLootRuntimeSnapshot(bob, 900, out _));
    }

    [Fact]
    public void EmptyGenerationEligibilityDoesNotBecomeUnrestricted()
    {
        var f = new Fixture();
        var player = f.Player(1);
        var instance = new LootInstance(900, new() { [1] = 11 }, LooterType.Player, LootEntityType.Creature);
        var item = instance.AddLootItem(10, LootItemType.StaticItem, 1, []);

        Assert.False(item.CanLoot(player.CharacterId));
        Assert.False(item.IsEligible(player.Identity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectDropRejectsDifferentMapAndPlayerDeaths(bool playerDeath)
    {
        var f = new Fixture();
        var player = f.Player(1);
        f.AddTable();
        IWorldEntity corpse;
        if (playerDeath)
        {
            var victim = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            proxy.SetProperty(nameof(IPlayer.Map), f.Map);
            proxy.SetProperty(nameof(IPlayer.CreatureId), 100u);
            corpse = victim;
        }
        else
            corpse = f.Corpse(RecordingDispatchProxy<IBaseMap>.Create(out _));

        Assert.False(f.Manager.DropLoot(player, corpse));
        Assert.Empty(f.Instances);
    }

    [Fact]
    public void TrackedLooterOnAnotherMapCannotCollectRollOrAssign()
    {
        var f = new Fixture();
        var alice = f.Player(1);
        f.AddTable();
        f.Manager.DropLoot(alice, f.Corpse());
        var instance = Assert.Single(f.Instances);
        var item = Assert.Single(instance);
        var elsewhere = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var traveller = f.Player(1, map: elsewhere);

        Assert.False(instance.GiveLoot(traveller, item.Id));
        Assert.False(instance.RollLoot(traveller, item.Id, LootRollAction.Need));
        Assert.False(instance.AssignMasterLoot(traveller, item.Id, traveller.Identity));
        Assert.False(item.Delivered);
    }

    [Theory]
    [InlineData(LootConditionType.QuestIsComplete, true, true)]
    [InlineData(LootConditionType.QuestIsComplete, false, false)]
    [InlineData(LootConditionType.QuestNotComplete, true, false)]
    [InlineData(LootConditionType.QuestNotComplete, false, true)]
    [InlineData((LootConditionType)999, false, false)]
    public void QuestAndUnknownConditionsDoNotLeakRewards(LootConditionType condition, bool completed, bool expected)
    {
        var f = new Fixture();
        var player = f.Player(1, completed: completed);
        var group = MakeTable(condition, 42);

        Assert.Equal(expected, group.GenerateLootDrops(player).Any());
    }

    [Theory]
    [InlineData(0f, 100f, false)]
    [InlineData(100f, 0f, false)]
    [InlineData(100f, 100f, true)]
    public void ProbabilityBoundariesPreserveZeroAndGuaranteedDrops(float groupChance, float itemChance, bool expected)
    {
        var f = new Fixture();
        var group = MakeTable(groupChance: groupChance, itemChance: itemChance);
        Assert.Equal(expected, group.GenerateLootDrops(f.Player(1)).Any());
    }

    [Fact]
    public void ItemQuantitySupportsInclusiveUnsignedRange()
    {
        var item = new LootItem(10, LootItemType.StaticItem, 100, uint.MaxValue - 1, uint.MaxValue);
        Assert.True(item.TryGetDrop(out uint count));
        Assert.InRange(count, uint.MaxValue - 1, uint.MaxValue);
    }

    private static LootGroup MakeTable(LootConditionType condition = LootConditionType.None, uint value = 0,
        LootItemType type = LootItemType.StaticItem, uint staticId = 10, float groupChance = 100, float itemChance = 100) => new(new LootGroupModel
        {
            Id = 1, Probability = groupChance, ConditionType = (uint)condition, Condition = value,
            Item = [new LootItemModel { Id = 1, Type = (uint)type, StaticId = staticId, Probability = itemChance, MinCount = 1, MaxCount = 1 }]
        }, loadChildren: false);

    private sealed class Fixture
    {
        public IBaseMap Map { get; } = RecordingDispatchProxy<IBaseMap>.Create(out _);
        private readonly Dictionary<Identity, IPlayer> players = [];
        public GroupStateManager Groups { get; } = new();
        public GlobalLootManager Manager { get; }
        public List<LootInstance> Instances => (List<LootInstance>)typeof(GlobalLootManager).GetField("lootInstances", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Manager);

        public Fixture()
        {
            IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out var proxy);
            proxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), a => a[0] is Identity id ? players.GetValueOrDefault(id) : players.Values.FirstOrDefault(p => p.CharacterId == (ulong)a[0]));
            IItemManager items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
            IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
            infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, ItemQualityId = 3 });
            itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
            IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            tableProxy.SetProperty(nameof(IGameTableManager.Creature2), Table(new Creature2Entry { Id = 100 }));
            Manager = new GlobalLootManager(Groups, playerManager, items, gameTableManager: tables);
        }

        public IPlayer Player(ulong id, ulong group = 0, uint playerClass = 1, IBaseMap map = null, bool completed = false)
        {
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            var identity = new Identity { Id = id, RealmId = 1 };
            proxy.SetProperty(nameof(IPlayer.Identity), identity);
            proxy.SetProperty(nameof(IPlayer.CharacterId), id);
            proxy.SetProperty(nameof(IPlayer.Guid), (uint)id + 10);
            proxy.SetProperty(nameof(IPlayer.Map), map ?? Map);
            proxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
            proxy.SetProperty(nameof(IPlayer.Class), (Class)playerClass);
            proxy.SetProperty(nameof(IPlayer.GroupAssociation), group);
            proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            proxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
            IQuestManager quests = RecordingDispatchProxy<IQuestManager>.Create(out var questProxy);
            questProxy.SetMethodReturn(nameof(IQuestManager.GetQuestState), completed ? QuestState.Completed : (QuestState)0);
            proxy.SetProperty(nameof(IPlayer.QuestManager), quests);
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.CurrencyManager), RecordingDispatchProxy<IAccountCurrencyManager>.Create(out _));
            proxy.SetProperty(nameof(IPlayer.Account), account);
            players[identity] = player;
            return player;
        }

        public void Group(params IPlayer[] members) => Group(members, LootRule.NeedBeforeGreed);
        public void Group(IPlayer[] members, LootRule rule) => Groups.UpdateGroup(new GroupLootState
        {
            GroupId = 77, Leader = members[0].Identity, NormalRule = rule, ThresholdRule = rule,
            Members = members.Select((p, i) => new GroupLootMember { Identity = p.Identity, GroupIndex = (uint)i }).ToList()
        });

        public IWorldEntity Corpse(IBaseMap map = null)
        {
            IWorldEntity corpse = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
            proxy.SetProperty(nameof(IWorldEntity.Map), map ?? Map);
            proxy.SetProperty(nameof(IWorldEntity.Position), Vector3.Zero);
            proxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
            proxy.SetProperty(nameof(IWorldEntity.CreatureId), 100u);
            return corpse;
        }

        public void AddTable(LootConditionType condition = LootConditionType.None, uint value = 0, LootItemType type = LootItemType.StaticItem, uint staticId = 10)
        {
            var dictionary = (Dictionary<uint, List<LootGroup>>)typeof(GlobalLootManager).GetField("creatureLoot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Manager);
            dictionary[100] = [MakeTable(condition, value, type, staticId)];
        }

        private static GameTable<T> Table<T>(T entry) where T : class, new()
        {
            var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
            uint id = (uint)typeof(T).GetField("Id")!.GetValue(entry);
            void Set(string field, object value) => typeof(GameTable<T>).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, value);
            Set("<Entries>k__BackingField", new[] { entry });
            Set("header", new GameTableHeader { MaxId = id + 1 });
            int[] lookup = Enumerable.Repeat(-1, (int)id + 1).ToArray();
            lookup[id] = 0;
            Set("lookup", lookup);
            return table;
        }
    }
}
