using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 129: native case <c>0x81</c> returns false for a null unit, then walks the spell list at <c>unit+0x15c8</c> and asks the spell group manager (<c>DAT_140c65b70</c> virtual <c>+0x38</c>)
    /// whether each spell id belongs to group <c>ObjectId</c>. Equal returns "some spell is in the group", NotEqual its negation and other comparisons are false. <c>Value</c> is not read. All 146 retail
    /// <c>ObjectId</c>s are at most 784, the largest <c>SpellGroupId</c> in <c>Spell4GroupList</c> (the list ids run to 1579), so <c>ObjectId</c> is a spell group id. The previous implementation read <c>Value</c>
    /// (0 in 102 of 146 slots) as a Spell4 id and fell back to <c>ObjectId</c> as a Spell4 id.
    /// Status: Implemented (ObjectId range evidence; the manager call's body was not read). See Decomp/Analysis/PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md.
    /// Both entry points retain the supplied primary unit; Target is relationship context, not a replacement subject (PREREQUISITE_PRIMARY_SUBJECT_20261114.md).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellEffectOnUnit)]
    public class PrerequisiteCheckActiveSpellEffectOnUnit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            bool active = subject.HasActiveSpellInGroup(objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => active,
                PrerequisiteComparison.NotEqual => !active,
                _                               => false
            };
        }
    }
}
