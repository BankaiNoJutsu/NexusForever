using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Entity;

public class PrerequisiteManagerTests
{
    [Fact]
    public void Meets_WithMissingPrerequisiteTableFailsClosed()
    {
        PrerequisiteManager manager = CreateManager(CreateGameTableManager());

        Assert.False(manager.Meets(player: null, prerequisiteId: 2755u));
    }

    [Fact]
    public void Meets_WithMissingPrerequisiteRowFailsClosed()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Prerequisite), CreateGameTable<PrerequisiteEntry>());

        PrerequisiteManager manager = CreateManager(gameTableManager);

        Assert.False(manager.Meets(player: null, prerequisiteId: 6338u));
    }

    [Theory]
    [InlineData(4992u, 4u, 1u, 0, false)]
    [InlineData(4992u, 4u, 1u, 1, true)]
    [InlineData(4992u, 4u, 1u, 2, true)]
    [InlineData(391u, 3u, 8u, 7, false)]
    [InlineData(391u, 3u, 8u, 8, false)]
    [InlineData(391u, 3u, 8u, 9, true)]
    [InlineData(76u, 6u, 5u, 4, true)]
    [InlineData(76u, 6u, 5u, 5, true)]
    [InlineData(76u, 6u, 5u, 6, false)]
    [InlineData(6232u, 5u, 1u, 0, true)]
    [InlineData(6232u, 5u, 1u, 1, false)]
    [InlineData(6232u, 5u, 1u, 2, false)]
    public void LevelRowsFromBuild16042KeepStrictAndInclusiveBoundaries(uint id, uint mode, uint required, byte level, bool expected)
    {
        // Correlated with the imported build-16042 Prerequisite table. Preserve raw IDs
        // so a symbolic enum in a test cannot hide a wrong table-to-runtime mapping.
        var tables = CreateGameTableManager();
        SetAutoProperty(tables, nameof(GameTableManager.Prerequisite), CreateGameTable(new PrerequisiteEntry
        {
            Id = id, Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.Level, PrerequisiteType.None, PrerequisiteType.None],
            PrerequisiteComparisonId = [(PrerequisiteComparison)mode, 0, 0],
            Value = [required, 0, 0], ObjectId = [0, 0, 0]
        }));
        using var services = new ServiceCollection()
            .AddKeyedTransient<IPrerequisiteCheck>(PrerequisiteType.Level, (_, _) => new PrerequisiteCheckLevel(NullLogger<PrerequisiteCheckLevel>.Instance))
            .BuildServiceProvider();
        var manager = new PrerequisiteManager(NullLogger<PrerequisiteManager>.Instance, services, tables, new PrerequisiteParametersFactory());
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Level), (uint)level);
        Assert.Equal(expected, manager.Meets(player, id));
    }

    private static PrerequisiteManager CreateManager(GameTableManager gameTableManager)
    {
        return new PrerequisiteManager(
            NullLogger<PrerequisiteManager>.Instance,
            new ServiceCollection().BuildServiceProvider(),
            gameTableManager,
            new PrerequisiteParametersFactory());
    }

    private static GameTableManager CreateGameTableManager()
    {
        return (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
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

    private sealed class PrerequisiteParametersFactory : IFactory<IPrerequisiteParameters>
    {
        public IPrerequisiteParameters Resolve()
        {
            return new PrerequisiteParameters();
        }
    }
}
