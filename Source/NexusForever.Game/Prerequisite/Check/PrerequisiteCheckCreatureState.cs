using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 76: handler table <c>14049ef00</c> checks unit state index <c>value0</c> on target
    /// (optional <c>objectId0</c> filters <c>Creature2</c> id).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CreatureState)]
    public class PrerequisiteCheckCreatureState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            if (entity is not IUnitEntity unit)
                return false;

            if (objectId != 0u && unit.CreatureEntry?.Id != objectId)
                return false;

            uint hasState = unit.HasUnitState(value) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hasState, 1u);
        }
    }
}
