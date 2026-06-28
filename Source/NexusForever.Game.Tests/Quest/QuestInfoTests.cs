using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class QuestInfoTests
{
    [Fact]
    public void Constructor_WithMissingQuestTablesCreatesEmptyCollections()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id = 1u
                }
            ]);

        var info = new QuestInfo(
            CreateEntry(prerequisiteQuestId: 10u, objectiveId: 20u),
            gameTableManager: gameTableManager);

        Assert.NotNull(info.DifficultyEntry);
        Assert.Empty(info.PrerequisiteQuests);
        Assert.Empty(info.Objectives);
        Assert.Empty(info.Rewards);
    }

    [Fact]
    public void RewardCalculations_WithCompleteTablesUseDifficultyAndFormulaRows()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id                   = 1u,
                    XpMultiplier         = 1.5f,
                    CashRewardMultiplier = 2f,
                    RepRewardMultiplier  = 1.5f
                }
            ],
            xpEntries:
            [
                new XpPerLevelEntry
                {
                    Id                    = 3u,
                    BaseQuestXpPerLevel   = 100u,
                    BaseRepRewardPerLevel = 20u
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

        var info = new QuestInfo(CreateEntry(), gameTableManager: gameTableManager);

        Assert.Equal(150u, info.GetRewardExperience());
        Assert.Equal(18u, info.GetRewardMoney());
        Assert.Equal(30f, info.GetRewardReputation(0f));
        Assert.Single(info.Rewards);
    }

    [Fact]
    public void RewardCalculations_WithMissingXpOrFormulaTablesReturnZero()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id                   = 1u,
                    XpMultiplier         = 1.5f,
                    CashRewardMultiplier = 2f
                }
            ]);

        var info = new QuestInfo(CreateEntry(), gameTableManager: gameTableManager);

        Assert.Equal(0u, info.GetRewardExperience());
        Assert.Equal(0u, info.GetRewardMoney());
        Assert.Equal(0f, info.GetRewardReputation(0f));
    }

    [Fact]
    public void RewardCalculations_WithMissingDifficultyRowReturnZero()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
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

        var info = new QuestInfo(CreateEntry(), gameTableManager: gameTableManager);

        Assert.Null(info.DifficultyEntry);
        Assert.Equal(0u, info.GetRewardExperience());
        Assert.Equal(0u, info.GetRewardMoney());
        Assert.Equal(0f, info.GetRewardReputation(0f));
    }

    [Fact]
    public void RewardCalculations_WithOverridesDoNotRequireTables()
    {
        GameTableManager gameTableManager = CreateGameTableManager();

        var info = new QuestInfo(
            CreateEntry(rewardXpOverride: 123u, rewardCashOverride: 456u),
            gameTableManager: gameTableManager);

        Assert.Equal(123u, info.GetRewardExperience());
        Assert.Equal(456u, info.GetRewardMoney());
        Assert.Equal(789f, info.GetRewardReputation(789f));
    }

    [Theory]
    [InlineData(3479u)]
    [InlineData(3480u)]
    public void RewardCalculations_Q3479Q3480Build16042CashFormulaReturnsMappedAmount(uint questId)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            difficultyEntries:
            [
                new Quest2DifficultyEntry
                {
                    Id                   = 3u,
                    CashRewardMultiplier = 10f
                }
            ],
            formulaEntries:
            [
                new GameFormulaEntry
                {
                    Id         = 530u,
                    Datafloat0 = 1.93f
                }
            ]);

        var info = new QuestInfo(
            CreateEntry(
                id: questId,
                conLevel: 3u,
                difficultyId: 3u),
            gameTableManager: gameTableManager);

        Assert.Equal(83u, info.GetRewardMoney());
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

    private static GameTableManager CreateGameTableManager(
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

        return gameTableManager;
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
