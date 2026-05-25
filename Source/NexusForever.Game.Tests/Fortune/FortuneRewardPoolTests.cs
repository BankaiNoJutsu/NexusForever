using NexusForever.Game.Fortune;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Fortune;

namespace NexusForever.Game.Tests.Fortune;

public class FortuneRewardPoolTests
{
    [Fact]
    public void GetRewardCatalog_ProbabilitiesAreNormalizedFractions()
    {
        var pool = new TestFortuneRewardPool(
        [
            new TestFortuneRewardPool.Entry(1u, 101u, RewardRarity.Normal, 1000u),
            new TestFortuneRewardPool.Entry(2u, 202u, RewardRarity.Rare, 200u),
            new TestFortuneRewardPool.Entry(3u, 303u, RewardRarity.Epic, 50u)
        ]);

        FortuneRewardCatalog catalog = pool.GetRewardCatalog();

        Assert.Equal([101u, 202u, 303u], catalog.Item2IdRewards);
        Assert.Equal(3, catalog.RewardItemProbabilities.Count);
        Assert.InRange(catalog.RewardItemProbabilities.Sum(), 0.99f, 1.01f);
        Assert.True(catalog.RewardItemProbabilities[0] > catalog.RewardItemProbabilities[1]);
        Assert.True(catalog.RewardItemProbabilities[1] > catalog.RewardItemProbabilities[2]);
    }

    [Fact]
    public void PickCardRewards_ZeroRollSelectsFromTopOfWeightWalk()
    {
        var pool = new TestFortuneRewardPool(
        [
            new TestFortuneRewardPool.Entry(1u, 101u, RewardRarity.Normal, 10_000u),
            new TestFortuneRewardPool.Entry(2u, 102u, RewardRarity.Normal, 10_000u),
            new TestFortuneRewardPool.Entry(3u, 103u, RewardRarity.Normal, 10_000u),
            new TestFortuneRewardPool.Entry(4u, 404u, RewardRarity.Epic, 1u)
        ]);

        FortuneCardReward[] picks = pool.PickCardRewards(new ZeroRollRandom());

        Assert.Equal(3, picks.Length);
        Assert.Equal(3, picks.Select(pick => pick.AccountItemId).Distinct().Count());
        Assert.DoesNotContain(picks, pick => pick.AccountItemId == 4u);
    }

    private sealed class TestFortuneRewardPool : IFortuneRewardPool
    {
        private readonly IReadOnlyList<Entry> entries;

        public TestFortuneRewardPool(IReadOnlyList<Entry> entries)
        {
            this.entries = entries;
        }

        public IReadOnlyList<uint> GetDisplayItem2Ids() => GetRewardCatalog().Item2IdRewards;

        public FortuneRewardCatalog GetRewardCatalog()
        {
            var item2Ids = entries.Where(entry => entry.Item2Id != 0u).Select(entry => entry.Item2Id).ToList();
            ulong totalWeight = 0;
            foreach (Entry entry in entries.Where(entry => entry.Item2Id != 0u))
                totalWeight += entry.Weight;

            var probabilities = entries
                .Where(entry => entry.Item2Id != 0u)
                .Select(entry => entry.Weight / (float)totalWeight)
                .ToList();

            return new FortuneRewardCatalog(item2Ids, probabilities);
        }

        public FortuneCardReward[] PickCardRewards(Random random)
        {
            const int cardCount = 3;
            var picks = new FortuneCardReward[cardCount];
            var used = new HashSet<uint>();

            for (int i = 0; i < cardCount; i++)
            {
                List<Entry> available = entries.Where(entry => !used.Contains(entry.AccountItemId)).ToList();
                if (available.Count == 0)
                    available = entries.ToList();

                Entry entry = PickWeighted(available, random);
                used.Add(entry.AccountItemId);
                picks[i] = new FortuneCardReward(entry.AccountItemId, entry.Item2Id, entry.Rarity);
            }

            return picks;
        }

        private static Entry PickWeighted(List<Entry> available, Random random)
        {
            uint totalWeight = 0;
            foreach (Entry entry in available)
                totalWeight += entry.Weight;

            uint roll = (uint)random.NextInt64(totalWeight);
            foreach (Entry entry in available)
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

        internal readonly record struct Entry(uint AccountItemId, uint Item2Id, RewardRarity Rarity, uint Weight);
    }

    private sealed class ZeroRollRandom : Random
    {
        public override int Next(int maxValue) => 0;

        public override int Next(int minValue, int maxValue) => minValue;

        public override long NextInt64(long maxValue) => 0;
    }
}
