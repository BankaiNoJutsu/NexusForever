using System.Collections;
using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Session;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Map;

public class MovementRelocationQueueTests
{
    [Fact]
    public void RepeatedMovementUsesOneQueueEntryAndLatestTarget()
    {
        var map = new TestMap();
        var entity = map.Entity(1);
        for (int i = 1; i <= 1000; i++) map.EnqueueRelocate(entity, new Vector3(i, i, i), true);
        Assert.Equal(1, map.QueueCount);
        Assert.Equal(1, map.PendingCount);
        map.Tick();
        Assert.Equal(new Vector3(1000), Assert.Single(map.Moves).Position);
        Assert.Equal(0, map.QueueCount);
        Assert.Equal(0, map.PendingCount);
    }

    [Fact]
    public void ExplicitRelocationSealsMovementAndCannotEraseALaterPendingTarget()
    {
        var map = new TestMap(1);
        var entity = map.Entity(1);
        map.EnqueueRelocate(entity, Vector3.One, true);
        map.EnqueueRelocate(entity, new Vector3(10));
        map.EnqueueRelocate(entity, new Vector3(2), true);
        map.EnqueueRelocate(entity, new Vector3(3), true);
        Assert.Equal(3, map.QueueCount);
        map.Tick();
        Assert.Equal(1, map.PendingCount);
        map.Tick();
        Assert.Equal(1, map.PendingCount);
        map.Tick();
        Assert.Equal(new[] { Vector3.One, new Vector3(10), new Vector3(3) }, map.Moves.Select(m => m.Position));
        Assert.Equal(0, map.PendingCount);
    }

    [Fact]
    public void ExistingRelocationEntryPointKeepsEveryExplicitMove()
    {
        var map = new TestMap();
        var entity = map.Entity(1);
        for (int i = 1; i <= 3; i++) map.EnqueueRelocate(entity, new Vector3(i));
        map.Tick();
        Assert.Equal(new[] { new Vector3(1), new Vector3(2), new Vector3(3) }, map.Moves.Select(m => m.Position));
    }

    [Fact]
    public void ConcurrentProducersKeepOneEntryAndDoNotLoseFinalTarget()
    {
        var map = new TestMap();
        var entity = map.Entity(1);
        Parallel.For(0, 2000, i => map.EnqueueRelocate(entity, new Vector3(i, -i, i), true));
        map.EnqueueRelocate(entity, new Vector3(7, 8, 9), true);
        Assert.Equal(1, map.QueueCount);
        map.Tick();
        Assert.Equal(new Vector3(7, 8, 9), Assert.Single(map.Moves).Position);
        Assert.Equal(0, map.PendingCount);
    }

    [Fact]
    public async Task MovementQueuedDuringProcessingGetsANewAction()
    {
        var map = new TestMap(1);
        var entity = map.Entity(1);
        using var captured = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        map.RelocateHook = (_, _) =>
        {
            captured.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        map.EnqueueRelocate(entity, Vector3.One, true);
        Task tick = Task.Run(map.Tick);
        try
        {
            Assert.True(captured.Wait(TimeSpan.FromSeconds(10)));
            map.EnqueueRelocate(entity, new Vector3(2), true);
        }
        finally { release.Set(); }
        await tick;
        map.RelocateHook = null;
        Assert.Equal(1, map.QueueCount);
        map.Tick();
        Assert.Equal(new[] { Vector3.One, new Vector3(2) }, map.Moves.Select(m => m.Position));
        Assert.Equal(0, map.PendingCount);
    }

    [Fact]
    public void RemovalSealsMovementAndLaterStaleActionIsDiscarded()
    {
        var map = new TestMap();
        var entity = map.Entity(1);
        map.EnqueueRelocate(entity, Vector3.One, true);
        map.EnqueueRemove(entity);
        map.EnqueueRelocate(entity, new Vector3(2), true);
        map.Tick();
        Assert.Equal(Vector3.One, Assert.Single(map.Moves).Position);
        Assert.Equal(new[] { "move", "remove" }, map.Events);
        Assert.Equal(0, map.PendingCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachedOrTransferredEntityCannotBeMovedByOldMap(bool transferred)
    {
        var map = new TestMap();
        var entity = map.Entity(1);
        map.EnqueueRelocate(entity, Vector3.One, true);
        ((RecordingDispatchProxy<IGridEntity>)(object)entity).SetProperty(nameof(IGridEntity.Map), transferred ? new TestMap() : null);
        map.Tick();
        Assert.Empty(map.Moves);
        Assert.Equal(0, map.PendingCount);
    }

    [Fact]
    public void EntitiesWithSameGuidRetainIndependentPendingMoves()
    {
        var map = new TestMap();
        var first = map.Entity(1);
        var second = map.Entity(1);
        map.EnqueueRelocate(first, Vector3.One, true);
        map.EnqueueRelocate(second, new Vector3(2), true);
        map.Tick();
        Assert.Same(first, map.Moves[0].Entity);
        Assert.Same(second, map.Moves[1].Entity);
    }

    [Fact]
    public void BackloggedMovementDoesNotMultiplyQueueAheadOfAddAndRemove()
    {
        var map = new TestMap(100);
        var movers = Enumerable.Range(1, 170).Select(i => map.Entity((uint)i)).ToArray();
        for (int tick = 0; tick < 100; tick++)
            foreach (var mover in movers) map.EnqueueRelocate(mover, new Vector3(tick), true);
        var joining = map.Entity(1000);
        ((RecordingDispatchProxy<IGridEntity>)(object)joining).SetProperty(nameof(IGridEntity.Map), null);
        map.EnqueueAdd(joining, new MapPosition { Position = Vector3.Zero });
        map.EnqueueRemove(map.Entity(1001));
        Assert.Equal(172, map.QueueCount);
        map.Tick();
        Assert.DoesNotContain("add", map.Events);
        map.Tick();
        Assert.Equal("add", map.Events[^2]);
        Assert.Equal("remove", map.Events[^1]);
        Assert.Equal(170, map.Moves.Count);
        Assert.All(map.Moves, m => Assert.Equal(new Vector3(99), m.Position));
        Assert.Equal(0, map.QueueCount);
        Assert.Equal(0, map.PendingCount);
    }

    [Theory]
    [InlineData("current")]
    [InlineData("invisible")]
    [InlineData("reused")]
    [InlineData("dismounted")]
    [InlineData("new-admission")]
    public void VehicleMovementUsesTheSeatOwnerAtEnqueueAndExecution(string state)
    {
        var map = new TestMap();
        typeof(BaseMap).GetProperty(nameof(BaseMap.File))!.SetValue(map, new MapFile());
        var rider = RecordingDispatchProxy<IAdmissionTestPlayer>.Create(out var player);
        var admission = new PlayerMapAdmissionState();
        admission.Queue();
        player.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionSyncRoot), admission.SyncRoot);
        player.SetMethodReturnFactory("get_MapAdmissionGeneration", () => admission.Generation);
        player.SetMethodHandler(nameof(IPlayerMapAdmission.IsMapAdmissionCurrent), args => admission.IsCurrent((ulong)args[0]));
        player.SetProperty(nameof(IPlayer.Map), map);
        player.SetProperty(nameof(IPlayer.Guid), 42u);
        map.Register(rider);
        player.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        player.SetMethodHandler(nameof(IPlayer.SetPlatform), args =>
        {
            player.SetProperty(nameof(IPlayer.PlatformGuid), (args[0] as IWorldEntity)?.Guid);
            return null;
        });

        var vehicle = new VehicleEntity(RecordingDispatchProxy<IMovementManager>.Create(out _));
        typeof(GridEntity).GetProperty(nameof(IGridEntity.Map))!.SetValue(vehicle, map);
        typeof(GridEntity).GetProperty(nameof(IGridEntity.Guid))!.SetValue(vehicle, 55u);
        typeof(VehicleEntity).GetProperty(nameof(VehicleEntity.VehicleEntry))!.SetValue(vehicle, new UnitVehicleEntry { NumberPilots = 1 });
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity)
            .GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vehicle);
        visible[rider.Guid] = rider;
        vehicle.EnqueuePassengerAdd(rider, VehicleSeatType.Pilot, 0);
        vehicle.Update(.01);
        Assert.Equal(55u, rider.PlatformGuid);

        if (state == "invisible") visible.Remove(rider.Guid);
        if (state == "reused")
        {
            var replacement = RecordingDispatchProxy<IPlayer>.Create(out var replacementPlayer);
            replacementPlayer.SetProperty(nameof(IPlayer.Map), map);
            replacementPlayer.SetProperty(nameof(IPlayer.Guid), rider.Guid);
            visible[rider.Guid] = replacement;
            map.Register(replacement);
        }

        var destination = new Vector3(10, 0, 12);
        vehicle.OnRelocate(destination);
        if (state == "dismounted") vehicle.PassengerRemove(rider);
        if (state == "new-admission") admission.Queue();
        map.Tick();

        Assert.Equal(destination, vehicle.Position);
        if (state is "current" or "invisible")
        {
            var moved = Assert.Single(map.Moves);
            Assert.Same(rider, moved.Entity);
            Assert.Equal(destination, moved.Position);
        }
        else Assert.Empty(map.Moves);
    }

    private sealed class TestMap : BaseMap
    {
        public List<(IGridEntity Entity, Vector3 Position)> Moves { get; } = [];
        public List<string> Events { get; } = [];
        public Action<IGridEntity, Vector3> RelocateHook { get; set; }
        public int QueueCount => pendingActions.Count;
        public int PendingCount => ((IDictionary)typeof(BaseMap).GetField("pendingRelocations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!).Count;

        public TestMap(uint threshold = 100) : base(RecordingDispatchProxy<IEntityFactory>.Create(out _),
            RecordingDispatchProxy<IPublicEventManager>.Create(out _), sharedConfiguration: Configuration(threshold))
        {
            typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, new WorldEntry { Id = 426 });
        }

        public IGridEntity Entity(uint guid)
        {
            var entity = RecordingDispatchProxy<IGridEntity>.Create(out var proxy);
            proxy.SetProperty(nameof(IGridEntity.Guid), guid);
            proxy.SetProperty(nameof(IGridEntity.Map), this);
            entities[guid] = entity;
            return entity;
        }

        public void Tick() => typeof(BaseMap).GetMethod("ProcessGridActions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, null);
        public void Register(IGridEntity entity) => entities[entity.Guid] = entity;
        protected override void RelocateEntity(IGridEntity entity, Vector3 vector)
        {
            Moves.Add((entity, vector));
            Events.Add("move");
            RelocateHook?.Invoke(entity, vector);
            entity.OnRelocate(vector);
        }
        protected override void AddEntity(IGridEntity entity, Vector3 vector) => Events.Add("add");
        protected override void RemoveEntity(IGridEntity entity)
        {
            Events.Add("remove");
            ((RecordingDispatchProxy<IGridEntity>)(object)entity).SetProperty(nameof(IGridEntity.Map), null);
        }
        protected override void SpawnGrid(uint x, uint z) { }
        private static ISharedConfiguration Configuration(uint threshold)
        {
            var config = new SharedConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridActionThreshold"] = threshold.ToString(), ["Realm:Map:GridUnloadTimer"] = "600"
            }).Build());
            config.Initialise<TestConfiguration>();
            return config;
        }
    }
    private sealed class TestConfiguration { public RealmConfig Realm { get; set; } }
}
