using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 40: handler table <c>Prerequisite_CheckHealth</c> (<c>14049e440</c>) compares
    /// integer health via <c>PrerequisiteManager_ApplyComparison</c> (<c>1404a2090</c>), distinct from
    /// type 172 <see cref="PrerequisiteType.HealthScaled"/> float path at <c>1404a2010</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Health)]
    public class PrerequisiteCheckHealth : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            return PrerequisiteCompare.Compare(comparison, entity.Health, value);
        }
    }
}
