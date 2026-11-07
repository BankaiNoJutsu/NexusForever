using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_VEHICLE_SEAT_20261107.md): type 83 (FUN_14049cbb0) looks up the unit's vehicle entity (unit+0xc0). Without one Equal is false and NotEqual true. With one, it matches
// when ObjectId is 0 or equals the vehicle's creature id, and, when Value is not -1, when the unit's seat type (unit+200, 3 = invalid by default) equals Value. Equal returns the match, NotEqual its negation,
// other comparisons false. Retail: 97 slots, ObjectId a Creature2 id in 57 distinct slots (0 = any), Value 0xffffffff (any seat, 80 slots) or 0 (the pilot seat, 17).
public class PrerequisiteCheckVehicleTests
{
    private const uint VehicleGuid = 77u;
    private const uint VehicleCreature = 43810u;

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, VehicleCreature, 0xffffffffu, true)]    // any seat of that vehicle
    [InlineData(PrerequisiteComparison.Equal, 0u, 0xffffffffu, true)]                 // any vehicle, any seat
    [InlineData(PrerequisiteComparison.Equal, 99999u, 0xffffffffu, false)]            // another vehicle
    [InlineData(PrerequisiteComparison.NotEqual, VehicleCreature, 0xffffffffu, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 99999u, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.Equal, VehicleCreature, 0u, true)]             // pilot seat
    [InlineData(PrerequisiteComparison.Equal, VehicleCreature, 1u, false)]            // the player is not in a passenger seat
    [InlineData(PrerequisiteComparison.NotEqual, VehicleCreature, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, VehicleCreature, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, VehicleCreature, 0xffffffffu, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, VehicleCreature, 0u, false)]
    public void ComparesTheVehicleCreatureAndTheSeatType(PrerequisiteComparison comparison, uint objectId, uint value, bool expected)
    {
        IPlayer player = PlayerOnVehicle(VehicleSeatType.Pilot);

        Assert.Equal(expected, new PrerequisiteCheckVehicle().Meets(player, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, false)]
    public void APassengerSeatIsAnotherSeatType(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = PlayerOnVehicle(VehicleSeatType.Passenger);

        Assert.Equal(expected, new PrerequisiteCheckVehicle().Meets(player, comparison, value, VehicleCreature, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, false)]
    public void WithoutAVehicleEqualFailsAndNotEqualMeets(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.PlatformGuid), null);

        Assert.Equal(expected, new PrerequisiteCheckVehicle().Meets(player, comparison, 0xffffffffu, VehicleCreature, new PrerequisiteParameters()));
    }

    [Fact]
    public void AMountPlatformIsNotAVehicle()
    {
        IPlayer player = PlayerOnPlatform(EntityType.Mount, VehicleSeatType.Pilot);

        Assert.False(new PrerequisiteCheckVehicle().Meets(player, PrerequisiteComparison.Equal, 0xffffffffu, 0u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckVehicle().Meets(player, PrerequisiteComparison.NotEqual, 0xffffffffu, 0u, new PrerequisiteParameters()));
    }

    // Type 84 (FUN_14049ccc0, vtable +0xe8) is the same test as 83 with the vehicle entity required to be of entity type 7 (EntityType.Mount) instead of a vehicle. Retail: 15 slots, Equal 7, NotEqual 8,
    // ObjectId 0 in 13, Value 0xffffffff in 14. The previous implementation compared "is on a mount" with Value, which can never equal 0xffffffff.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, 0xffffffffu, false)]
    [InlineData(PrerequisiteComparison.Equal, VehicleCreature, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.Equal, 99999u, 0xffffffffu, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 99999u, 0xffffffffu, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 1u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, 0xffffffffu, false)]
    public void MountComparesTheMountCreatureAndTheSeatType(PrerequisiteComparison comparison, uint objectId, uint value, bool expected)
    {
        IPlayer player = PlayerOnPlatform(EntityType.Mount, VehicleSeatType.Pilot);

        Assert.Equal(expected, new PrerequisiteCheckMount().Meets(player, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    public void MountWithOnlyAVehiclePlatformOrNoneIsNotMounted(PrerequisiteComparison comparison, bool expected)
    {
        Assert.Equal(expected, new PrerequisiteCheckMount().Meets(PlayerOnVehicle(VehicleSeatType.Pilot), comparison, 0xffffffffu, 0u, new PrerequisiteParameters()));

        IPlayer none = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.PlatformGuid), null);
        Assert.Equal(expected, new PrerequisiteCheckMount().Meets(none, comparison, 0xffffffffu, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer PlayerOnVehicle(VehicleSeatType seat) => PlayerOnPlatform(EntityType.Vehicle, seat);

    private static IPlayer PlayerOnPlatform(EntityType type, VehicleSeatType seat)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IVehicleEntity vehicle = RecordingDispatchProxy<IVehicleEntity>.Create(out var vehicleProxy);
        IVehiclePassenger passenger = RecordingDispatchProxy<IVehiclePassenger>.Create(out var passengerProxy);

        passengerProxy.SetProperty(nameof(IVehiclePassenger.SeatType), seat);
        vehicleProxy.SetProperty(nameof(IWorldEntity.Type), type);
        vehicleProxy.SetProperty(nameof(IWorldEntity.CreatureId), VehicleCreature);
        vehicleProxy.SetMethodHandler(nameof(IVehicleEntity.GetPassenger), args => args.Length == 1 ? passenger : null);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 9u);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), VehicleGuid);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisible), (MethodInfo _, object[] args) => (uint)args[0] == VehicleGuid ? vehicle : null);
        return player;
    }
}
