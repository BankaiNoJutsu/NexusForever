using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 83: native test <c>FUN_14049cbb0</c> (vtable <c>+0xe0</c>) looks up the unit's vehicle entity (<c>unit+0xc0</c>). Without one, Equal is false and NotEqual true.
    /// With one it matches when <c>ObjectId</c> is 0 or equals the vehicle's creature id, and, unless <c>Value</c> is -1 (0xffffffff, any seat), when the unit's seat type (<c>unit+200</c>, which the client
    /// initialises to 3, <see cref="VehicleSeatType.Invalid"/>) equals <c>Value</c>. Equal returns the match, NotEqual its negation and every other comparison is false.
    /// Retail: 97 slots, <c>ObjectId</c> a Creature2 id (57 distinct, none a UnitVehicle id), <c>Value</c> 0xffffffff in 80 slots (any seat) and 0 in 17 (the pilot seat).
    /// The server tests the player's platform entity when it is a <see cref="EntityType.Vehicle"/> (a mount is not a vehicle here, as before). That <c>unit+200</c> is the seat type rather than the seat position is inferred
    /// from the client's per-entity default of 3 and the retail Values; they would be the same for the pilot seat.
    /// See Decomp/Analysis/PREREQUISITE_VEHICLE_SEAT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Vehicle)]
    public class PrerequisiteCheckVehicle : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MovementPrerequisiteHelper.MeetsPlatformVehicle(player, EntityType.Vehicle, comparison, value, objectId);
    }
}
