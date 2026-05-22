using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Static.Item;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Fortune
{
    /// <summary>
    /// Builds a conservative fortune reward pool from <see cref="AccountItemEntry"/> rows until a
    /// dedicated client fortune table is mapped.
    /// BLOCKED: retail Madame Fay weight table is not mapped; selection uses uniform random over candidates.
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
            return pool.Value
                .Where(entry => entry.Item2Id != 0u)
                .Select(entry => entry.Item2Id)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
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

        private static FortunePoolEntry PickEntry(IReadOnlyList<FortunePoolEntry> entries, HashSet<uint> usedAccountItemIds, Random random)
        {
            List<FortunePoolEntry> available = entries
                .Where(entry => !usedAccountItemIds.Contains(entry.AccountItemId))
                .ToList();

            if (available.Count == 0)
                available = entries.ToList();

            return available[random.Next(available.Count)];
        }

        private static IReadOnlyList<FortunePoolEntry> BuildPool()
        {
            return GameTableManager.Instance.AccountItem.Entries
                .Where(IsFortuneRewardCandidate)
                .Select(entry => new FortunePoolEntry(entry.Id, ResolveItem2Id(entry), MapRarity(entry)))
                .OrderBy(entry => entry.AccountItemId)
                .ToList();
        }

        private static bool IsFortuneRewardCandidate(AccountItemEntry entry)
        {
            if (entry == null)
                return false;

            if (entry.AccountCurrencyEnum == (uint)AccountCurrencyType.FortuneCoin && entry.Item2Id == 0u)
                return false;

            return entry.Item2Id != 0u
                || entry.EntitlementId != 0u
                || entry.GenericUnlockSetId != 0u;
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

            Item2Entry itemEntry = GameTableManager.Instance.Item.GetEntry(entry.Item2Id);
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

        private readonly record struct FortunePoolEntry(uint AccountItemId, uint Item2Id, RewardRarity Rarity);
    }
}
