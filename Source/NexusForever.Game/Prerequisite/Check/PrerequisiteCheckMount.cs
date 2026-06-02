using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 84: handler table <c>Prerequisite_CheckMount</c> (<c>14049f0c0</c>) tests mounted flag at
    /// entity <c>+0x16e8</c>. NF checks platform <see cref="EntityType.Mount"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Mount)]
    [PrerequisiteCheck(PrerequisiteType.Unknown289)]
    public class PrerequisiteCheckMount : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint mounted = MovementPrerequisiteHelper.GetPlatformEntityType(player) == (uint)EntityType.Mount ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, mounted, value);
        }
    }
}
