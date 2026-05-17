using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Loot
{
    public class LootGroup
    {
        public ulong Id { get; }
        public float Probability { get; }

        private readonly uint minDrop;
        private readonly uint maxDrop;
        private readonly LootConditionType conditionType;
        private readonly uint condition;

        private readonly List<LootGroup> childLootGroups = [];
        private readonly List<LootItem> lootItems = [];

        public LootGroup(LootGroupModel lootGroupModel, bool loadChildren = true)
        {
            Id            = lootGroupModel.Id;
            Probability   = lootGroupModel.Probability;
            minDrop       = lootGroupModel.MinDrop;
            maxDrop       = Math.Max(lootGroupModel.MinDrop, lootGroupModel.MaxDrop);
            conditionType = (LootConditionType)lootGroupModel.ConditionType;
            condition     = lootGroupModel.Condition;

            if (loadChildren)
            {
                foreach (LootGroupModel childLootGroup in DatabaseManager.Instance.GetDatabase<WorldDatabase>().GetLootGroupChildren(Id))
                    childLootGroups.Add(new LootGroup(childLootGroup));
            }

            foreach (LootItemModel lootItemModel in lootGroupModel.Item)
                lootItems.Add(new LootItem(lootItemModel));
        }

        public IEnumerable<(LootItem Item, uint Count)> GenerateLootDrops(IPlayer player)
        {
            if (!WillDrop(player))
                return [];

            List<(LootItem Item, uint Count)> itemsDropped = GenerateLootItems(player);
            if (minDrop == 0u && maxDrop == 0u)
                return itemsDropped;

            uint desiredDrop = minDrop == maxDrop ? minDrop : (uint)Random.Shared.Next((int)minDrop, (int)maxDrop + 1);

            for (int attempts = 0; itemsDropped.Count < minDrop && attempts < 10; attempts++)
                itemsDropped.AddRange(GenerateLootItems(player));

            while (itemsDropped.Count > desiredDrop)
                itemsDropped.RemoveAt(Random.Shared.Next(0, itemsDropped.Count));

            return itemsDropped;
        }

        private bool WillDrop(IPlayer player)
        {
            if (!MeetsCondition(player))
                return false;

            double chance = Random.Shared.NextDouble() * 100d;
            return chance < Probability;
        }

        private bool MeetsCondition(IPlayer player)
        {
            return conditionType switch
            {
                LootConditionType.None                 => true,
                LootConditionType.QuestObjectiveActive => player.QuestManager.IsActiveObjectiveId(condition),
                _                                      => true
            };
        }

        private List<(LootItem Item, uint Count)> GenerateLootItems(IPlayer player)
        {
            List<(LootItem Item, uint Count)> itemsDropped = [];

            foreach (LootGroup lootGroup in childLootGroups)
                itemsDropped.AddRange(lootGroup.GenerateLootDrops(player));

            foreach (LootItem lootItem in lootItems)
            {
                if (lootItem.TryGetDrop(out uint count))
                    itemsDropped.Add((lootItem, count));
            }

            return itemsDropped;
        }
    }
}
