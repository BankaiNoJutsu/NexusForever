using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 149: native <c>FUN_14049c840</c> computes <c>state = (unit+0x1e4 != 0) || (unit+0x2ac != 0)</c> and compares it with <c>Value</c>: Equal <c>state == Value</c>, NotEqual
    /// <c>state != Value</c>. <c>unit+0x2ac</c> is the in-combat flag (prerequisite 28) and <c>unit+0x1e4</c> is the in-use flag the client sets from <c>ServerUnitInUse</c> (0x0755), which the server sends
    /// from <see cref="IWorldEntity.IsBusy"/>. The writer was read by an agent and not re-derived, see Decomp/Analysis/PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md. Any other comparison is false.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.State)]
    public class PrerequisiteCheckEntityState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters.Target as IUnitEntity ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            uint state = subject.IsBusy || subject.InCombat ? 1u : 0u;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => state == value,
                PrerequisiteComparison.NotEqual => state != value,
                _                               => false
            };
        }
    }
}
