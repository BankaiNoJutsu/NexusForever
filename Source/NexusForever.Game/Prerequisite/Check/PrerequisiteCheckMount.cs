using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 84: native test <c>FUN_14049ccc0</c> (vtable <c>+0xe8</c>) is the vehicle test of type 83 (<see cref="PrerequisiteCheckVehicle"/>) with the unit's vehicle entity required to have entity type 7
    /// (<see cref="EntityType.Mount"/>): <c>ObjectId</c> 0 or the mount's creature id, <c>Value</c> -1 (any seat) or the seat type; Equal returns the match, NotEqual its negation, other comparisons false.
    /// Retail: 15 slots, Equal 7, NotEqual 8, <c>Value</c> 0xffffffff in 14. The previous implementation compared "is on a mount" with <c>Value</c>, which can never equal 0xffffffff.
    /// See Decomp/Analysis/PREREQUISITE_VEHICLE_SEAT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Mount)]
    public class PrerequisiteCheckMount : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MovementPrerequisiteHelper.MeetsPlatformVehicle(player, EntityType.Mount, comparison, value, objectId);
    }

    /// <summary>
    /// Prerequisite type 289 (slot <c>+0x3d0</c>, comparison only): kept on the earlier "is on a mount platform compared with Value" behaviour. Its native body was not read.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unknown289)]
    public class PrerequisiteCheckMountScalar : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint mounted = MovementPrerequisiteHelper.GetPlatformEntityType(player) == (uint)EntityType.Mount ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, mounted, value);
        }
    }
}
