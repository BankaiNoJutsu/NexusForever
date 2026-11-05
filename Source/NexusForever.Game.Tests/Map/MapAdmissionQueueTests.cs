using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NexusForever.WorldServer.Network;
using Role = NexusForever.Game.Static.Matching.Role;

namespace NexusForever.Game.Tests.Map;

public interface IAdmissionTestPlayer : IPlayer, IPlayerMapAdmission { }

public sealed class MapAdmissionQueueTests
{
    [Theory]
    [InlineData("before routing")]
    [InlineData("before source removal")]
    [InlineData("after source removal")]
    [InlineData("no departure")]
    [InlineData("unrelated teleport")]
    public void MatchDepartureCancelsOnlyItsOwnTransfer(string timing)
    {
        WorldEntry sourceWorld = new() { Id = 51 };
        WorldEntry matchWorld = new() { Id = 77, Type = MapType.Pvp };
        var source = new TestMap();
        source.Initialise(sourceWorld);
        var destination = new TestMap();
        var mapFactory = RecordingDispatchProxy<IMapFactory>.Create(out var factory);
        factory.SetMethodHandler(nameof(IMapFactory.CreateMap), args => Equals(args[0], matchWorld.Type) ? destination : source);
        var maps = new MapManager(mapFactory);

        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var session = RecordingDispatchProxy<IGameSession>.Create(out var connection);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, session);
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Identity { RealmId = 1, Id = 42 });
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var facing);
        facing.SetMethodReturn(nameof(IMovementManager.GetRotation), Vector3.UnitX);
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.MovementManager))!.SetValue(player, movement);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.World), PlayerBindPointTests.Table(sourceWorld, matchWorld));
        PlayerBindPointTests.Field(typeof(Player), player, "mapManager", maps);
        PlayerBindPointTests.Field(typeof(Player), player, "gameTableManager", tables);
        source.Attach(player, new Vector3(10, 20, 30));

        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        var data = RecordingDispatchProxy<IMatchingDataManager>.Create(out var entrances);
        entrances.SetMethodReturn(nameof(IMatchingDataManager.GetMapEntrance), new MapEntrance
        {
            MapId = (ushort)matchWorld.Id, Team = 0, Position = Vector3.One, Rotation = Vector3.UnitY
        });
        var memberFactory = RecordingDispatchProxy<IFactory<IMatchTeamMember>>.Create(out var members);
        members.SetMethodReturn(nameof(IFactory<IMatchTeamMember>.Resolve), new MatchTeamMember(players));
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var match = RecordingDispatchProxy<IMatch>.Create(out var matchProxy);
        matchProxy.SetProperty(nameof(IMatch.MatchingMap), new MatchingMap
        {
            GameMapEntry = new MatchingGameMapEntry { WorldId = matchWorld.Id },
            GameTypeEntry = new MatchingGameTypeEntry()
        });
        var team = new MatchTeam(data, memberFactory, publisher);
        team.Initialise(match, Static.Matching.MatchTeam.Red);
        matchProxy.SetMethodReturn(nameof(IMatch.GetTeams), new[] { team });

        if (timing == "unrelated teleport")
            player.TeleportTo(matchWorld, Vector3.One);
        team.MatchJoin(player.Identity, Role.DPS);
        var admission = (IPlayerMapAdmission)player;
        ulong originalGeneration = admission.MapAdmissionGeneration;
        if (timing is "before source removal" or "after source removal")
            maps.Update(0);
        if (timing == "after source removal")
        {
            source.Tick();
            Assert.Null(player.Map);
        }

        if (timing != "no departure")
            team.MatchLeave(player.Identity);
        maps.Update(0);
        source.Tick();
        maps.Update(0);
        maps.Update(0);

        if (timing is "no departure" or "unrelated teleport")
        {
            Assert.Same(player, Assert.Single(destination.Added));
            Assert.True(admission.IsMapAdmissionCurrent(originalGeneration));
        }
        else
        {
            Assert.Empty(destination.Added);
            Assert.False(admission.IsMapAdmissionCurrent(originalGeneration));
            Assert.DoesNotContain(connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)),
                call => call.Arguments[0] is ServerForceKick);
            if (timing == "after source removal")
            {
                Assert.Same(player, Assert.Single(source.Added));
                Assert.Equal(new Vector3(10, 20, 30), source.LastAddedPosition);
            }
            else
            {
                Assert.Same(source, player.Map);
                Assert.Empty(source.Added);
                Assert.True(player.CanTeleport());
                Assert.False(player.IsLoading);
                Assert.Equal(player.Guid, player.ControlGuid);
            }
            Assert.Contains(facing.GetInvocations(nameof(IMovementManager.SetRotation)),
                call => Equals(call.Arguments[0], Vector3.UnitX));
        }
    }

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
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void FailedRealPlayerBootstrapDisconnectsAndPreservesMapCleanupOwnership(bool afterAttachment, bool replaced)
    {
        var map = new BootstrapFaultMap();
        var mapManager = RecordingDispatchProxy<IMapManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IMapManager.AddToMap), args =>
        {
            map.EnqueueAdd((IPlayer)args[0], (IMapPosition)args[1]);
            return null;
        });
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var player = new Player(movement, null, null, null, null, null, null, mapManager: mapManager);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, session);
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Game.Abstract.Identity { RealmId = 1, Id = 42 });
        if (afterAttachment)
        {
            var scripts = RecordingDispatchProxy<IScriptCollection>.Create(out var scriptProxy);
            scriptProxy.SetMethodHandler(nameof(IScriptCollection.Invoke), _ => throw new InvalidOperationException("fixture attached bootstrap failure"));
            typeof(GridEntity).GetField("scriptCollection", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, scripts);
        }
        else
        {
            sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
            {
                if (args[0] is not ServerChangeWorld) return null;
                if (replaced) sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new InvalidOperationException("fixture pre-attachment bootstrap failure");
            });
        }

        var position = Position();
        position.Position = new Vector3(MapDefines.GridSize + 32, 0, 32);
        player.TeleportTo(position);
        var admission = (IPlayerMapAdmission)player;
        ulong generation = admission.MapAdmissionGeneration;
        map.Tick();
        player.OnEnteredWorld();
        map.Tick();

        Assert.Equal(replaced ? 0 : 1, sessionProxy.GetInvocations(nameof(IGameSession.ForceDisconnect)).Count);
        Assert.DoesNotContain(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)), call => call.Arguments[0] is ServerPlayerEnteredWorld);
        Assert.False(admission.IsMapAdmissionCurrent(generation));
        Assert.False(player.IsLoading);
        Assert.True(player.CanTeleport());
        var all = RecordingDispatchProxy<ISearchCheck<IPlayer>>.Create(out var searchProxy);
        searchProxy.SetMethodReturn(nameof(ISearchCheck<IPlayer>.CheckEntity), true);
        var grid = map.GridAt(position.Position);
        Assert.Equal(afterAttachment ? 1u : 0u, BootstrapFaultMap.GridOccupancy(grid));
        if (afterAttachment)
        {
            Assert.Same(map, player.Map);
            Assert.Same(player, Assert.Single(map.Registered));
            Assert.Same(player, map.GetEntity<IPlayer>(player.Guid));
            Assert.Same(player, Assert.Single(grid.Search(position.Position, all)));
            typeof(GridEntity).GetField("scriptCollection", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, null);
            map.EnqueueRemove(player);
            map.Tick();
            Assert.Equal(0u, BootstrapFaultMap.GridOccupancy(grid));
        }
        Assert.Null(player.Map);
        Assert.Empty(map.Registered);
        Assert.Empty(grid.Search(position.Position, all));
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
        public Vector3 LastAddedPosition { get; private set; }
        public int SpawnCount { get; private set; }
        public Action OnSpawn { get; init; }
        public Action OnAdded { get; init; }
        public int QueueCount => pendingActions.Count;
        public TestMap() : base(null, null, sharedConfiguration: Configuration()) { }
        public override void Initialise(WorldEntry entry) =>
            typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, entry);
        public override GenericError? CanEnter(IPlayer player, IMapPosition position) => null;
        public override void Update(double lastTick) => Tick();
        public void Attach(Player player, Vector3 position)
        {
            typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, this);
            typeof(GridEntity).GetProperty(nameof(GridEntity.Guid))!.SetValue(player, 1u);
            typeof(GridEntity).GetProperty(nameof(GridEntity.Position))!.SetValue(player, position);
            entities.Add(player.Guid, player);
        }
        protected override void RemoveEntity(IGridEntity entity)
        {
            entities.Remove(entity.Guid);
            typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(entity, null);
        }
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
            LastAddedPosition = vector;
            OnAdded?.Invoke();
        }
    }

    private sealed class BootstrapFaultMap : BaseMap
    {
        public IEnumerable<IGridEntity> Registered => entities.Values;

        public BootstrapFaultMap() : base(null, RecordingDispatchProxy<IPublicEventManager>.Create(out _), sharedConfiguration: Configuration())
        {
            typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, Position().Info.Entry);
            VisionRange = -MapDefines.GridSize;
        }

        public void Tick() => typeof(BaseMap).GetMethod("ProcessGridActions", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, null);
        protected override void SpawnGrid(uint gridX, uint gridZ) { }

        public IMapGrid GridAt(Vector3 position)
        {
            var (x, z) = MapGrid.GetGridCoord(position);
            var grids = (IMapGrid[])typeof(BaseMap).GetField("grids", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;
            return grids[z * MapDefines.WorldGridCount + x];
        }

        public static uint GridOccupancy(IMapGrid grid) => (uint)typeof(MapGrid).GetField("entityCount", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(grid)!;
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
