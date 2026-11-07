using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 82: native <c>FUN_14049c5b0</c> (vtable <c>+0x48</c>) is false for a null unit; otherwise <c>flag = *(int*)(unit+0x80) == Value</c> through the boolean tail
    /// (Equal returns the flag, NotEqual its negation, every other comparison is false). The client type id is read as <see cref="EntityType"/> (same numbering: 20 player, 23 ghost).
    /// Retail: 1 slot. See Decomp/Analysis/PREREQUISITE_UNIT_ENTITY_TYPE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnitEntityType)]
    public class PrerequisiteCheckUnitEntityType : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target as IWorldEntity ?? player, comparison, value);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value);

        private static bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value)
            => entity != null && PrerequisiteCompare.CompareFlag(comparison, (uint)entity.Type == value);
    }
}
