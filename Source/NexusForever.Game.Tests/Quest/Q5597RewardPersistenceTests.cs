using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.Session;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class Q5597RewardPersistenceTests
{
    private const ushort QuestId = 5597;
    private const ushort AchievementId = 4134;
    private const uint Receiver = 24187;
    private const uint PushedItem = 7001;
    private static readonly uint[] RewardIds = [29664, 81917];

    [Theory]
    [InlineData(1u, false)]
    [InlineData(2u, false)]
    [InlineData(2u, true)]
    public async Task TurnInAndReloadPreserveBothFixedItemsAndChecklistOrLeaveQuestAchieved(uint slots, bool reclaim)
    {
        using var f = new Fixture(null);
        using (var seed = f.Context())
        {
            seed.CharacterQuest.Add(new() { Id = 42, QuestId = QuestId, State = (byte)QuestState.Achieved });
            if (reclaim)
                seed.Item.Add(new() { Id = 99, OwnerId = 42, ItemId = PushedItem,
                    Location = (ushort)InventoryLocation.Inventory, BagIndex = 0, StackCount = 1, Durability = 1 });
            await seed.SaveChangesAsync();
        }
        ConfigureQuest(f, reclaim);
        var items = CreateItems();

        using (var actor = LoadActor(f, items, slots))
        {
            await actor.Load();
            actor.Quests.QuestComplete(QuestId, 0, false);
            AssertState(actor);
            await CharacterAtomicSave.Execute(() => f.Context(), 42, actor.Player.Save);
        }

        using (var read = f.Context())
        {
            Assert.Equal(slots == 1 ? QuestState.Achieved : QuestState.Completed, (QuestState)read.CharacterQuest.Single().State);
            Assert.Equal(slots == 1 ? [] : RewardIds, read.Item.OrderBy(row => row.ItemId).Select(row => row.ItemId).ToArray());
            if (slots == 1) Assert.Empty(read.CharacterAchievement);
            else
            {
                var checklist = Assert.Single(read.CharacterAchievement);
                Assert.Equal(2u, checklist.ProgressState);
                Assert.NotNull(checklist.DateCompleted);
            }
        }
        using var loaded = LoadActor(f, items, slots);
        await loaded.Load();
        AssertState(loaded);
        if (slots == 2)
        {
            Assert.Throws<QuestException>(() => loaded.Quests.QuestComplete(QuestId, 0, false));
            AssertState(loaded);
        }

        void AssertState(Actor actor)
        {
            Assert.Equal(slots == 1 ? QuestState.Achieved : QuestState.Completed, actor.Quests.GetQuestState(QuestId));
            Assert.Equal(slots == 1 ? [] : RewardIds, actor.Player.Inventory
                .Where(bag => bag.Location == InventoryLocation.Inventory).SelectMany(bag => bag).Select(item => item.Info.Id).Order().ToArray());
            var standing = Assert.IsType<CharacterAchievementManager>(actor.Player.AchievementManager).CaptureExperienceAchievements();
            if (slots == 1) Assert.Empty(standing);
            else
            {
                var checklist = Assert.Single(standing);
                Assert.Equal(2u, checklist.Progress);
                Assert.NotNull(checklist.Completed);
            }
        }
    }

    private static void ConfigureQuest(Fixture f, bool reclaim, ushort questId = QuestId)
    {
        var info = (RecordingDispatchProxy<IQuestInfo>)(object)f.Info;
        info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = questId, PushedItemIds = reclaim ? [PushedItem] : [], PushedItemCounts = reclaim ? [1u] : []
        });
        info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList<IQuestObjectiveInfo>.Empty);
        info.SetProperty(nameof(IQuestInfo.Rewards), new[] { Reward(3000, 81917), Reward(5236, 29664) }.ToImmutableDictionary(row => row.Id));
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)f.Global)
            .SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), ImmutableList.Create(Receiver));

        Quest2RewardEntry Reward(uint id, uint item) => new()
        {
            Id = id, Quest2Id = questId, Quest2RewardTypeId = (uint)QuestRewardType.Item, ObjectId = item, ObjectAmount = 1
        };
    }

    private static IItemManager CreateItems()
    {
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        var definitions = RewardIds.Append(PushedItem).ToDictionary(id => id, id =>
        {
            var info = RecordingDispatchProxy<IItemInfo>.Create(out var proxy);
            proxy.SetProperty(nameof(IItemInfo.Id), id);
            proxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = id, MaxStackCount = 1 });
            return info;
        });
        ulong nextId = 100;
        itemManager.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => nextId++);
        itemManager.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => definitions[(uint)args[0]]);
        return items;
    }

    private static Actor LoadActor(Fixture f, IItemManager items, uint slots,
        CharacterModel model = null, IGameSession session = null, IAccount account = null, AuthDatabase auth = null,
        ushort questId = QuestId, CharacterDatabase characterDatabase = null)
    {
        if (model == null)
        {
            using var read = f.Context();
            model = read.Character.AsNoTracking().Include(row => row.Quest).Include(row => row.Item)
                .Include(row => row.Achievement).Single();
        }
        var actor = new Actor(f, actorModel: model, actorAccount: account, actorSession: session, authDatabase: auth,
            characterDatabase: characterDatabase,
            actorEvents: session?.Events, inventoryFactory: (player, loaded) =>
        {
            var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
            var bag = new Bag(InventoryLocation.Inventory, slots, items);
            foreach (var row in loaded.Item)
                bag.AddItem(new GameItem(row, items, f.Source.Tables), row.BagIndex);
            SetField(inventory, "characterId", player.CharacterId);
            SetField(inventory, "player", player);
            SetField(inventory, "itemManager", items);
            SetField(inventory, "gameTableManager", f.Source.Tables);
            SetField(inventory, "bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
            SetField(inventory, "deletedItems", new List<IItem>());
            return inventory;
        });
        var mondo = (NonPlayerEntity)RuntimeHelpers.GetUninitializedObject(typeof(NonPlayerEntity));
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.CreatureEntry))!.SetValue(mondo, new Creature2Entry { Id = Receiver });
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(actor.Player, new Dictionary<uint, IGridEntity> { [1] = mondo });

        var bloodstone = new AchievementInfo(new AchievementEntry
        {
            Id = AchievementId, AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist,
            PrerequisiteId = 18, PrerequisiteIdServer = 18
        });
        bloodstone.ChecklistEntries.Add(new AchievementChecklistEntry
            { Id = 5495, AchievementId = AchievementId, Bit = 1, ObjectId = questId });
        var registry = RecordingDispatchProxy<IGlobalAchievementManager>.Create(out var achievementManager);
        achievementManager.SetMethodReturn(nameof(IGlobalAchievementManager.GetAchievement), bloodstone);
        achievementManager.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements),
            args => (AchievementType)args[0] == AchievementType.QuestCompleteChecklist ? new[] { bloodstone } : Array.Empty<AchievementInfo>());
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var prerequisiteManager);
        prerequisiteManager.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] == 18);
        actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, model,
            globalAchievementManager: registry, prerequisiteManager: prerequisites));
        return actor;
    }

    private static void SetField(Inventory inventory, string name, object value)
        => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
}
