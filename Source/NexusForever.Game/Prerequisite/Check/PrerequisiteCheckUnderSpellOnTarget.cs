using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 50: native case <c>0x32</c> returns false for a null first unit; without a prerequisite target Equal is false, NotEqual true and every other comparison false; otherwise
    /// <c>FUN_1404699f0</c> asks whether the first unit has an active spell with id <c>Value</c> whose source unit is the target. Equal returns that, NotEqual its negation, other comparisons false.
    /// <c>ObjectId</c> is not read. The previous implementation asked whether the target was under the spell and fell back to <c>ObjectId</c> when <c>Value</c> was 0.
    /// Status: native body read; caster attribution comes from <c>RegisterSpellCaster</c>. See Decomp/Analysis/PREREQUISITE_CASTER_ATTRIBUTED_SPELLS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnderSpellOnTarget)]
    public class PrerequisiteCheckUnderSpellOnTarget : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            if (parameters?.Target == null)
                return comparison == PrerequisiteComparison.NotEqual;

            bool found = subject.GetActiveSpellStackCount(value, parameters.Target.Guid) > 0u;
            return PrerequisiteCompare.CompareFlag(comparison, found);
        }
    }
}
