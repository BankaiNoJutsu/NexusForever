using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Entity.Trigger;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Entity;

public class VolumeGridTriggerEntityTests
{
    [Fact]
    public void OnAddToMap_CountsPlayersAlreadyInsideTriggerRange()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(3f, 0f, 0f));
        var map = new TestMap(50f, publicEventManager, player);

        var trigger = new VolumeGridTriggerEntity(scriptManager);
        trigger.Initialise(8242u, 10f, 8283u);

        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, invocation.Arguments[0]);
        Assert.Equal(8283u, invocation.Arguments[1]);
        Assert.Equal(1, invocation.Arguments[2]);
    }

    [Fact]
    public void CheckEntityInRange_DecrementsWhenPlayerLeavesTriggerRange()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, Vector3.Zero, out var playerProxy);
        var map = new TestMap(50f, publicEventManager);

        var trigger = new VolumeGridTriggerEntity(scriptManager);
        trigger.Initialise(8242u, 10f, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        trigger.CheckEntityInRange(player);
        playerProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(20f, 0f, 0f));
        trigger.CheckEntityInRange(player);

        Assert.Collection(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            invocation =>
            {
                Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, invocation.Arguments[0]);
                Assert.Equal(8283u, invocation.Arguments[1]);
                Assert.Equal(1, invocation.Arguments[2]);
            },
            invocation =>
            {
                Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, invocation.Arguments[0]);
                Assert.Equal(8283u, invocation.Arguments[1]);
                Assert.Equal(-1, invocation.Arguments[2]);
            });
    }

    [Fact]
    public void CheckEntityInRange_DoesNotDuplicateVolumeCountWhilePlayerStaysInside()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, Vector3.Zero);
        var map = new TestMap(50f, publicEventManager);

        var trigger = new VolumeGridTriggerEntity(scriptManager);
        trigger.Initialise(8242u, 10f, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        trigger.CheckEntityInRange(player);
        trigger.CheckEntityInRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, invocation.Arguments[0]);
        Assert.Equal(8283u, invocation.Arguments[1]);
        Assert.Equal(1, invocation.Arguments[2]);
    }

    [Fact]
    public void OnAddToMap_IgnoresZeroObjectIdAndNonPlayers()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(3f, 0f, 0f));
        IGridEntity nonPlayer = CreateGridEntity(8u, new Vector3(4f, 0f, 0f));

        var zeroObjectTrigger = new VolumeGridTriggerEntity(scriptManager);
        zeroObjectTrigger.Initialise(8242u, 10f, 0u);
        zeroObjectTrigger.OnAddToMap(new TestMap(50f, publicEventManager, player), 99u, Vector3.Zero);

        var nonPlayerTrigger = new VolumeGridTriggerEntity(scriptManager);
        nonPlayerTrigger.Initialise(8243u, 10f, 8283u);
        nonPlayerTrigger.OnAddToMap(new TestMap(50f, publicEventManager, nonPlayer), 100u, Vector3.Zero);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void TurnstileTrigger_CountsPlayerEntryOnce()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(3f, 0f, 0f));
        var map = new TestMap(50f, publicEventManager, player);

        var trigger = new TurnstileTriggerEntity(scriptManager);
        trigger.Initialise(8242u, 10f, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.Turnstile, invocation.Arguments[0]);
        Assert.Equal(8283u, invocation.Arguments[1]);
        Assert.Equal(1, invocation.Arguments[2]);
    }

    [Fact]
    public void TurnstileTrigger_DoesNotDuplicateEntryOrEmitLeaveDelta()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, Vector3.Zero, out var playerProxy);
        var map = new TestMap(50f, publicEventManager);

        var trigger = new TurnstileTriggerEntity(scriptManager);
        trigger.Initialise(8242u, 10f, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        trigger.CheckEntityInRange(player);
        trigger.CheckEntityInRange(player);
        playerProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(20f, 0f, 0f));
        trigger.CheckEntityInRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.Turnstile, invocation.Arguments[0]);
        Assert.Equal(8283u, invocation.Arguments[1]);
        Assert.Equal(1, invocation.Arguments[2]);
    }

    [Fact]
    public void TurnstileTrigger_IgnoresZeroObjectIdAndNonPlayers()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(3f, 0f, 0f));
        IGridEntity nonPlayer = CreateGridEntity(8u, new Vector3(4f, 0f, 0f));

        var zeroObjectTrigger = new TurnstileTriggerEntity(scriptManager);
        zeroObjectTrigger.Initialise(8242u, 10f, 0u);
        zeroObjectTrigger.OnAddToMap(new TestMap(50f, publicEventManager, player), 99u, Vector3.Zero);

        var nonPlayerTrigger = new TurnstileTriggerEntity(scriptManager);
        nonPlayerTrigger.Initialise(8243u, 10f, 8283u);
        nonPlayerTrigger.OnAddToMap(new TestMap(50f, publicEventManager, nonPlayer), 100u, Vector3.Zero);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void WorldLocationTrigger_UsesHorizontalRadiusWithHalfHitRadius()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(10.4f, 1f, 0f), hitRadius: 1f);
        var map = new TestMap(50f, publicEventManager, player);

        WorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(scriptManager, radius: 10f, maxVerticalDistance: 2f);
        trigger.Initialise(51735u, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ParticipantsInTriggerVolume, invocation.Arguments[0]);
        Assert.Equal(8283u, invocation.Arguments[1]);
        Assert.Equal(1, invocation.Arguments[2]);
    }

    [Fact]
    public void WorldLocationTrigger_RejectsPlayersOutsideVerticalClamp()
    {
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IPlayer player = CreatePlayer(7u, new Vector3(3f, 3f, 0f), hitRadius: 1f);
        var map = new TestMap(50f, publicEventManager, player);

        WorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(scriptManager, radius: 10f, maxVerticalDistance: 2f);
        trigger.Initialise(51735u, 8283u);
        trigger.OnAddToMap(map, 99u, Vector3.Zero);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position)
    {
        return CreatePlayer(guid, position, out _, 0f);
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position, float hitRadius)
    {
        return CreatePlayer(guid, position, out _, hitRadius);
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreatePlayer(guid, position, out playerProxy, 0f);
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position, out RecordingDispatchProxy<IPlayer> playerProxy, float hitRadius)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out var questManagerProxy);
        questManagerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<NexusForever.Game.Abstract.Quest.IQuest>());

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        playerProxy.SetProperty(nameof(IGridEntity.Position), position);
        playerProxy.SetProperty(nameof(IUnitEntity.HitRadius), hitRadius);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static IGridEntity CreateGridEntity(uint guid, Vector3 position)
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out var entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IGridEntity.Position), position);
        return entity;
    }

    private static WorldLocationVolumeGridTriggerEntity CreateWorldLocationTrigger(IScriptManager scriptManager, float radius, float maxVerticalDistance)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out var gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
        {
            Id = 51735u,
            Radius = radius,
            MaxVerticalDistance = maxVerticalDistance
        }));

        return new WorldLocationVolumeGridTriggerEntity(
            scriptManager,
            gameTableManager,
            NullLogger<WorldLocationVolumeGridTriggerEntity>.Instance);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries);

        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(idField.GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        for (int i = 0; i < entries.Length; i++)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entries[i]));
            lookup[id] = i;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }

    private sealed class TestMap(float visionRange, IPublicEventManager publicEventManager, params IGridEntity[] entities) : IBaseMap
    {
        public WorldEntry Entry { get; } = new();

        public float? VisionRange { get; } = visionRange;

        public MapFile File => null;

        public IPublicEventManager PublicEventManager { get; } = publicEventManager;

        public void Update(double lastTick)
        {
        }

        public void Initialise(WorldEntry entry) => throw new NotSupportedException();

        public void EnqueueAdd(IGridEntity entity, IMapPosition position) => throw new NotSupportedException();

        public bool CanEnter(IGridEntity entity, IMapPosition position) => throw new NotSupportedException();

        public GenericError? CanEnter(IPlayer entity, IMapPosition position) => throw new NotSupportedException();

        public string WriteDebugInformation() => string.Empty;

        public void EnqueueRemove(IGridEntity entity) => throw new NotSupportedException();

        public void EnqueueRelocate(IGridEntity entity, Vector3 position) => throw new NotSupportedException();

        public IEnumerable<T> Search<T>(Vector3 vector, float? radius, ISearchCheck<T> check) where T : IGridEntity
        {
            return entities
                .OfType<T>()
                .Where(check.CheckEntity)
                .ToList();
        }

        public void GridSearch(Vector3 vector, float? radius, out List<IMapGrid> intersectedGrids)
        {
            intersectedGrids = [];
        }

        public T GetEntity<T>(uint guid) where T : IGridEntity => default;

        public void EnqueueToAll(IWritable message) => throw new NotSupportedException();

        public void GridAddVisiblePlayer(uint gridX, uint gridZ) => throw new NotSupportedException();

        public void GridRemoveVisiblePlayer(uint gridX, uint gridZ) => throw new NotSupportedException();

        public float? GetTerrainHeight(float x, float z) => throw new NotSupportedException();

        public ResurrectionType GetResurrectionType() => ResurrectionType.None;

        public void OnEnterZone(IWorldEntity entity, uint zone) => throw new NotSupportedException();

        public void OnPublicEventFinish(IPublicEvent publicEvent, IPublicEventTeam publicEventTeam) => throw new NotSupportedException();
    }
}
