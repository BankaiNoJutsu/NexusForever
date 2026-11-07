using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 31: native test <c>FUN_14049ed50</c>. Walks the unit's nearby-entity list for an entity whose creature id equals <c>ObjectId</c> and whose
    /// 3D distance from the unit is at most <c>Value</c> (squared comparison, inclusive). Equal meets when such a creature exists, NotEqual when none does, other
    /// comparisons never meet. See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CreatureWithinDistance)]
    public class PrerequisiteCheckCreatureWithinDistance : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            float range = value;
            float rangeSquared = range * range;

            bool found = player.GetVisibleCreature<IWorldEntity>(objectId)
                .Any(creature => Vector3.DistanceSquared(player.Position, creature.Position) <= rangeSquared);

            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, found);
        }
    }
}
