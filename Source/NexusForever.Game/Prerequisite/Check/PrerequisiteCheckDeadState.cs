using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 12: live case <c>0x0c</c> dispatches to
    /// <c>Prerequisite_CheckDeadState</c> (<c>14049c800</c>), which tests native
    /// entity state fields <c>+0x250</c>/<c>+0x254</c> and entity type <c>0x17</c>.
    /// NF keeps the existing <see cref="IUnitEntity.IsAlive"/> proxy until those native
    /// fields are mapped to server-owned runtime state.
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
