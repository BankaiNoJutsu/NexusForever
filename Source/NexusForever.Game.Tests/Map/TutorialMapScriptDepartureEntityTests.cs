using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Tests.Map;

public class TutorialMapScriptDepartureEntityTests
{
    [Theory]
    [MemberData(nameof(DepartureFallbackCases))]
    public void EnsureDepartureEntities_SpawnsTerminalsAndChecklistInteractables(
        string methodName,
        IReadOnlyCollection<WorldLocation2Entry> worldLocations,
        IReadOnlyList<uint> expectedTerminalCreatureIds,
        IReadOnlyList<uint> expectedChecklistCreatureIds)
    {
        (TutorialMapScript script, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateScript(worldLocations);

        InvokePrivate(script, methodName);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds = mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(1 + expectedTerminalCreatureIds.Count + expectedChecklistCreatureIds.Count, adds.Count);

        AssertCreatedEntity(createdEntities[0], expectedCreatureId: worldLocations.First().Id == 51694u ? 73604u : 74778u, expectInteractionPrereq: false);

        for (int i = 0; i < expectedTerminalCreatureIds.Count; i++)
            AssertCreatedEntity(createdEntities[1 + i], expectedTerminalCreatureIds[i], expectInteractionPrereq: true);

        for (int i = 0; i < expectedChecklistCreatureIds.Count; i++)
        {
            CreatedEntity entity = createdEntities[1 + expectedTerminalCreatureIds.Count + i];
            AssertCreatedEntity(entity, expectedChecklistCreatureIds[i], expectInteractionPrereq: true);
            Assert.Contains(GetArguments(entity.Proxy, nameof(IWorldEntity.SetQuestChecklistIndex)), arguments =>
                arguments.Length == 1 && (byte)arguments[0] == i);
        }

        Assert.All(adds, invocation =>
        {
            Assert.IsAssignableFrom<IWorldEntity>(invocation.Arguments[0]);
            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(invocation.Arguments[1]);
            Assert.Equal(3460u, position.Info.Entry.Id);
        });
    }

    public static IEnumerable<object[]> DepartureFallbackCases()
    {
        yield return
        [
            "EnsureExileDepartureEntities",
            new[]
            {
                CreateWorldLocation(51694u, 10f, 20f, 30f),
                CreateWorldLocation(51695u, 11f, 20f, 31f),
                CreateWorldLocation(51696u, 12f, 20f, 32f),
                CreateWorldLocation(51697u, 13f, 20f, 33f)
            },
            new[] { ExileEverstarGroveDepartureTerminalCreatureId, ExileNorthernWildsDepartureTerminalCreatureId },
            new uint[] { 73677u, 73678u, 73679u, 73680u, 73681u, 73682u, 73683u, 73684u }
        ];

        yield return
        [
            "EnsureDominionDepartureEntities",
            new[]
            {
                CreateWorldLocation(52750u, 20f, 30f, 40f),
                CreateWorldLocation(52747u, 21f, 30f, 41f),
                CreateWorldLocation(52748u, 22f, 30f, 42f),
                CreateWorldLocation(52749u, 23f, 30f, 43f)
            },
            new[] { DominionCrimsonIsleDepartureTerminalCreatureId, DominionLevianBayDepartureTerminalCreatureId },
            new uint[] { 74759u, 74760u, 74761u, 74762u, 74763u, 74764u, 74765u, 74766u }
        ];
    }

    private static (TutorialMapScript Script, RecordingDispatchProxy<IBaseMap> MapProxy, IReadOnlyList<CreatedEntity> CreatedEntities) CreateScript(
        IReadOnlyCollection<WorldLocation2Entry> worldLocations)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));

        Queue<CreatedEntity> entities = new();
        entities.Enqueue(CreateEntity<INonPlayerEntity>());
        for (int i = 0; i < 10; i++)
            entities.Enqueue(CreateEntity<ISimpleCollidableEntity>());

        List<CreatedEntity> createdEntities = [];
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () =>
        {
            CreatedEntity entity = entities.Dequeue();
            createdEntities.Add(entity);
            return entity.Instance;
        });

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });
        mapProxy.SetMethodReturnFactory(nameof(IBaseMap.Search), () => Array.Empty<IWorldEntity>());

        var script = new TutorialMapScript(
            NullLogger<TutorialMapScript>.Instance,
            cinematicFactory,
            entityCacheManager,
            entityFactory,
            globalQuestManager,
            gameTableManager);

        typeof(TutorialMapScript)
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(script, map);

        return (script, mapProxy, createdEntities);
    }

    private static CreatedEntity CreateEntity<T>() where T : class, IWorldEntity
    {
        T instance = RecordingDispatchProxy<T>.Create(out RecordingDispatchProxy<T> proxy);
        return new CreatedEntity(instance, proxy);
    }

    private static void AssertCreatedEntity(CreatedEntity entity, uint expectedCreatureId, bool expectInteractionPrereq)
    {
        Assert.Contains(GetArguments(entity.Proxy, nameof(IWorldEntity.Initialise)), arguments =>
            arguments.Length == 1 && (uint)arguments[0] == expectedCreatureId);

        IReadOnlyList<object[]> flagUpdates = GetArguments(entity.Proxy, "set_CreateFlags");
        if (expectInteractionPrereq)
        {
            Assert.Contains(flagUpdates, arguments =>
                arguments.Length == 1 && ((EntityCreateFlag)arguments[0] & EntityCreateFlag.HasInteractionPrereq) != 0);
        }
        else
        {
            Assert.DoesNotContain(flagUpdates, arguments =>
                arguments.Length == 1 && ((EntityCreateFlag)arguments[0] & EntityCreateFlag.HasInteractionPrereq) != 0);
        }
    }

    private static IReadOnlyList<object[]> GetArguments(object proxy, string methodName)
    {
        object invocations = proxy.GetType().GetMethod("GetInvocations")!.Invoke(proxy, [methodName]);
        return Assert.IsAssignableFrom<IEnumerable>(invocations)
            .Cast<object>()
            .Select(invocation => (object[])invocation.GetType().GetProperty("Arguments")!.GetValue(invocation))
            .ToList();
    }

    private static void InvokePrivate(TutorialMapScript script, string methodName)
    {
        MethodInfo method = typeof(TutorialMapScript).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(script, []);
    }

    private static WorldLocation2Entry CreateWorldLocation(uint id, float x, float y, float z)
    {
        return new WorldLocation2Entry
        {
            Id        = id,
            WorldId   = 3460u,
            Position0 = x,
            Position1 = y,
            Position2 = z
        };
    }

    private static GameTable<WorldLocation2Entry> CreateGameTable(IReadOnlyCollection<WorldLocation2Entry> entries)
    {
        var table = (GameTable<WorldLocation2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldLocation2Entry>));

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(table, entries.ToArray());
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(table, new GameTableHeader { MaxId = entries.Max(e => e.Id) + 1u });

        int[] lookup = Enumerable.Repeat(-1, (int)entries.Max(e => e.Id) + 1).ToArray();
        int index = 0;
        foreach (WorldLocation2Entry entry in entries)
            lookup[entry.Id] = index++;

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(table, lookup);

        return table;
    }

    private sealed record CreatedEntity(IWorldEntity Instance, object Proxy);
}
