using NexusForever.Database.World.Model;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Loot;

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
}
