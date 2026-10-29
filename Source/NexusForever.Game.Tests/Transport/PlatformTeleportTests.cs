using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Transport;

public class PlatformTeleportTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocalTeleportDetachesBeforeBroadcastAndPreservesExplicitRelocationAndCallback(bool loadingScreen)
    {
        var f = new Fixture();
        Vector3 destination = new(4, 5, 6);
        int callbacks = 0;
        f.Movement.SetMethodHandler(nameof(IMovementManager.BroadcastNetworkEntityCommands), _ =>
        {
            Assert.Null(f.Player.PlatformGuid);
            Assert.Equal(destination, f.LastPosition);
            f.Events.Add("broadcast");
            return null;
        });
        f.Player.TeleportToLocal(destination, loadingScreen, p =>
        {
            Assert.Null(f.Player.PlatformGuid);
            Assert.Equal(destination, p);
            callbacks++;
            f.Events.Add("callback");
        });

        Assert.Null(f.Player.PlatformGuid);
        Assert.Equal(42u, f.Player.ControlGuid);
        Assert.Equal(1, callbacks);
        var move = Assert.Single(f.Map.GetInvocations(nameof(IBaseMap.EnqueueRelocate)));
        Assert.Equal(2, move.Arguments.Length);
        Assert.Equal(destination, move.Arguments[1]);
        Assert.True(f.Events.IndexOf("detach") < f.Events.IndexOf("broadcast"));
        Assert.True(f.Events.IndexOf("broadcast") < f.Events.IndexOf("callback"));
        Assert.Equal(loadingScreen ? 1 : 0, f.Session.Invocations.Count(i => i.Arguments[0] is ServerLoadingScreen));
    }

    [Fact]
    public void WorldTeleportDetachesBeforeMapTransferAndCapturesWorldReturnPosition()
    {
        var f = new Fixture();
        var destination = new MapPosition { Info = new MapInfo { Entry = new WorldEntry { Id = 51 } }, Position = new Vector3(4, 5, 6) };
        f.MapManager.SetMethodHandler(nameof(IMapManager.AddToMap), args =>
        {
            Assert.Null(f.Player.PlatformGuid);
            Assert.Same(destination, args[1]);
            Assert.Contains("detach", f.Events);
            return null;
        });

        f.Player.TeleportTo(destination);

        Assert.Null(f.Player.PlatformGuid);
        Assert.Single(f.MapManager.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.False(f.Player.CanTeleport());
        var source = f.Player.GetOpenWorldReturnPosition();
        Assert.Equal(426u, source.Info.Entry.Id);
        Assert.Equal(new Vector3(100, 0, 100), source.Position);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingTransferRejectsTeleportWithoutDetachingCurrentPlatform(bool local)
    {
        var f = new Fixture();
        var pending = typeof(Player).GetField("pendingTeleport", BindingFlags.Instance | BindingFlags.NonPublic)!;
        pending.SetValue(f.Player, RuntimeHelpers.GetUninitializedObject(pending.FieldType));
        if (local) f.Player.TeleportToLocal(Vector3.Zero);
        else f.Player.TeleportTo(new MapPosition { Info = new MapInfo { Entry = new WorldEntry { Id = 51 } } });
        Assert.Equal(22u, f.Player.PlatformGuid);
        Assert.DoesNotContain("detach", f.Events);
        Assert.Empty(f.MapManager.Invocations);
        Assert.Empty(f.Map.GetInvocations(nameof(IBaseMap.EnqueueRelocate)));
    }

    [Fact]
    public void DismountFromNormalPlatformKeepsExistingRecoveryWithoutVehicleCast()
    {
        var f = new Fixture();
        f.Player.Dismount();
        Assert.Null(f.Player.PlatformGuid);
        Assert.Equal(42u, f.Player.ControlGuid);
    }

    private sealed class Fixture
    {
        public Player Player { get; } = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        public RecordingDispatchProxy<IMovementManager> Movement { get; }
        public RecordingDispatchProxy<IBaseMap> Map { get; }
        public RecordingDispatchProxy<IMapManager> MapManager { get; }
        public RecordingDispatchProxy<IGameSession> Session { get; }
        public List<string> Events { get; } = [];
        public Vector3 LastPosition { get; private set; }

        public Fixture()
        {
            typeof(Player).GetProperty(nameof(IPlayer.Identity))!.SetValue(Player,
                new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 42 });
            var platform = RecordingDispatchProxy<IWorldEntity>.Create(out var pp);
            pp.SetProperty(nameof(IWorldEntity.Guid), 22u);
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp);
            Map = mp;
            mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 426 });
            mp.SetMethodReturn(nameof(IBaseMap.GetEntity), platform);
            typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!.SetValue(Player, map);
            typeof(GridEntity).GetProperty(nameof(IPlayer.Guid))!.SetValue(Player, 42u);
            typeof(GridEntity).GetProperty(nameof(IPlayer.Position))!.SetValue(Player, new Vector3(100, 0, 100));
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Player,
                new Dictionary<uint, IGridEntity> { [22] = platform });
            var manager = RecordingDispatchProxy<IMapManager>.Create(out var mm);
            MapManager = mm;
            typeof(Player).GetField("mapManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Player, manager);
            uint? platformId = 22;
            var movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
            Movement = movementProxy;
            movementProxy.SetMethodReturnFactory(nameof(IMovementManager.GetPlatform), () => platformId);
            movementProxy.SetMethodHandler(nameof(IMovementManager.SetPlatform), args =>
            {
                platformId = (uint?)args[0]; Events.Add("detach"); return null;
            });
            movementProxy.SetMethodHandler(nameof(IMovementManager.SetPosition), args => { LastPosition = (Vector3)args[0]; return null; });
            typeof(WorldEntity).GetProperty(nameof(IPlayer.MovementManager))!.SetValue(Player, movement);
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp);
            Session = sp;
            typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(Player, session);
        }
    }
}
