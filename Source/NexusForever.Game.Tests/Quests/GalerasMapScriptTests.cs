using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.Galeras;

namespace NexusForever.Game.Tests.Quests;

public class GalerasMapScriptTests
{
    private const uint GalerasWorld = 51u;
    private const uint Q4696TempleRetreatWorldLocation = 12649u;

    [Fact]
    public void OnLoad_SpawnsQ4696TempleRetreatTrigger()
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = GalerasWorld });
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[2] is ISearchCheck<IWorldLocationVolumeGridTriggerEntity>)
                return Array.Empty<IWorldLocationVolumeGridTriggerEntity>();

            return Array.Empty<IWorldEntity>();
        });

        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () => trigger);

        IGameTableManager gameTableManager = CreateGameTableManager(
            CreateWorldLocation(Q4696TempleRetreatWorldLocation, 6207.62f, -888.804f, -2818.61f));

        var script = new GalerasMapScript(
            NullLogger<GalerasMapScript>.Instance,
            entityFactory,
            gameTableManager);

        script.OnLoad(map);

        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            triggerProxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(Q4696TempleRetreatWorldLocation, initialise.Arguments[0]);
        Assert.Equal(0u, initialise.Arguments[1]);

        RecordingDispatchProxy<IBaseMap>.Invocation enqueue = Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Same(trigger, enqueue.Arguments[0]);
        var position = (IMapPosition)enqueue.Arguments[1];
        Assert.Equal(new Vector3(6207.62f, -888.804f, -2818.61f), position.Position);
        Assert.Same(map.Entry, position.Info.Entry);
    }

    [Fact]
    public void OnLoad_WhenRetreatTriggerAlreadyExists_DoesNotSpawnDuplicateQ4696RetreatTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity existingTrigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> existingTriggerProxy);
        existingTriggerProxy.SetProperty(nameof(IWorldLocationVolumeGridTriggerEntity.Entry), new WorldLocation2Entry
        {
            Id = Q4696TempleRetreatWorldLocation
        });

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = GalerasWorld });
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[2] is ISearchCheck<IWorldLocationVolumeGridTriggerEntity>)
                return new[] { existingTrigger };

            return Array.Empty<IWorldEntity>();
        });

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        IGameTableManager gameTableManager = CreateGameTableManager(
            CreateWorldLocation(Q4696TempleRetreatWorldLocation, 6207.62f, -888.804f, -2818.61f));

        var script = new GalerasMapScript(
            NullLogger<GalerasMapScript>.Instance,
            entityFactory,
            gameTableManager);

        script.OnLoad(map);

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(existingTriggerProxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
    }

    private static IGameTableManager CreateGameTableManager(params WorldLocation2Entry[] worldLocations)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));
        return gameTableManager;
    }

    private static WorldLocation2Entry CreateWorldLocation(uint id, float x, float y, float z)
    {
        return new WorldLocation2Entry
        {
            Id = id,
            Position0 = x,
            Position1 = y,
            Position2 = z
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
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
