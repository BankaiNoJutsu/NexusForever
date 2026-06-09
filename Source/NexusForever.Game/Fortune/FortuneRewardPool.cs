using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Static.Item;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Fortune
{
    /// <summary>
    /// Builds the Madame Fay reward pool from <see cref="AccountItemEntry"/> rows. Card selection and
    /// <c>ServerFortuneRewards.RewardItemProbabilities</c> use the same emulator rarity-tier weights.
    /// Exact retail per-item weights remain blocked: <c>AccountItem.tbl</c> has no weight column and
    /// <c>FortunesLib.GetFortunesLootList</c> reads server-sent floats.
    /// </summary>
    public sealed class FortuneRewardPool : IFortuneRewardPool
    {
        private const int CardCount = 3;

        private readonly Lazy<IReadOnlyList<FortunePoolEntry>> pool;

        public FortuneRewardPool()
        {
            pool = new Lazy<IReadOnlyList<FortunePoolEntry>>(BuildPool);
        }

        public IReadOnlyList<uint> GetDisplayItem2Ids()
        {
            return GetRewardCatalog().Item2IdRewards;
        }

        public FortuneRewardCatalog GetRewardCatalog()
        {
            IReadOnlyList<FortunePoolEntry> entries = pool.Value;
            if (entries.Count == 0)
                return new FortuneRewardCatalog([], []);

            var item2Ids = new List<uint>(entries.Count);
            var weights = new List<uint>(entries.Count);
            foreach (FortunePoolEntry entry in entries)
            {
                item2Ids.Add(entry.Item2Id);
                weights.Add(entry.Weight);
            }

            ulong totalWeight = 0;
            foreach (uint weight in weights)
                totalWeight += weight;

            var probabilities = new List<float>(weights.Count);
            foreach (uint weight in weights)
                probabilities.Add(FortuneRewardWeights.ToDisplayProbability(weight, totalWeight));

            return new FortuneRewardCatalog(item2Ids, probabilities);
        }

        public FortuneCardReward[] PickCardRewards(Random random)
        {
            IReadOnlyList<FortunePoolEntry> entries = pool.Value;
            if (entries.Count == 0)
                return [];

            var picks = new FortuneCardReward[CardCount];
            var usedAccountItemIds = new HashSet<uint>();

            for (int i = 0; i < CardCount; i++)
            {
                FortunePoolEntry entry = PickEntry(entries, usedAccountItemIds, random);
                usedAccountItemIds.Add(entry.AccountItemId);
                picks[i] = new FortuneCardReward(entry.AccountItemId, entry.Item2Id, entry.Rarity);
            }

            return picks;
        }

        public bool IsCardRewardDisplayable(uint accountItemId)
        {
            if (accountItemId == 0u)
                return false;

            return pool.Value.Any(entry => entry.AccountItemId == accountItemId);
        }

        private static FortunePoolEntry PickEntry(
            IReadOnlyList<FortunePoolEntry> entries,
            HashSet<uint> usedAccountItemIds,
            Random random)
        {
            List<FortunePoolEntry> available = entries
                .Where(entry => !usedAccountItemIds.Contains(entry.AccountItemId))
                .ToList();

            if (available.Count == 0)
                available = entries.ToList();

            uint totalWeight = 0;
            foreach (FortunePoolEntry entry in available)
                totalWeight += entry.Weight;

            if (totalWeight == 0u)
                return available[random.Next(available.Count)];

            uint roll = (uint)random.NextInt64((long)totalWeight);
            foreach (FortunePoolEntry entry in available)
            {
                if (entry.Weight <= roll)
                {
                    roll -= entry.Weight;
                    continue;
                }

                return entry;
            }

            return available[^1];
        }

        private static IReadOnlyList<FortunePoolEntry> BuildPool()
        {
            IEnumerable<AccountItemEntry> entries = GameTableManager.Instance.AccountItem?.Entries ?? [];
            return entries
                .Where(IsFortuneRewardCandidate)
                .Select(entry =>
                {
                    RewardRarity rarity = MapRarity(entry);
                    return new FortunePoolEntry(
                        entry.Id,
                        ResolveItem2Id(entry),
                        rarity,
                        FortuneRewardWeights.GetWeight(entry, rarity));
                })
                .Where(entry => entry.Weight > 0u)
                .OrderBy(entry => entry.AccountItemId)
                .ToList();
        }

        private static bool IsFortuneRewardCandidate(AccountItemEntry entry)
        {
            if (entry == null)
                return false;

            // Fortune_ApplyCards resolves each dealt account item to an item display object and
            // dereferences that object without a null guard. Entitlement/unlock-only rows have no
            // Item2 display object, so keep dealt cards item-backed until retail evidence maps a
            // card-safe non-item payload.
            return entry.Item2Id != 0u;
        }

        private static uint ResolveItem2Id(AccountItemEntry entry)
        {
            if (entry.Item2Id != 0u)
                return entry.Item2Id;

            return 0u;
        }

        private static RewardRarity MapRarity(AccountItemEntry entry)
        {
            if (entry.Item2Id == 0u)
                return RewardRarity.Normal;

            Item2Entry itemEntry = GameTableManager.Instance.Item?.GetEntry(entry.Item2Id);
            if (itemEntry == null)
                return RewardRarity.Normal;

            return MapRarity(itemEntry.ItemQualityId);
        }

        private static RewardRarity MapRarity(uint itemQualityId)
        {
            return itemQualityId switch
            {
                (uint)Quality.Legendary or (uint)Quality.Artifact => RewardRarity.Epic,
                (uint)Quality.Excellent or (uint)Quality.Superb => RewardRarity.Rare,
                _ => RewardRarity.Normal
            };
        }

        private readonly record struct FortunePoolEntry(
            uint AccountItemId,
            uint Item2Id,
            RewardRarity Rarity,
            uint Weight);
    }
}
