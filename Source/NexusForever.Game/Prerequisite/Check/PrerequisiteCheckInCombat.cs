using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 28: handler table <c>14049dcb0</c> reads unit scalar index <c>2</c> then
    /// <c>PrerequisiteManager_ApplyComparison</c> (<c>1404a2090</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InCombat)]
    public class PrerequisiteCheckInCombat : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            if (entity is not IUnitEntity unit)
                return false;

            uint inCombat = unit.InCombat ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, inCombat, value);
        }
    }
}
