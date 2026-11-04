using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Transport;

public class MountLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingSummonBlocksUntilSeatedEvenAfterMountEntersMap(bool mountHasMap)
    {
        var f = new Fixture();
        var mount = RecordingDispatchProxy<IMountEntity>.Create(out var mp);
        if (mountHasMap) mp.SetProperty(nameof(IMountEntity.Map), f.Player.Map);
        Assert.True(f.Player.CanMount());
        f.Player.SetPendingMount(mount);
        Assert.False(f.Player.CanMount());
    }

    [Fact]
    public void MissingSummonEventuallyAllowsRetryUsingMonotonicTimeout()
    {
        var f = new Fixture();
        f.Player.SetPendingMount(RecordingDispatchProxy<IMountEntity>.Create(out _));
        typeof(Player).GetField("pendingMountTimestamp", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(f.Player, Stopwatch.GetTimestamp() - Stopwatch.Frequency * 11);
        Assert.True(f.Player.CanMount());
    }

    [Theory]
    [InlineData("current")]
    [InlineData("expired")]
    [InlineData("superseded")]
    [InlineData("replacement")]
    [InlineData("transfer")]
    [InlineData("session")]
    public void DelayedPilotAdmissionOwnsTheOriginalSummonPlayerAndSession(string state)
    {
        var owner = new Fixture();
        var replacement = new Fixture();
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var mount = new MountEntity(movement);
        typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!.SetValue(mount, owner.Player.Map);
        typeof(GridEntity).GetProperty(nameof(IPlayer.Guid))!.SetValue(mount, 55u);
        typeof(VehicleEntity).GetProperty(nameof(VehicleEntity.VehicleEntry))!.SetValue(mount, new UnitVehicleEntry { NumberPilots = 1 });
        typeof(VehicleEntity).GetProperty(nameof(VehicleEntity.SpellEntry))!.SetValue(mount, new Spell4Entry { Id = 11 });
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity)
            .GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mount);
        visible[owner.Player.Guid] = owner.Player;
        mount.EnqueuePassengerAdd(owner.Player, VehicleSeatType.Pilot, 0);
        owner.Player.SetPendingMount(mount);
        var newSession = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);

        switch (state)
        {
            case "expired":
                typeof(Player).GetField("pendingMountTimestamp", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(owner.Player, Stopwatch.GetTimestamp() - Stopwatch.Frequency * 11);
                break;
            case "superseded":
                owner.Player.SetPendingMount(RecordingDispatchProxy<IMountEntity>.Create(out _));
                break;
            case "replacement":
                typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!.SetValue(replacement.Player, owner.Player.Map);
                visible[owner.Player.Guid] = replacement.Player;
                break;
            case "transfer":
                ((IPlayerMapAdmission)owner.Player).BeginMapAdmission();
                break;
            case "session":
                typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(owner.Player, newSession);
                break;
        }

        mount.Update(.01);

        Assert.Null(replacement.Player.PlatformGuid);
        Assert.Empty(replacement.Session.Invocations);
        Assert.Empty(sessionProxy.Invocations);
        var removes = owner.Map.GetInvocations(nameof(IBaseMap.EnqueueRemove));
        if (state == "current")
        {
            Assert.NotNull(mount.GetPassenger(owner.Player.Guid));
            Assert.Equal(mount.Guid, owner.Player.PlatformGuid);
            Assert.Equal(mount.Guid, owner.Player.ControlGuid);
            Assert.Single(owner.Session.Invocations.Select(call => call.Arguments[0]).OfType<ServerVehiclePassengerSelf>());
            Assert.Empty(removes);
        }
        else
        {
            Assert.Null(mount.GetPassenger(owner.Player.Guid));
            Assert.Null(owner.Player.PlatformGuid);
            Assert.Null(owner.Player.ControlGuid);
            Assert.Empty(owner.Session.Invocations);
            Assert.Same(mount, Assert.Single(removes).Arguments[0]);
            Assert.Equal(state != "superseded", owner.Player.CanMount());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DismountClearsMissingOrNonPassengerVehicleAndRestoresControl(bool vehicleVisible)
    {
        var f = new Fixture();
        var vehicle = RecordingDispatchProxy<IVehicleEntity>.Create(out var vp);
        vp.SetProperty(nameof(IVehicleEntity.Guid), 55u);
        f.Player.SetPlatform(vehicle);
        if (vehicleVisible) f.Visible[55] = vehicle;
        f.Player.SetPendingMount(RecordingDispatchProxy<IMountEntity>.Create(out _));

        f.Player.Dismount();

        Assert.Null(f.Player.PlatformGuid);
        Assert.Equal(f.Player.Guid, f.Player.ControlGuid);
        Assert.Empty(vp.GetInvocations(nameof(IVehicleEntity.PassengerRemove)));
        Assert.IsType<ServerMovementControl>(Assert.Single(f.Session.Invocations).Arguments[0]);
        Assert.True(f.Player.CanMount());
    }

    [Fact]
    public void SeatingPilotClearsTheSummonReservation()
    {
        var f = new Fixture();
        var mount = (MountEntity)RuntimeHelpers.GetUninitializedObject(typeof(MountEntity));
        f.Player.SetPendingMount(mount);
        Assert.False(f.Player.CanMount());
        typeof(VehicleEntity).GetProperty(nameof(VehicleEntity.SpellEntry))!.SetValue(mount, new NexusForever.GameTable.Model.Spell4Entry { Id = 11 });

        typeof(MountEntity).GetMethod("OnPassengerAdd", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(mount, [f.Player, NexusForever.Game.Static.Entity.VehicleSeatType.Pilot, (byte)0]);

        Assert.True(f.Player.CanMount());
    }

    [Fact]
    public void DismountKeepsExistingPassengerRemovalPath()
    {
        var f = new Fixture();
        var vehicle = RecordingDispatchProxy<IVehicleEntity>.Create(out var vp);
        vp.SetProperty(nameof(IVehicleEntity.Guid), 55u);
        vp.SetMethodReturn(nameof(IVehicleEntity.GetPassenger), RecordingDispatchProxy<IVehiclePassenger>.Create(out _));
        f.Player.SetPlatform(vehicle);
        f.Visible[55] = vehicle;

        f.Player.Dismount();

        Assert.Same(f.Player, Assert.Single(vp.GetInvocations(nameof(IVehicleEntity.PassengerRemove))).Arguments[0]);
    }

    private sealed class Fixture
    {
        public Player Player { get; } = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        public Dictionary<uint, IGridEntity> Visible { get; } = [];
        public RecordingDispatchProxy<IGameSession> Session { get; }
        public RecordingDispatchProxy<IBaseMap> Map { get; }

        public Fixture()
        {
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var maps);
            Map = maps;
            typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!.SetValue(Player, map);
            typeof(GridEntity).GetProperty(nameof(IPlayer.Guid))!.SetValue(Player, 42u);
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Player, Visible);
            uint? platform = null;
            var movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
            movementProxy.SetMethodReturnFactory(nameof(IMovementManager.GetPlatform), () => platform);
            movementProxy.SetMethodHandler(nameof(IMovementManager.SetPlatform), args => { platform = (uint?)args[0]; return null; });
            typeof(WorldEntity).GetProperty(nameof(IPlayer.MovementManager))!.SetValue(Player, movement);
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp);
            Session = sp;
            typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(Player, session);
            typeof(Player).GetProperty(nameof(IPlayer.PetCustomisationManager))!
                .SetValue(Player, RecordingDispatchProxy<IPetCustomisationManager>.Create(out _));
        }
    }
}
