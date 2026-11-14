using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 102 (historical enum name <c>SpellCooldownNodeOnUnit</c>): native evaluator case 0x66 calls <c>1404a4fe0</c> with the primary unit, comparison and <c>ObjectId</c>.
    /// A null primary fails. Otherwise the helper walks primary <c>+0x15d0</c> and compares node virtual <c>+0x30</c> with <c>ObjectId</c>; Equal returns the match, NotEqual its negation, other comparisons fail.
    /// <c>Value</c> and the relationship unit are not read. Both entry points retain their supplied primary subject.
    /// Evidence tier: Verified native selector, Spell4Effects row ID and target ownership; the server's receiving-unit and untimed-registration coverage remains incomplete. No retail parity claim.
    /// See Decomp/Analysis/PREREQUISITE_102_NODE_LIFETIME_20261114.md and PREREQUISITE_102_PRIMARY_SUBJECT_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeOnUnit)]
    public class PrerequisiteCheckSpellCooldownNodeOnUnit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            bool active = subject.HasActiveSpellEffectId(objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => active,
                PrerequisiteComparison.NotEqual => !active,
                _                               => false
            };
        }
    }
}
