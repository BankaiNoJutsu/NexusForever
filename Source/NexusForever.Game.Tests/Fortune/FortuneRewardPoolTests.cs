using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Fortune;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Fortune;

[Collection(LegacyServiceProviderCollection.Name)]
public class FortuneRewardPoolTests
{
    [Fact]
    public void GetRewardCatalog_RealPoolAdvertisesOnlyMappedItemRewards()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new AccountItemEntry { Id = 1u, Item2Id = 101u },
                new AccountItemEntry { Id = 2u, Item2Id = 202u },
                new AccountItemEntry { Id = 3u, EntitlementId = 303u },
                new AccountItemEntry
                {
                    Id = 4u,
                    AccountCurrencyEnum = (uint)AccountCurrencyType.FortuneCoin,
                    AccountCurrencyAmount = 1ul
                }
            ],
            [
                new Item2Entry { Id = 101u, ItemQualityId = (uint)Quality.Good },
                new Item2Entry { Id = 202u, ItemQualityId = (uint)Quality.Excellent }
            ]);

        try
        {
            var pool = new FortuneRewardPool();

            FortuneRewardCatalog catalog = pool.GetRewardCatalog();

            Assert.Equal([101u, 202u], catalog.Item2IdRewards);
            Assert.Equal(2, catalog.RewardItemProbabilities.Count);
            Assert.InRange(catalog.RewardItemProbabilities.Sum(), 0.99f, 1.01f);
            Assert.True(catalog.RewardItemProbabilities[0] > catalog.RewardItemProbabilities[1]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void PickCardRewards_RealPoolSkipsNonItemAccountRewards()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new AccountItemEntry { Id = 1u, Item2Id = 101u },
                new AccountItemEntry { Id = 2u, EntitlementId = 202u },
                new AccountItemEntry { Id = 3u, GenericUnlockSetId = 303u },
                new AccountItemEntry { Id = 4u, Item2Id = 104u },
                new AccountItemEntry { Id = 5u, Item2Id = 105u }
            ],
            [
                new Item2Entry { Id = 101u, ItemQualityId = (uint)Quality.Good },
                new Item2Entry { Id = 104u, ItemQualityId = (uint)Quality.Good },
                new Item2Entry { Id = 105u, ItemQualityId = (uint)Quality.Good }
            ]);

        try
        {
            var pool = new FortuneRewardPool();

            FortuneCardReward[] picks = pool.PickCardRewards(new ZeroRollRandom());

            Assert.Equal(3, picks.Length);
            Assert.Equal([1u, 4u, 5u], picks.Select(pick => pick.AccountItemId));
            Assert.Equal([101u, 104u, 105u], picks.Select(pick => pick.Item2Id));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

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

    private static IServiceProvider BuildGameTableProvider(AccountItemEntry[] accountItems, Item2Entry[] itemEntries)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItem), CreateGameTable(accountItems));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(itemEntries));

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        int maxId = (int)entries.Select(GetEntryId).DefaultIfEmpty(0u).Max();
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)(maxId + 1)
        });

        var lookup = new int[maxId + 1];
        Array.Fill(lookup, -1);
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        SetField(table, "lookup", lookup);
        return table;
    }

    private static uint GetEntryId<T>(T entry) where T : class, new()
    {
        return (uint)typeof(T)
            .GetField("Id", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        SetField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
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

        public bool IsCardRewardDisplayable(uint accountItemId)
        {
            return entries.Any(entry => entry.AccountItemId == accountItemId && entry.Item2Id != 0u);
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
