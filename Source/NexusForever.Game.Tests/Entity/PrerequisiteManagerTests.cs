using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
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
