using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Housing;

public class GlobalResidenceManagerTests
{
    [Theory]
    [InlineData("property-missing-table")]
    [InlineData("property-empty-table")]
    [InlineData("location-missing-table")]
    [InlineData("location-empty-table")]
    [InlineData("world-missing-table")]
    [InlineData("world-empty-table")]
    public void GetResidenceEntrance_WithMissingStaticDataThrowsHousingException(string missingSource)
    {
        GameTableManager gameTableManager = BuildGameTableManager(gameTableManager =>
        {
            if (missingSource != "property-missing-table")
            {
                GameTable<HousingPropertyInfoEntry> table = missingSource == "property-empty-table"
                    ? CreateGameTable<HousingPropertyInfoEntry>()
                    : CreateGameTable(new HousingPropertyInfoEntry
                    {
                        Id               = (uint)PropertyInfoId.Residence,
                        WorldLocation2Id = 700u
                    });
                SetTable(gameTableManager, nameof(GameTableManager.HousingPropertyInfo), table);
            }

            if (missingSource != "location-missing-table")
            {
                GameTable<WorldLocation2Entry> table = missingSource == "location-empty-table"
                    ? CreateGameTable<WorldLocation2Entry>()
                    : CreateGameTable(new WorldLocation2Entry
                    {
                        Id      = 700u,
                        WorldId = 900u
                    });
                SetTable(gameTableManager, nameof(GameTableManager.WorldLocation2), table);
            }

            if (missingSource != "world-missing-table")
            {
                GameTable<WorldEntry> table = missingSource == "world-empty-table"
                    ? CreateGameTable<WorldEntry>()
                    : CreateGameTable(new WorldEntry
                    {
                        Id = 900u
                    });
                SetTable(gameTableManager, nameof(GameTableManager.World), table);
            }
        });
        GlobalResidenceManager manager = CreateManager(gameTableManager);

        Assert.Throws<HousingException>(() => manager.GetResidenceEntrance(PropertyInfoId.Residence));
    }

    [Fact]
    public void GetResidenceEntrance_WithTableBackedRowsBuildsEntrance()
    {
        GameTableManager gameTableManager = BuildGameTableManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.HousingPropertyInfo), CreateGameTable(new HousingPropertyInfoEntry
            {
                Id               = (uint)PropertyInfoId.Residence,
                WorldLocation2Id = 700u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
            {
                Id        = 700u,
                WorldId   = 900u,
                Position0 = 1f,
                Position1 = 2f,
                Position2 = 3f,
                Facing0   = 4f,
                Facing1   = 5f,
                Facing2   = 6f,
                Facing3   = 7f
            }));
            SetTable(gameTableManager, nameof(GameTableManager.World), CreateGameTable(new WorldEntry
            {
                Id = 900u
            }));
        });
        GlobalResidenceManager manager = CreateManager(gameTableManager);

        IResidenceEntrance entrance = manager.GetResidenceEntrance(PropertyInfoId.Residence);

        Assert.Equal(900u, entrance.Entry.Id);
        Assert.Equal(1f, entrance.Position.X);
        Assert.Equal(2f, entrance.Position.Y);
        Assert.Equal(3f, entrance.Position.Z);
        Assert.Equal(4f, entrance.Rotation.X);
        Assert.Equal(5f, entrance.Rotation.Y);
        Assert.Equal(6f, entrance.Rotation.Z);
        Assert.Equal(7f, entrance.Rotation.W);
    }

    private static GameTableManager BuildGameTableManager(Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);
        return gameTableManager;
    }

    private static GlobalResidenceManager CreateManager(GameTableManager gameTableManager)
    {
        return new GlobalResidenceManager(gameTableManager: gameTableManager);
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
