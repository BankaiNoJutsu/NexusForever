using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Achievement;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Achievement;

public class AchievementInfoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithMissingChecklistTableUsesEmptyChecklist(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = BuildGameTableManager(
            includeEmptyTable ? CreateGameTable<AchievementChecklistEntry>() : null);

        var info = new AchievementInfo(new AchievementEntry
        {
            Id = 42u
        }, gameTableManager);

        Assert.Empty(info.ChecklistEntries);
    }

    [Fact]
    public void Constructor_WithChecklistTableFiltersRowsByAchievementId()
    {
        GameTableManager gameTableManager = BuildGameTableManager(CreateGameTable(
            new AchievementChecklistEntry
            {
                Id            = 1u,
                AchievementId = 42u,
                Bit           = 0u,
                ObjectId      = 100u
            },
            new AchievementChecklistEntry
            {
                Id            = 2u,
                AchievementId = 43u,
                Bit           = 1u,
                ObjectId      = 101u
            },
            new AchievementChecklistEntry
            {
                Id            = 3u,
                AchievementId = 42u,
                Bit           = 2u,
                ObjectId      = 102u
            }));

        var info = new AchievementInfo(new AchievementEntry
        {
            Id = 42u
        }, gameTableManager);

        Assert.Collection(
            info.ChecklistEntries,
            entry =>
            {
                Assert.Equal(42u, entry.AchievementId);
                Assert.Equal(0u, entry.Bit);
                Assert.Equal(100u, entry.ObjectId);
            },
            entry =>
            {
                Assert.Equal(42u, entry.AchievementId);
                Assert.Equal(2u, entry.Bit);
                Assert.Equal(102u, entry.ObjectId);
            });
    }

    private static GameTableManager BuildGameTableManager(GameTable<AchievementChecklistEntry> achievementChecklistTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (achievementChecklistTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.AchievementChecklist), achievementChecklistTable);

        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }
}
