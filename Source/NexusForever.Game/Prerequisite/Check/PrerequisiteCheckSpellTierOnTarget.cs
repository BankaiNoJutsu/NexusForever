using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 60: native case <c>0x3c</c> returns false for a null first unit; without a prerequisite target Equal is false, NotEqual true and every other comparison false; otherwise
    /// <c>FUN_14046a110</c> sums the node counts (<c>+0x50</c>, see <see cref="PrerequisiteCheckSpellTier"/>) of the first unit's active spells with id <c>ObjectId</c> whose source unit is the
    /// target, and the sum is compared with <c>Value</c> through the integer helper.
    /// Status: native body read; the server attributes spell states to casters through <c>RegisterSpellCaster</c> (inferred stack model).
    /// See Decomp/Analysis/PREREQUISITE_CASTER_ATTRIBUTED_SPELLS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTierOnTarget)]
    public class PrerequisiteCheckSpellTierOnTarget : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            if (parameters?.Target == null)
                return comparison == PrerequisiteComparison.NotEqual;

            return PrerequisiteCompare.Compare(comparison, subject.GetActiveSpellStackCount(objectId, parameters.Target.Guid), value);
        }
    }
}
