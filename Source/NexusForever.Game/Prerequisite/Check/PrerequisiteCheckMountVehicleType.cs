using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 90: handler table <c>Prerequisite_CheckMountVehicleType</c> (<c>14049f460</c>) and live vtable
    /// <c>+0x2c0</c> filter mount subobject unit-type ids (client types 3–5). NF compares platform
    /// <see cref="IWorldEntity.Type"/> while mounted.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.MountVehicleType)]
    public class PrerequisiteCheckMountVehicleType : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint platformType = MovementPrerequisiteHelper.GetPlatformEntityType(player);
            // Native (evaluator case 0x5a, vtable +0x2c0, 14049e730) receives only the unit and the comparison: the flag is "the unit has a link at +0xf00" (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // Value is never read; the previous comparison of a platform type with Value inverted all 9 retail slots (Value 0). The redirect through a type 3/7/9 link is not modelled.
            return PrerequisiteCompare.CompareFlag(comparison, platformType != 0u);
        }
    }
}
