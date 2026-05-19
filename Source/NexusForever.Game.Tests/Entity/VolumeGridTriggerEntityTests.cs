using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Entity.Trigger;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
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

    private static IPlayer CreatePlayer(uint guid, Vector3 position)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        playerProxy.SetProperty(nameof(IGridEntity.Position), position);
        return player;
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
