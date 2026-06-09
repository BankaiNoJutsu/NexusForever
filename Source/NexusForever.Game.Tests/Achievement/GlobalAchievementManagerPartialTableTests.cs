using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Achievement;

[Collection(LegacyServiceProviderCollection.Name)]
public class GlobalAchievementManagerPartialTableTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Initialise_WithMissingAchievementTableUsesEmptyCaches(bool includeEmptyTable)
    {
        var manager = new GlobalAchievementManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(manager, gameTableManager =>
        {
            if (includeEmptyTable)
                SetTable(gameTableManager, nameof(GameTableManager.Achievement), CreateGameTable<AchievementEntry>());
        }));

        manager.Initialise();

        Assert.Null(manager.GetAchievement(42));
        Assert.Empty(manager.GetCharacterAchievements(AchievementType.KillCreatureEntry));
        Assert.Empty(manager.GetGuildAchievements(AchievementType.KillCreatureEntry));
    }

    [Fact]
    public void Initialise_WithTableBackedRowsIndexesCharacterAndGuildAchievements()
    {
        var manager = new GlobalAchievementManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(manager, gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Achievement), CreateGameTable(
                new AchievementEntry
                {
                    Id                = 42u,
                    AchievementTypeId = (uint)AchievementType.KillCreatureEntry
                },
                new AchievementEntry
                {
                    Id                = 43u,
                    AchievementTypeId = (uint)AchievementType.KillCreatureEntry,
                    Flags             = (uint)AchievementFlags.Guild
                }));
            SetTable(gameTableManager, nameof(GameTableManager.AchievementChecklist), CreateGameTable<AchievementChecklistEntry>());
        }));

        manager.Initialise();

        Assert.Equal(42u, manager.GetAchievement(42).Id);
        Assert.Equal(43u, manager.GetAchievement(43).Id);
        Assert.Equal([42], manager.GetCharacterAchievements(AchievementType.KillCreatureEntry).Select(a => a.Id));
        Assert.Equal([43], manager.GetGuildAchievements(AchievementType.KillCreatureEntry).Select(a => a.Id));
    }

    private static IServiceProvider BuildProvider(
        GlobalAchievementManager manager,
        Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .AddSingleton(CreateEmptyDatabaseManager())
            .AddSingleton(manager)
            .BuildServiceProvider();
    }

    private static DatabaseManager CreateEmptyDatabaseManager()
    {
        var manager = (DatabaseManager)RuntimeHelpers.GetUninitializedObject(typeof(DatabaseManager));
        SetPrivateField(manager, "databases", ImmutableDictionary<Type, IDatabase>.Empty);
        return manager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0ul : entries.Max(GetEntryId) + 1ul
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(T[] entries) where T : class, new()
    {
        if (entries.Length == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1ul)).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static ulong GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        return Convert.ToUInt64(idField.GetValue(entry));
    }

    private static void SetTable<T>(GameTableManager gameTableManager, string propertyName, GameTable<T> table) where T : class, new()
    {
        SetAutoProperty(gameTableManager, propertyName, table);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }
}
