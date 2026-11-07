using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 196 (Racial): native test <c>FUN_14049c520</c> compares the unit's race id (first int of the pointer at <c>unit+0xd0</c>, the same value TargetGroup types 12/13 test;
    /// 0 when absent) with <c>Value</c>. All 40 retail Values are UnitRace ids. Equal meets on equality, NotEqual on inequality, other comparisons never meet. The subject follows the
    /// <c>CreatureDifficulty</c> precedent (<c>parameters.Target ?? player</c>). See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Racial)]
    public class PrerequisiteCheckUnitRace : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters.Target ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            uint unitRaceId = subject.CreatureEntry?.UnitRaceId ?? 0u;
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, unitRaceId == value);
        }
    }
}
