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
            return PrerequisiteCompare.Compare(comparison, platformType, value);
        }
    }
}
