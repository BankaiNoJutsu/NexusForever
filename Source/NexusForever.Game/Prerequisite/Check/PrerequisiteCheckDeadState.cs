using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 12: native <c>FUN_14049c800</c> computes <c>flag = unit+0x250 != 0 || unit+0x254 != 0 || entity type (unit+0x80) == 0x17</c> and ends with the boolean tail:
    /// Equal is <c>flag == Value</c>, NotEqual is <c>flag != Value</c>, every other comparison is false. <c>0x17</c> is <see cref="EntityType.Ghost"/>; the two fields are not mapped and are
    /// represented by <see cref="IUnitEntity.IsAlive"/> (inferred). See Decomp/Analysis/PREREQUISITE_DEAD_STATE_BOOLEAN_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.DeadState)]
    public class PrerequisiteCheckDeadState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            return entity is IUnitEntity unit && MeetsUnit(unit, comparison, value, objectId, parameters);
        }

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            bool dead = !subject.IsAlive || subject.Type == EntityType.Ghost;
            return PrerequisiteCompare.CompareFlag(comparison, (dead ? 1u : 0u) == value);
        }
    }
}
