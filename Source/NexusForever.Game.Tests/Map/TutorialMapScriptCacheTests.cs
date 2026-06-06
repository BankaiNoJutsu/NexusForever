using System.Collections;
using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;

namespace NexusForever.Game.Tests.Map;

public class TutorialMapScriptCacheTests
{
    [Fact]
    public void HasActiveCombatLaneAnchors_UsesCachedAnchorWhenActiveEntityIsNotYetPresent()
    {
        TutorialMapScript script = CreateScriptWithCachedEntities(
            new EntityModel
            {
                World = 3460,
                Creature = 73463u,
                X = -120f,
                Y = 0f,
                Z = 440f
            });

        bool result = InvokeHasActiveCombatLaneAnchors(script, (new Vector3(-120f, 0f, 440f), 5f, 73463u));

        Assert.True(result);
    }

    [Fact]
    public void HasActiveCombatLaneAnchors_ReturnsFalseWhenCacheDoesNotContainAnchor()
    {
        TutorialMapScript script = CreateScriptWithCachedEntities();

        bool result = InvokeHasActiveCombatLaneAnchors(script, (new Vector3(-120f, 0f, 440f), 5f, 73463u));

        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(CombatLaneFinalWaveCases))]
    public void EnsureCombatSimulationEntities_WhenImportedAnchorsExist_SpawnsMissingFinalWave(
        string methodName,
        IReadOnlyCollection<WorldLocation2Entry> worldLocations,
        IReadOnlyCollection<EntityModel> cachedAnchors,
        uint[] expectedCreatureIds)
    {
        (TutorialMapScript script, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateCombatLaneScript(worldLocations, cachedAnchors);

        InvokePrivate(script, methodName);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds = mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(expectedCreatureIds.Length, adds.Count);
        Assert.Equal(expectedCreatureIds.Length, createdEntities.Count);

        for (int i = 0; i < expectedCreatureIds.Length; i++)
        {
            CreatedEntity entity = createdEntities[i];
            Assert.Contains(GetArguments(entity.Proxy, nameof(IWorldEntity.Initialise)), arguments =>
                arguments.Length == 1 && (uint)arguments[0] == expectedCreatureIds[i]);
        }
    }

    [Fact]
    public void EnsureCryopodNpcs_WhenImportedConversationNpcExists_BackfillsReceiverAndProjectors()
    {
        IWorldEntity activeConversationNpc = CreateActiveEntity(73663u, new Vector3(10f, 20f, 30f));
        (TutorialMapScript script, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateCryopodScript(
                new[]
                {
                    CreateWorldLocation(51711u, 10f, 20f, 30f),
                    CreateWorldLocation(51712u, 20f, 20f, 40f),
                    CreateWorldLocation(52819u, 30f, 20f, 50f),
                    CreateWorldLocation(51741u, 40f, 20f, 60f),
                    CreateWorldLocation(52905u, 50f, 20f, 70f)
                },
                new[] { activeConversationNpc });

        InvokePrivate(script, "EnsureCryopodNpcs");

        uint[] expectedCreatureIds = [73664u, 73422u, 73662u, 73421u, 73741u, 73741u];
        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds = mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(expectedCreatureIds.Length, adds.Count);
        Assert.Equal(expectedCreatureIds.Length, createdEntities.Count);

        for (int i = 0; i < expectedCreatureIds.Length; i++)
        {
            CreatedEntity entity = createdEntities[i];
            Assert.Contains(GetArguments(entity.Proxy, nameof(IWorldEntity.Initialise)), arguments =>
                arguments.Length == 1 && (uint)arguments[0] == expectedCreatureIds[i]);
        }

        AssertProjectorFallback(createdEntities[^2]);
        AssertProjectorFallback(createdEntities[^1]);
    }

    public static IEnumerable<object[]> CombatLaneFinalWaveCases()
    {
        yield return
        [
            "EnsureExileCombatSimulationEntities",
            new[]
            {
                CreateWorldLocation(51739u, -140f, -874f, 340f),
                CreateWorldLocation(51662u, -70f, -868f, 384f),
                CreateWorldLocation(51663u, -88f, -868f, 414f),
                CreateWorldLocation(51664u, -119f, -868f, 425f),
                CreateWorldLocation(51671u, -106f, -864f, 502f),
                CreateWorldLocation(52753u, -113f, -864f, 471f),
                CreateWorldLocation(51740u, -120f, -874f, 535f)
            },
            new[]
            {
                CreateEntityModel(73463u, -70f, -868f, 384f),
                CreateEntityModel(73667u, -88f, -868f, 414f),
                CreateEntityModel(73668u, -119f, -868f, 425f),
                CreateEntityModel(73494u, -106f, -864f, 502f),
                CreateEntityModel(73494u, -113f, -864f, 471f)
            },
            new uint[] { 73492u, 73567u, 73492u }
        ];

        yield return
        [
            "EnsureDominionCombatSimulationEntities",
            new[]
            {
                CreateWorldLocation(52898u, 140f, -874f, 340f),
                CreateWorldLocation(52899u, 70f, -868f, 384f),
                CreateWorldLocation(52900u, 88f, -868f, 414f),
                CreateWorldLocation(52901u, 119f, -868f, 425f),
                CreateWorldLocation(52902u, 106f, -864f, 502f),
                CreateWorldLocation(52903u, 113f, -864f, 471f),
                CreateWorldLocation(53015u, 120f, -874f, 535f)
            },
            new[]
            {
                CreateEntityModel(73463u, 70f, -868f, 384f),
                CreateEntityModel(73667u, 88f, -868f, 414f),
                CreateEntityModel(73668u, 119f, -868f, 425f),
                CreateEntityModel(74862u, 106f, -864f, 502f),
                CreateEntityModel(74862u, 113f, -864f, 471f)
            },
            new uint[] { 73473u, 73566u, 73473u }
        ];
    }

    private static TutorialMapScript CreateScriptWithCachedEntities(params EntityModel[] cachedEntities)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out RecordingDispatchProxy<IEntityCacheManager> cacheManagerProxy);

        cacheManagerProxy.SetMethodReturn(
            nameof(IEntityCacheManager.GetEntityCache),
            new EntityCache(cachedEntities.ToImmutableList()));

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
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(script, map);

        return script;
    }

    private static (TutorialMapScript Script, RecordingDispatchProxy<IBaseMap> MapProxy, IReadOnlyList<CreatedEntity> CreatedEntities) CreateCombatLaneScript(
        IReadOnlyCollection<WorldLocation2Entry> worldLocations,
        IReadOnlyCollection<EntityModel> cachedAnchors)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out RecordingDispatchProxy<IEntityCacheManager> cacheManagerProxy);

        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));
        cacheManagerProxy.SetMethodReturn(
            nameof(IEntityCacheManager.GetEntityCache),
            new EntityCache(cachedAnchors.ToImmutableList()));

        Queue<CreatedEntity> entities = new();
        for (int i = 0; i < 3; i++)
            entities.Enqueue(CreateEntity<INonPlayerEntity>());

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
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(script, map);

        return (script, mapProxy, createdEntities);
    }

    private static (TutorialMapScript Script, RecordingDispatchProxy<IBaseMap> MapProxy, IReadOnlyList<CreatedEntity> CreatedEntities) CreateCryopodScript(
        IReadOnlyCollection<WorldLocation2Entry> worldLocations,
        IReadOnlyCollection<IWorldEntity> activeEntities)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out RecordingDispatchProxy<IEntityCacheManager> cacheManagerProxy);

        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));
        cacheManagerProxy.SetMethodReturn(
            nameof(IEntityCacheManager.GetEntityCache),
            new EntityCache(ImmutableList<EntityModel>.Empty));

        Queue<CreatedEntity> entities = new();
        for (int i = 0; i < 4; i++)
            entities.Enqueue(CreateEntity<INonPlayerEntity>());
        for (int i = 0; i < 2; i++)
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
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args => FilterActiveEntities(args, activeEntities));

        var script = new TutorialMapScript(
            NullLogger<TutorialMapScript>.Instance,
            cinematicFactory,
            entityCacheManager,
            entityFactory,
            globalQuestManager,
            gameTableManager);

        typeof(TutorialMapScript)
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(script, map);

        return (script, mapProxy, createdEntities);
    }

    private static CreatedEntity CreateEntity<T>() where T : class, IWorldEntity
    {
        T instance = RecordingDispatchProxy<T>.Create(out RecordingDispatchProxy<T> proxy);
        return new CreatedEntity(instance, proxy);
    }

    private static IWorldEntity CreateActiveEntity(uint creatureId, Vector3 position)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IGridEntity.Position), position);
        return entity;
    }

    private static IWorldEntity[] FilterActiveEntities(object[] args, IReadOnlyCollection<IWorldEntity> activeEntities)
    {
        object searchCheck = args.Length >= 3 ? args[2] : null;
        return activeEntities
            .Where(entity => searchCheck == null || (bool)searchCheck.GetType().GetMethod(nameof(ISearchCheck<IWorldEntity>.CheckEntity))!.Invoke(searchCheck, [entity]))
            .ToArray();
    }

    private static void AssertProjectorFallback(CreatedEntity entity)
    {
        Assert.Contains(GetArguments(entity.Proxy, nameof(IWorldEntity.Initialise)), arguments =>
            arguments.Length == 1 && (uint)arguments[0] == 73741u);
        Assert.Contains(GetArguments(entity.Proxy, "set_CreateFlags"), arguments =>
            arguments.Length == 1 && ((EntityCreateFlag)arguments[0] & EntityCreateFlag.HasInteractionPrereq) != 0);
    }

    private static bool InvokeHasActiveCombatLaneAnchors(
        TutorialMapScript script,
        params (Vector3 Position, float Radius, uint CreatureId)[] anchors)
    {
        MethodInfo method = typeof(TutorialMapScript).GetMethod(
            "HasActiveCombatLaneAnchors",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        return Assert.IsType<bool>(method.Invoke(script, [anchors]));
    }

    private static void InvokePrivate(TutorialMapScript script, string methodName)
    {
        MethodInfo method = typeof(TutorialMapScript).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(script, []);
    }

    private static IReadOnlyList<object[]> GetArguments(object proxy, string methodName)
    {
        object invocations = proxy.GetType().GetMethod("GetInvocations")!.Invoke(proxy, [methodName]);
        return Assert.IsAssignableFrom<IEnumerable>(invocations)
            .Cast<object>()
            .Select(invocation => (object[])invocation.GetType().GetProperty("Arguments")!.GetValue(invocation))
            .ToList();
    }

    private static EntityModel CreateEntityModel(uint creatureId, float x, float y, float z)
    {
        return new EntityModel
        {
            World    = 3460,
            Creature = creatureId,
            X        = x,
            Y        = y,
            Z        = z
        };
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
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, entries.ToArray());
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = entries.Max(e => e.Id) + 1u });

        int[] lookup = Enumerable.Repeat(-1, (int)entries.Max(e => e.Id) + 1).ToArray();
        int index = 0;
        foreach (WorldLocation2Entry entry in entries)
            lookup[entry.Id] = index++;

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }

    private sealed record CreatedEntity(IWorldEntity Instance, object Proxy);
}
