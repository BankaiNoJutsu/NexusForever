using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.PublicEvents;

public class TrackingSlotHelperTests
{
    [Fact]
    public void TryGetEntry_WithNullManagerOrMissingTable_ReturnsNull()
    {
        Assert.Null(TrackingSlotHelper.TryGetEntry(null, 42u));

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        Assert.Null(TrackingSlotHelper.TryGetEntry(gameTableManager, 42u));
    }

    [Fact]
    public void TryGetEntry_WithZeroTrackingSlotId_DoesNotReadTable()
    {
        IGameTableManager gameTableManager =
            RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);

        Assert.Null(TrackingSlotHelper.TryGetEntry(gameTableManager, 0u));
        Assert.Empty(proxy.GetInvocations("get_TrackingSlot"));
    }

    [Fact]
    public void TryGetEntry_MasksWireValueToFifteenBitTrackingSlotId()
    {
        TrackingSlotEntry expected = new()
        {
            Id = 0x1234u,
            PublicEventObjectiveId = 5010u,
            LocalizedTextIdLabel = 100u,
            IconPath = "ClientSprites:Marker"
        };
        IGameTableManager gameTableManager = CreateGameTableManager(expected);

        TrackingSlotEntry entry = TrackingSlotHelper.TryGetEntry(gameTableManager, 0x9234u);

        Assert.Same(expected, entry);
    }

    [Fact]
    public void TryGetPublicEventObjectiveId_ReturnsObjectiveFromTrackingSlotRow()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(new TrackingSlotEntry
        {
            Id = 0x321u,
            PublicEventObjectiveId = 5138u
        });

        Assert.Equal(5138u, TrackingSlotHelper.TryGetPublicEventObjectiveId(gameTableManager, 0x8321u));
    }

    [Fact]
    public void TrackingSlotHelper_DoesNotSelectSlotFromObjectiveOnlyWhenRowsShareObjective()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            new TrackingSlotEntry
            {
                Id = 0x0101u,
                PublicEventObjectiveId = 5010u
            },
            new TrackingSlotEntry
            {
                Id = 0x0102u,
                PublicEventObjectiveId = 5010u
            });

        Assert.Equal(5010u, TrackingSlotHelper.TryGetPublicEventObjectiveId(gameTableManager, 0x0101u));
        Assert.Equal(5010u, TrackingSlotHelper.TryGetPublicEventObjectiveId(gameTableManager, 0x0102u));
        Assert.DoesNotContain(
            typeof(TrackingSlotHelper).GetMethods(BindingFlags.Static | BindingFlags.Public),
            method => method.Name.Contains("TrackingSlotIdForPublicEventObjective", StringComparison.Ordinal));
    }

    private static IGameTableManager CreateGameTableManager(params TrackingSlotEntry[] entries)
    {
        IGameTableManager gameTableManager =
            RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.TrackingSlot), CreateGameTable(entries));
        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);

        int[] lookup = BuildLookup(entries, out ulong maxId);
        SetPrivateField(table, "lookup", lookup);
        SetPrivateField(table, "header", new GameTableHeader { MaxId = maxId });

        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries, out ulong maxId) where T : class
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        maxId = entries
            .Select(entry => Convert.ToUInt64(idField.GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        for (int i = 0; i < entries.Count; i++)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entries[i]));
            lookup[(int)id] = i;
        }

        return lookup;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
