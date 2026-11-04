using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Map;

public interface IAdmissionTestPlayer : IPlayer, IPlayerMapAdmission { }

public sealed class MapAdmissionQueueTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledAndSupersededBaseMapAddsNeverActivateOrAdd(bool superseded)
    {
        var player = new TestPlayer();
        var map = new TestMap();
        map.EnqueueAdd(player.Player, Position());
        if (superseded) player.State.Queue(); else player.State.Cancel();
        map.Tick();
        Assert.Equal(0, map.SpawnCount);
        Assert.Empty(map.Added);
        Assert.Equal(0, map.QueueCount);
    }

    [Fact]
    public void CancellationAfterGridActivationCannotReachGraphInsertion()
    {
        var player = new TestPlayer();
        var map = new TestMap { OnSpawn = player.State.Cancel };
        map.EnqueueAdd(player.Player, Position());
        map.Tick();
        Assert.Equal(1, map.SpawnCount);
        Assert.Empty(map.Added);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachedTransferFailsOnceWhenDestinationGridCannotAdmit(bool activationFault)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, session);
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Game.Abstract.Identity { RealmId = 1, Id = 42 });
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.MovementManager))!.SetValue(player, RecordingDispatchProxy<IMovementManager>.Create(out _));
        var source = RecordingDispatchProxy<IBaseMap>.Create(out var sourceProxy);
        var mapProperty = typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!;
        mapProperty.SetValue(player, source);
        sourceProxy.SetMethodHandler(nameof(IBaseMap.EnqueueRemove), _ => { mapProperty.SetValue(player, null); return null; });

        int spawnAttempts = 0;
        var map = new TestMap { OnSpawn = () =>
        {
            if (activationFault && ++spawnAttempts == 1) throw new InvalidOperationException("fixture grid activation failure");
        }};
        var mapManager = RecordingDispatchProxy<IMapManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IMapManager.AddToMap), args =>
        {
            map.EnqueueAdd((IPlayer)args[0], (IMapPosition)args[1]);
            return null;
        });
        typeof(Player).GetField("mapManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, mapManager);

        var position = Position();
        typeof(BaseMap).GetProperty(nameof(BaseMap.Entry))!.SetValue(map, position.Info.Entry);
        if (!activationFault) map.HoldGridUnloading(position.Position);
        player.TeleportTo(position);
        Assert.Null(player.Map);
        Assert.False(player.CanTeleport());
        Assert.True(player.IsLoading);
        var admission = (IPlayerMapAdmission)player;
        ulong generation = admission.MapAdmissionGeneration;
        map.Tick(); // The destination now owns the add after source removal.

        if (!activationFault)
        {
            for (int attempt = 0; attempt < 5; attempt++) map.Tick();
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(call => call.Arguments[0]).OfType<ServerForceKick>());
            Assert.Equal(1, map.QueueCount);
        }

        var following = new TestPlayer();
        var nextPosition = Position();
        nextPosition.Position = new Vector3(3 * MapDefines.GridSize, 0, 0);
        map.EnqueueAdd(following.Player, nextPosition);
        map.Tick();
        map.Tick();
        var kick = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerForceKick>());
        Assert.Equal(ForceKickReason.WorldDisconnect, kick.Reason);
        Assert.False(player.IsLoading);
        Assert.True(player.CanTeleport());
        Assert.False(admission.IsMapAdmissionCurrent(generation));
        Assert.False(admission.IsMapAdmissionCurrent(admission.BeginMapAdmission()));
        Assert.Same(following.Player, Assert.Single(map.Added));
        Assert.Equal(0, map.QueueCount);
    }

    [Fact]
    public void PendingRemovalRetryKeepsOriginalGenerationAndDropsCanceledPlayer()
    {
        var player = new TestPlayer();
        var map = new TestMap();
        player.Proxy.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IBaseMap>.Create(out _));
        map.EnqueueAdd(player.Player, Position());
        map.Tick();
        Assert.Equal(1, map.QueueCount);
        player.State.Cancel();
        player.Proxy.SetProperty(nameof(IPlayer.Map), null);
        map.Tick();
        Assert.Equal(0, map.QueueCount);
        Assert.Empty(map.Added);
    }

    [Fact]
    public void ValidPendingRemovalConvertsOnceAndReplacementPlayerIsUnaffected()
    {
        var old = new TestPlayer();
        var replacement = new TestPlayer();
        var map = new TestMap();
        old.Proxy.SetProperty(nameof(IPlayer.Map), map);
        map.EnqueueAdd(old.Player, Position());
        old.State.Cancel();
        replacement.Proxy.SetProperty(nameof(IPlayer.Map), map);
        map.EnqueueAdd(replacement.Player, Position());
        map.Tick();
        replacement.Proxy.SetProperty(nameof(IPlayer.Map), null);
        map.Tick();
        map.Tick();
        Assert.Same(replacement.Player, Assert.Single(map.Added));
    }

    [Fact]
    public void MapManagerDiscardsOldGenerationBeforeCreatingMap()
    {
        var player = new TestPlayer();
        IMapFactory factory = RecordingDispatchProxy<IMapFactory>.Create(out var proxy);
        var manager = new MapManager(factory);
        manager.AddToMap(player.Player, Position());
        player.State.Queue();
        manager.Update(0);
        Assert.Empty(proxy.GetInvocations(nameof(IMapFactory.CreateMap)));
    }

    [Fact]
    public void CanceledInstanceQueueCannotCreateInstanceOrReserveCount()
    {
        var player = new TestPlayer();
        var map = new TestInstancedMap();
        map.EnqueueAdd(player.Player, Position());
        player.State.Cancel();
        map.Update(0);
        Assert.Equal(0, map.Created);
        Assert.Empty(map.ManagerProxy.GetInvocations(nameof(IMapManager.IncreaseInstanceCount)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnloadingInstanceRetryPreservesAdmissionOwnership(bool cancel)
    {
        var player = new TestPlayer();
        var map = new TestInstancedMap();
        map.InstanceProxy.SetProperty(nameof(IMapInstance.UnloadStatus), MapUnloadStatus.UnloadingPlayers);
        map.EnqueueAdd(player.Player, Position());
        map.Update(0);
        Assert.Equal(1, map.Created);
        Assert.Empty(map.InstanceProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        if (cancel) player.State.Cancel();
        map.InstanceProxy.SetProperty(nameof(IMapInstance.UnloadStatus), null);
        map.Update(0);
        Assert.Equal(cancel ? 0 : 1, map.InstanceProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
        Assert.Equal(1, map.Created);
    }

    [Theory]
    [InlineData("denied")]
    [InlineData("admission-fault")]
    [InlineData("creation-fault")]
    [InlineData("superseded-fault")]
    public void AFailedInstanceEntryPreservesOtherTransfers(string outcome)
    {
        var waiting = new TestPlayer();
        var refused = new TestPlayer();
        var following = new TestPlayer();
        var map = new TestInstancedMap();
        map.InstanceProxy.SetProperty(nameof(IMapInstance.UnloadStatus), MapUnloadStatus.UnloadingPlayers);
        MapPosition otherDestination = map.AddDestination(out var otherInstance);
        if (outcome == "creation-fault") map.FailCreationFor = refused.Player;
        otherInstance.SetMethodHandler(nameof(IMap.CanEnter), args =>
        {
            if (!ReferenceEquals(args[0], refused.Player))
                return null;
            if (outcome == "superseded-fault") refused.State.Queue();
            if (outcome is "admission-fault" or "superseded-fault")
                throw new InvalidOperationException("fixture admission failure");
            return GenericError.InstanceFull;
        });
        map.EnqueueAdd(waiting.Player, Position());
        map.EnqueueAdd(refused.Player, otherDestination);
        map.EnqueueAdd(following.Player, otherDestination);

        map.Update(0);
        var failures = refused.Proxy.GetInvocations(nameof(IPlayer.OnTeleportToFailed));
        if (outcome == "superseded-fault")
        {
            Assert.Empty(failures);
        }
        else
        {
            Assert.Equal(outcome == "denied" ? GenericError.InstanceFull : GenericError.InstanceInvalidDestination,
                Assert.Single(failures).Arguments[0]);
        }

        int followingAddsOnFirstTick = otherInstance.GetInvocations(nameof(IMap.EnqueueAdd)).Count;
        Assert.Empty(map.InstanceProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        map.InstanceProxy.SetProperty(nameof(IMapInstance.UnloadStatus), null);
        map.Update(0);
        map.Update(0);

        Assert.Same(waiting.Player, Assert.Single(map.InstanceProxy.GetInvocations(nameof(IMap.EnqueueAdd))).Arguments[0]);
        Assert.Same(following.Player, Assert.Single(otherInstance.GetInvocations(nameof(IMap.EnqueueAdd))).Arguments[0]);
        Assert.Equal(1, followingAddsOnFirstTick);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InstanceCapacityCountsOnlyCurrentAddOrPendingReservations(bool pending, bool cancel)
    {
        var reserved = new TestPlayer();
        var fresh = new TestPlayer();
        var map = new CapacityMap();
        if (pending) reserved.Proxy.SetProperty(nameof(IPlayer.Map), map);
        map.EnqueueAdd(reserved.Player, Position());
        if (cancel) reserved.State.Cancel();
        Assert.Equal(cancel ? null : GenericError.InstanceFull, map.CanEnter(fresh.Player, Position()));
    }

    [Fact]
    public async Task AdmissionGateSerializesCancellationAgainstAnInProgressAdd()
    {
        var player = new TestPlayer();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task admission = Task.Run(() => PlayerMapAdmissionGuard.Run(player.Player, player.State.Generation, () =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
        }));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task cancellation = Task.Run(() =>
        {
            cancellationStarted.SetResult();
            lock (player.State.SyncRoot) player.State.Cancel();
        });
        await cancellationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotSame(cancellation, await Task.WhenAny(cancellation, Task.Delay(50)));
        release.Set();
        await Task.WhenAll(admission, cancellation).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(PlayerMapAdmissionGuard.Run(player.Player, player.State.Generation, () => throw new InvalidOperationException("Canceled admission ran")));
    }

    [Fact]
    public void DeferredEntryIsReleasedAfterTheOuterVirtualAddReturns()
    {
        var player = new TestPlayer();
        var order = new List<string>();
        var map = new TestMap { OnAdded = () =>
        {
            player.State.BeginBootstrap();
            Assert.False(player.State.RequestEntry(true));
            order.Add("map-instance-and-script-callbacks");
        }};
        player.Proxy.SetMethodHandler(nameof(IPlayerMapAdmission.CompleteMapAdmission), args =>
        {
            Assert.True(player.State.FinishBootstrap((ulong)args[0], (bool)args[1]));
            order.Add("entered-world");
            return null;
        });
        map.EnqueueAdd(player.Player, Position());
        map.Tick();
        Assert.Equal(new[] { "map-instance-and-script-callbacks", "entered-world" }, order);
    }

    [Fact]
    public void FailedOuterCallbackReleasesBootstrapWithoutAcknowledgingEntry()
    {
        var player = new TestPlayer();
        var map = new TestMap { OnAdded = () =>
        {
            player.State.BeginBootstrap();
            player.State.RequestEntry(true);
            throw new InvalidOperationException("map script failed");
        }};
        player.Proxy.SetMethodHandler(nameof(IPlayerMapAdmission.CompleteMapAdmission), args =>
        {
            Assert.False((bool)args[1]);
            Assert.False(player.State.FinishBootstrap((ulong)args[0], (bool)args[1]));
            return null;
        });
        map.EnqueueAdd(player.Player, Position());
        Assert.Throws<TargetInvocationException>(() => map.Tick());
        Assert.Null(player.State.BootstrapGeneration);
        Assert.False(player.State.RequestEntry(true));
    }

    [Fact]
    public void DuplicateDirectAddCannotInsertAnAlreadyAdmittedPlayerAgain()
    {
        var player = new TestPlayer();
        TestMap map = null!;
        map = new TestMap { OnAdded = () => player.Proxy.SetProperty(nameof(IPlayer.Map), map) };
        map.EnqueueAdd(player.Player, Position());
        map.EnqueueAdd(player.Player, Position());
        map.Tick();
        Assert.Same(player.Player, Assert.Single(map.Added));
        Assert.Equal(0, map.QueueCount);
    }

    private static MapPosition Position() => new()
    {
        Position = Vector3.Zero,
        Info = new MapInfo { Entry = new WorldEntry { Id = 51 } }
    };

    private static ISharedConfiguration Configuration()
    {
        var result = RecordingDispatchProxy<ISharedConfiguration>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(ISharedConfiguration.Get), new MapConfig { GridActionThreshold = 100, GridUnloadTimer = 600 });
        return result;
    }

    private sealed class TestPlayer
    {
        public PlayerMapAdmissionState State { get; } = new();
        public IAdmissionTestPlayer Player { get; }
        public RecordingDispatchProxy<IAdmissionTestPlayer> Proxy { get; }
        public TestPlayer()
        {
            Player = RecordingDispatchProxy<IAdmissionTestPlayer>.Create(out var proxy);
            Proxy = proxy;
            State.Queue();
            proxy.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionSyncRoot), State.SyncRoot);
            proxy.SetMethodReturnFactory("get_MapAdmissionGeneration", () => State.Generation);
            proxy.SetMethodHandler(nameof(IPlayerMapAdmission.BeginMapAdmission), _ => State.Queue());
            proxy.SetMethodHandler(nameof(IPlayerMapAdmission.IsMapAdmissionCurrent), args => State.IsCurrent((ulong)args[0]));
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
            proxy.SetProperty(nameof(IPlayer.Account), account);
        }
    }

    private sealed class TestMap : BaseMap
    {
        public List<IGridEntity> Added { get; } = [];
        public int SpawnCount { get; private set; }
        public Action OnSpawn { get; init; }
        public Action OnAdded { get; init; }
        public int QueueCount => pendingActions.Count;
        public TestMap() : base(null, null, sharedConfiguration: Configuration()) { }
        public void Tick() => typeof(BaseMap).GetMethod("ProcessGridActions", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, null);
        public void HoldGridUnloading(Vector3 position)
        {
            var (x, z) = MapGrid.GetGridCoord(position);
            var grid = new MapGrid(x, z, allowUnload: true);
            grid.Unload();
            var grids = (IMapGrid[])typeof(BaseMap).GetField("grids", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;
            grids[z * MapDefines.WorldGridCount + x] = grid;
        }
        protected override void SpawnGrid(uint x, uint z) { SpawnCount++; OnSpawn?.Invoke(); }
        protected override void AddEntity(IGridEntity entity, Vector3 vector)
        {
            Added.Add(entity);
            OnAdded?.Invoke();
        }
    }

    private sealed class TestInstancedMap : InstancedMap<IMapInstance>
    {
        public int Created { get; private set; }
        public IPlayer FailCreationFor { get; set; }
        public RecordingDispatchProxy<IMapManager> ManagerProxy { get; }
        public RecordingDispatchProxy<IMapInstance> InstanceProxy { get; }
        private readonly Dictionary<Guid, IMapInstance> destinations = new();
        private readonly IMapLock mapLock;
        public TestInstancedMap() : this(RecordingDispatchProxy<IMapManager>.Create(out var proxy), proxy) { }
        private TestInstancedMap(IMapManager manager, RecordingDispatchProxy<IMapManager> proxy) : base(manager)
        {
            ManagerProxy = proxy;
            mapLock = AddDestination(out var instanceProxy).Info.MapLock;
            InstanceProxy = instanceProxy;
        }

        public MapPosition AddDestination(out RecordingDispatchProxy<IMapInstance> instanceProxy)
        {
            var instance = RecordingDispatchProxy<IMapInstance>.Create(out instanceProxy);
            Guid id = Guid.NewGuid();
            var world = new WorldEntry { Id = 51 };
            instanceProxy.SetProperty(nameof(IMapInstance.InstanceId), id);
            instanceProxy.SetProperty(nameof(IMap.Entry), world);
            var destinationLock = RecordingDispatchProxy<IMapLock>.Create(out var lockProxy);
            lockProxy.SetProperty(nameof(IMapLock.InstanceId), id);
            destinations.Add(id, instance);
            return new MapPosition { Info = new MapInfo { Entry = world, MapLock = destinationLock } };
        }

        protected override IMapLock GetMapLock(IPlayer player) => mapLock;
        protected override IMapInstance CreateInstance(IPlayer player, IMapLock mapLock)
        {
            if (ReferenceEquals(FailCreationFor, player)) throw new InvalidOperationException("fixture creation failure");
            Created++;
            return destinations[mapLock.InstanceId];
        }
    }

    private sealed class CapacityMap : MapInstance
    {
        public CapacityMap() : base(null, null, sharedConfiguration: Configuration())
        {
            typeof(MapInstance).GetField("instanceLimit", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, 1u);
            typeof(BaseMap).GetProperty(nameof(File))!.SetValue(this, new TestMapFile());
        }
        protected override IMapPosition GetPlayerReturnLocation(IPlayer player) => null;
    }

    private sealed class TestMapFile : MapFile
    {
        public TestMapFile() => grids.Add((64u << 16) | 64u, new MapFileGrid());
    }
}
