using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 280: native case <c>0x118</c> requires the type 59 helper (<c>FUN_1404a4f60</c>, a stack count of the spell id <c>ObjectId</c> compared with <c>Value</c>) to hold for both
    /// the caster and the target unit. See <see cref="PrerequisiteCheckSpellTier"/> and Decomp/Analysis/PREREQUISITE_SPELL_STACK_COUNT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTierOnCasterAndTarget)]
    public class PrerequisiteCheckSpellTierOnCasterAndTarget : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null || parameters?.Target == null)
                return false;

            return PrerequisiteCompare.Compare(comparison, player.GetActiveSpellStackCount(objectId), value)
                && PrerequisiteCompare.Compare(comparison, parameters.Target.GetActiveSpellStackCount(objectId), value);
        }
    }
}
