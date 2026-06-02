using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 82: live vtable <c>+0x48</c> and handler table <c>14049f010</c> compare client
    /// unit type id at entity <c>+0x80</c> to <c>value0</c>. NF uses <see cref="EntityType"/> until
    /// client unit-type ids are mapped on entities.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnitEntityType)]
    public class PrerequisiteCheckUnitEntityType : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            return PrerequisiteCompare.Compare(comparison, (uint)entity.Type, value);
        }
    }
}
