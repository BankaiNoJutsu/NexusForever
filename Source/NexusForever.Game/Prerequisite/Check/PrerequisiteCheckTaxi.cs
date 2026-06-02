using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 85: handler table <c>Prerequisite_CheckTaxi</c> (<c>14049f0f0</c>) tests taxi movement state at
    /// entity <c>+0x16a0</c>. NF checks platform <see cref="EntityType.Taxi"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Taxi)]
    public class PrerequisiteCheckTaxi : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint onTaxi = MovementPrerequisiteHelper.GetPlatformEntityType(player) == (uint)EntityType.Taxi ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, onTaxi, value);
        }
    }
}
