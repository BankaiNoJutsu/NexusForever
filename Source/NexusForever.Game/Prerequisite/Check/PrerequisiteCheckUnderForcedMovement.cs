using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 79: handler table <c>Prerequisite_CheckUnderForcedMovement</c> (<c>14049efa0</c>)
    /// tests forced-movement state at entity <c>+0x640</c>. NF proxies with
    /// <see cref="IMovementManager.ServerControl"/> until spline/forced-move flags are modeled.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnderForcedMovement)]
    [PrerequisiteCheck(PrerequisiteType.Unknown290)]
    public class PrerequisiteCheckUnderForcedMovement : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            uint underForced = entity.MovementManager.ServerControl ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, underForced, value);
        }
    }
}
