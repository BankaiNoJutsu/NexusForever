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
    /// Prerequisite type 289: evaluator case <c>0x121</c> calls manager vtable <c>+0x3d0</c> (<c>14049f0c0</c>) with the comparison only. The flag is bit 0 of the <c>flags</c> field of the
    /// RealmDataCenter row the client was started against (application object <c>DAT_140c635f0 + 0x16e8</c>, set from the <c>realmDataCenterId</c> launch argument through <c>14022e1c0</c>).
    /// Equal returns it, NotEqual its negation, any other comparison false. No unit, ObjectId or Value is read.
    /// </summary>
    /// <remarks>
    /// Retail RealmDataCenter rows carry the bit on <c>auth.ptr.wildstar-online.com</c> (id 12) and one internal row (id 13) only; the live NA and EU rows and the localhost row do not. Read as
    /// "this is a public test realm". The three retail slots offer a quest (10433, 10440) either after its predecessor or, on such a realm, without it.
    /// This server is not a test data centre, so the flag is false. The previous implementation compared "is mounted" with Value, following a label that mistook the application object for a unit.
    /// Status: Implemented from the native body; the meaning of the bit is inferred from the two rows that carry it. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.Unknown289)]
    public class PrerequisiteCheckTestDataCenter : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, false);
    }
}
