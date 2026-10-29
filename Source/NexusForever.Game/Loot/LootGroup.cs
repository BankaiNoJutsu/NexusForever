using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Loot
{
    public readonly record struct GeneratedLootDrop(LootItem Item, uint Count, IReadOnlyList<IPlayer> EligiblePlayers);

    public class LootGroup
    {
        public ulong Id { get; }
        public float Probability { get; }
        public string Comment { get; }

        private readonly uint minDrop;
        private readonly uint maxDrop;
        private readonly LootConditionType conditionType;
        private readonly uint condition;

        private readonly List<LootGroup> childLootGroups = [];
        private readonly List<LootItem> lootItems = [];

        public LootGroup(LootGroupModel lootGroupModel, bool loadChildren = true, IDatabaseManager databaseManager = null)
        {
            Id            = lootGroupModel.Id;
            Probability   = lootGroupModel.Probability;
            Comment       = lootGroupModel.Comment;
            minDrop       = lootGroupModel.MinDrop;
            maxDrop       = Math.Max(lootGroupModel.MinDrop, lootGroupModel.MaxDrop);
            conditionType = (LootConditionType)lootGroupModel.ConditionType;
            condition     = lootGroupModel.Condition;

            if (loadChildren && databaseManager != null)
            {
                foreach (LootGroupModel childLootGroup in databaseManager.GetDatabase<WorldDatabase>().GetLootGroupChildren(Id))
                    childLootGroups.Add(new LootGroup(childLootGroup, databaseManager: databaseManager));
            }

            foreach (LootItemModel lootItemModel in lootGroupModel.Item)
                lootItems.Add(new LootItem(lootItemModel));
        }

        public IEnumerable<(LootItem Item, uint Count)> GenerateLootDrops(IPlayer player)
        {
            return GenerateLootDropsForPlayers([player]).Select(d => (d.Item, d.Count));
        }

        public IEnumerable<GeneratedLootDrop> GenerateLootDropsForPlayers(IReadOnlyList<IPlayer> players)
        {
            List<IPlayer> eligible = players.Where(MeetsCondition).ToList();
            if (eligible.Count == 0 || !(Probability > 0f) || Random.Shared.NextDouble() * 100d >= Probability)
                return [];

            List<GeneratedLootDrop> itemsDropped = GenerateLootItems(eligible);
            if (minDrop == 0u && maxDrop == 0u)
                return itemsDropped;

            uint desiredDrop = minDrop == maxDrop ? minDrop : (uint)Random.Shared.NextInt64(minDrop, (long)maxDrop + 1);

            for (int attempts = 0; itemsDropped.Count < minDrop && attempts < 10; attempts++)
                itemsDropped.AddRange(GenerateLootItems(eligible));

            while (itemsDropped.Count > desiredDrop)
                itemsDropped.RemoveAt(Random.Shared.Next(0, itemsDropped.Count));

            return itemsDropped;
        }

        private bool MeetsCondition(IPlayer player)
        {
            return conditionType switch
            {
                LootConditionType.None                 => true,
                LootConditionType.IsClass              => player != null && (uint)player.Class == condition,
                LootConditionType.IsRace               => player != null && (uint)player.Race == condition,
                LootConditionType.IsLevel              => player != null && player.Level == condition,
                LootConditionType.IsLessThanLevel      => player != null && player.Level < condition,
                LootConditionType.IsMoreThanLevel      => player != null && player.Level > condition,
                LootConditionType.QuestObjectiveActive => player?.QuestManager != null && player.QuestManager.IsActiveObjectiveId(condition),
                LootConditionType.IsFaction            => player != null && (uint)player.Faction1 == condition,
                LootConditionType.QuestIsComplete      => IsQuestState(player, completed: true),
                LootConditionType.QuestNotComplete     => IsQuestState(player, completed: false),
                _                                      => false
            };
        }

        private bool IsQuestState(IPlayer player, bool completed)
        {
            return player?.QuestManager != null && condition is > 0 and <= ushort.MaxValue
                && (player.QuestManager.GetQuestState((ushort)condition) == QuestState.Completed) == completed;
        }

        private List<GeneratedLootDrop> GenerateLootItems(IReadOnlyList<IPlayer> players)
        {
            List<GeneratedLootDrop> itemsDropped = [];

            foreach (LootGroup lootGroup in childLootGroups)
                itemsDropped.AddRange(lootGroup.GenerateLootDropsForPlayers(players));

            foreach (LootItem lootItem in lootItems)
            {
                if (lootItem.TryGetDrop(out uint count))
                    itemsDropped.Add(new GeneratedLootDrop(lootItem, count, players));
            }

            return itemsDropped;
        }
    }
}
