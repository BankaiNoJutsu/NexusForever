using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 12: handler table slot points at <c>14049d6d0</c> (same body as live
    /// <see cref="PrerequisiteType.ActionSetSpell"/> case <c>0xdd</c>); NF uses death scalar until a
    /// separate dead-state witness is mapped.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.DeadState)]
    public class PrerequisiteCheckDeadState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            if (entity is not IUnitEntity unit)
                return false;

            uint deadState = unit.IsAlive ? 0u : 1u;
            return PrerequisiteCompare.Compare(comparison, deadState, value);
        }
    }
}
