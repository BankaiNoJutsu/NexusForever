using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Quest;

[Collection(LegacyServiceProviderCollection.Name)]
public class QuestInfoTests
{
    [Fact]
    public void Constructor_WithMissingQuestTablesCreatesEmptyCollections()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id = 1u
                }
            ]);

        try
        {
            var info = new QuestInfo(CreateEntry(prerequisiteQuestId: 10u, objectiveId: 20u));

            Assert.NotNull(info.DifficultyEntry);
            Assert.Empty(info.PrerequisiteQuests);
            Assert.Empty(info.Objectives);
            Assert.Empty(info.Rewards);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RewardCalculations_WithCompleteTablesUseDifficultyAndFormulaRows()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id                   = 1u,
                    XpMultiplier         = 1.5f,
                    CashRewardMultiplier = 2f
                }
            ],
            xpEntries:
            [
                new XpPerLevelEntry
                {
                    Id                  = 3u,
                    BaseQuestXpPerLevel = 100u
                }
            ],
            formulaEntries:
            [
                new GameFormulaEntry
                {
                    Id         = 530u,
                    Datafloat0 = 2f
                }
            ],
            rewardEntries:
            [
                new Quest2RewardEntry
                {
                    Id       = 7u,
                    Quest2Id = 9001u
                }
            ]);

        try
        {
            var info = new QuestInfo(CreateEntry());

            Assert.Equal(150u, info.GetRewardExperience());
            Assert.Equal(18u, info.GetRewardMoney());
            Assert.Single(info.Rewards);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RewardCalculations_WithMissingXpOrFormulaTablesReturnZero()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id                   = 1u,
                    XpMultiplier         = 1.5f,
                    CashRewardMultiplier = 2f
                }
            ]);

        try
        {
            var info = new QuestInfo(CreateEntry());

            Assert.Equal(0u, info.GetRewardExperience());
            Assert.Equal(0u, info.GetRewardMoney());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RewardCalculations_WithMissingDifficultyRowReturnZero()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(
            xpEntries:
            [
                new XpPerLevelEntry
                {
                    Id                  = 3u,
                    BaseQuestXpPerLevel = 100u
                }
            ],
            formulaEntries:
            [
                new GameFormulaEntry
                {
                    Id         = 530u,
                    Datafloat0 = 2f
                }
            ]);

        try
        {
            var info = new QuestInfo(CreateEntry());

            Assert.Null(info.DifficultyEntry);
            Assert.Equal(0u, info.GetRewardExperience());
            Assert.Equal(0u, info.GetRewardMoney());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RewardCalculations_WithOverridesDoNotRequireTables()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var info = new QuestInfo(CreateEntry(rewardXpOverride: 123u, rewardCashOverride: 456u));

            Assert.Equal(123u, info.GetRewardExperience());
            Assert.Equal(456u, info.GetRewardMoney());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static Quest2Entry CreateEntry(
        uint id = 9001u,
        uint conLevel = 3u,
        uint difficultyId = 1u,
        uint prerequisiteQuestId = 0u,
        uint objectiveId = 0u,
        uint rewardXpOverride = 0u,
        uint rewardCashOverride = 0u)
    {
        return new Quest2Entry
        {
            Id                 = id,
            ConLevel           = conLevel,
            Quest2DifficultyId = difficultyId,
            RewardXpOverride   = rewardXpOverride,
            RewardCashOverride = rewardCashOverride,
            PrerequisiteQuests = [prerequisiteQuestId, 0u, 0u],
            Objectives         = [objectiveId, 0u, 0u, 0u, 0u, 0u],
            PushedItemIds      = new uint[6],
            PushedItemCounts   = new uint[6]
        };
    }

    private static IServiceProvider BuildProvider(
        Quest2DifficultyEntry[] difficultyEntries = null,
        XpPerLevelEntry[] xpEntries = null,
        GameFormulaEntry[] formulaEntries = null,
        Quest2RewardEntry[] rewardEntries = null,
        Quest2Entry[] questEntries = null,
        QuestObjectiveEntry[] objectiveEntries = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (difficultyEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2Difficulty), CreateGameTable(difficultyEntries));
        if (xpEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.XpPerLevel), CreateGameTable(xpEntries));
        if (formulaEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.GameFormula), CreateGameTable(formulaEntries));
        if (rewardEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2Reward), CreateGameTable(rewardEntries));
        if (questEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable(questEntries));
        if (objectiveEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestObjective), CreateGameTable(objectiveEntries));

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);

        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Select(entry => (uint)idField.GetValue(entry)!).DefaultIfEmpty().Max();
        var lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)(uint)idField.GetValue(entries[i])!] = i;

        SetField(table, "lookup", lookup);
        SetField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)lookup.Length
        });

        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
