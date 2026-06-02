using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 83: live vtable <c>+0xe0</c> target
    /// <c>Prerequisite_CheckVehicleCreature_Table83</c> (<c>14049cbb0</c>) resolves the current vehicle
    /// entity and can filter by vehicle <c>Creature2</c> id. NF currently checks platform
    /// <see cref="EntityType.Vehicle"/> (not mount).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Vehicle)]
    public class PrerequisiteCheckVehicle : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint onVehicle = MovementPrerequisiteHelper.GetPlatformEntityType(player) == (uint)EntityType.Vehicle ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, onVehicle, value);
        }
    }
}
