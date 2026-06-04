using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Loot;

public class ItemContainerLootTests
{
    [Fact]
    public void PromotedItemContainerGroupDropsExactlyOneWeightedItem()
    {
        LootGroup group = CreateItemContainerLootGroup(
            new LootItemModel
            {
                Id          = 110000001337,
                Type        = (uint)LootItemType.StaticItem,
                StaticId    = 9001,
                Probability = 100f,
                MinCount    = 1u,
                MaxCount    = 1u
            },
            new LootItemModel
            {
                Id          = 110000001337,
                Type        = (uint)LootItemType.StaticItem,
                StaticId    = 9002,
                Probability = 0f,
                MinCount    = 1u,
                MaxCount    = 1u
            });

        List<(LootItem Item, uint Count)> drops = group.GenerateLootDrops(null).ToList();

        Assert.Single(drops);
        Assert.Equal(9001u, drops[0].Item.StaticId);
        Assert.Equal(1u, drops[0].Count);
    }

    [Fact]
    public void PromotedItemContainerGroupHonorsOneDropPolicyWhenSeveralItemsRoll()
    {
        LootGroup group = CreateItemContainerLootGroup(
            new LootItemModel
            {
                Id          = 110000001338,
                Type        = (uint)LootItemType.StaticItem,
                StaticId    = 9101,
                Probability = 100f,
                MinCount    = 1u,
                MaxCount    = 1u
            },
            new LootItemModel
            {
                Id          = 110000001338,
                Type        = (uint)LootItemType.StaticItem,
                StaticId    = 9102,
                Probability = 100f,
                MinCount    = 1u,
                MaxCount    = 1u
            });

        for (int i = 0; i < 50; i++)
        {
            List<(LootItem Item, uint Count)> drops = group.GenerateLootDrops(null).ToList();

            Assert.Single(drops);
            Assert.Equal(1u, drops[0].Count);
        }
    }

    [Fact]
    public void QuestVirtualLootGroup_DropsOnlyWhenQuestObjectiveIsActive()
    {
        bool active = false;
        IPlayer player = CreatePlayerWithQuestObjective(13011u, () => active, out var questManagerProxy);
        LootGroup group = CreateQuestVirtualLootGroup(objectiveId: 13011u, virtualItemId: 265u);

        Assert.Empty(group.GenerateLootDrops(player));

        active = true;
        List<(LootItem Item, uint Count)> drops = group.GenerateLootDrops(player).ToList();

        (LootItem item, uint count) = Assert.Single(drops);
        Assert.Equal(LootItemType.VirtualItem, item.Type);
        Assert.Equal(265u, item.StaticId);
        Assert.Equal(1u, count);
        Assert.All(questManagerProxy.GetInvocations(nameof(IQuestManager.IsActiveObjectiveId)),
            invocation => Assert.Equal(13011u, invocation.Arguments[0]));
    }

    [Fact]
    public void FactionLootGroup_DropsOnlyForMatchingFaction()
    {
        LootGroup group = CreateFactionLootGroup(Faction.Exile, staticItemId: 49979u);
        IPlayer exile = CreatePlayerWithFaction(Faction.Exile);
        IPlayer dominion = CreatePlayerWithFaction(Faction.Dominion);

        List<(LootItem Item, uint Count)> exileDrops = group.GenerateLootDrops(exile).ToList();
        List<(LootItem Item, uint Count)> dominionDrops = group.GenerateLootDrops(dominion).ToList();

        (LootItem item, uint count) = Assert.Single(exileDrops);
        Assert.Equal(LootItemType.StaticItem, item.Type);
        Assert.Equal(49979u, item.StaticId);
        Assert.Equal(1u, count);
        Assert.Empty(dominionDrops);
    }

    private static LootGroup CreateItemContainerLootGroup(params LootItemModel[] items)
    {
        LootGroupModel model = new()
        {
            Id          = items[0].Id,
            Probability = 100f,
            MinDrop     = 1u,
            MaxDrop     = 1u,
            Item        = items
        };

        return new LootGroup(model, loadChildren: false);
    }

    private static LootGroup CreateQuestVirtualLootGroup(uint objectiveId, uint virtualItemId)
    {
        LootGroupModel model = new()
        {
            Id            = 1200000001,
            Probability   = 100f,
            MinDrop       = 0u,
            MaxDrop       = 0u,
            ConditionType = (uint)LootConditionType.QuestObjectiveActive,
            Condition     = objectiveId,
            Item =
            [
                new LootItemModel
                {
                    Id          = 1200000001,
                    Type        = (uint)LootItemType.VirtualItem,
                    StaticId    = virtualItemId,
                    Probability = 100f,
                    MinCount    = 1u,
                    MaxCount    = 1u
                }
            ]
        };

        return new LootGroup(model, loadChildren: false);
    }

    private static LootGroup CreateFactionLootGroup(Faction faction, uint staticItemId)
    {
        LootGroupModel model = new()
        {
            Id            = 1200000002,
            Probability   = 100f,
            MinDrop       = 0u,
            MaxDrop       = 0u,
            ConditionType = (uint)LootConditionType.IsFaction,
            Condition     = (uint)faction,
            Item =
            [
                new LootItemModel
                {
                    Id          = 1200000002,
                    Type        = (uint)LootItemType.StaticItem,
                    StaticId    = staticItemId,
                    Probability = 100f,
                    MinCount    = 1u,
                    MaxCount    = 1u
                }
            ]
        };

        return new LootGroup(model, loadChildren: false);
    }

    private static IPlayer CreatePlayerWithQuestObjective(uint objectiveId, Func<bool> isActive, out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.IsActiveObjectiveId), args =>
        {
            return (uint)args[0] == objectiveId && isActive();
        });

        TestPlayerBuilder builder = TestPlayerBuilder.Create();
        builder.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return builder.Build();
    }

    private static IPlayer CreatePlayerWithFaction(Faction faction)
    {
        TestPlayerBuilder builder = TestPlayerBuilder.Create();
        builder.PlayerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        return builder.Build();
    }
}
