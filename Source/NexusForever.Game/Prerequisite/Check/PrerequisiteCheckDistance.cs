using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 95: handler table <c>Prerequisite_CheckDistance</c> (<c>14049f770</c>) compares
    /// distance between player and prerequisite target positions.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Distance)]
    public class PrerequisiteCheckDistance : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity target = parameters.Target as IWorldEntity;
            if (target == null)
                return false;

            float distance = Vector3.Distance(player.Position, target.Position);
            uint distanceUnits = (uint)distance;
            return PrerequisiteCompare.Compare(comparison, distanceUnits, value);
        }
    }
}
