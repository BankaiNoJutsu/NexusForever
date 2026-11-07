using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 130: native case <c>0x82</c> returns false for a null first unit; without a prerequisite target Equal is false and NotEqual true; otherwise <c>FUN_140469a70</c> asks the
    /// spell group manager whether some active spell of the first unit whose source unit is the target belongs to group <c>ObjectId</c> (see <see cref="PrerequisiteCheckActiveSpellEffectOnUnit"/> for
    /// type 129). Equal returns that, NotEqual its negation, other comparisons false; <c>Value</c> is not read. The previous implementation read <c>Value</c> as a Spell4 id and ignored the caster.
    /// Status: native body read; group membership as in type 129; caster attribution from <c>RegisterSpellCaster</c>.
    /// See Decomp/Analysis/PREREQUISITE_CASTER_ATTRIBUTED_SPELLS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellEffectOnTarget)]
    public class PrerequisiteCheckActiveSpellEffectOnTarget : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            if (parameters?.Target == null)
                return comparison == PrerequisiteComparison.NotEqual;

            bool active = subject.HasActiveSpellInGroup(objectId, parameters.Target.Guid);
            return PrerequisiteCompare.CompareFlag(comparison, active);
        }
    }
}
