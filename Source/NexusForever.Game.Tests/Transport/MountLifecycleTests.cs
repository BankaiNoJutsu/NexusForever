using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
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
        f.Player.SetPendingMount(RecordingDispatchProxy<IMountEntity>.Create(out _));
        Assert.False(f.Player.CanMount());
        var customisation = RecordingDispatchProxy<IPetCustomisationManager>.Create(out _);
        typeof(Player).GetProperty(nameof(IPlayer.PetCustomisationManager))!.SetValue(f.Player, customisation);
        var mount = (MountEntity)RuntimeHelpers.GetUninitializedObject(typeof(MountEntity));
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

        public Fixture()
        {
            var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
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
        }
    }
}
