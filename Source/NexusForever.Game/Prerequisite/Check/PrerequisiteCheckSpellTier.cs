using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 59: native case <c>0x3b</c> (<c>FUN_1404a4f60</c>) returns false for a null unit, otherwise sums the dword at node <c>+0x50</c> over the nodes of the unit's spell list
    /// (<c>unit+0x15c8</c>) whose spell id (node <c>+0x38</c> object, virtual <c>+8</c>) equals <c>ObjectId</c> and compares the sum with <c>Value</c> (integer helper <c>FUN_1404a2090</c>).
    /// Retail: 253 of the 260 distinct <c>ObjectId</c>s are Spell4 ids and <c>Value</c> runs 0..60 (equal to the <c>Spell4StackGroup</c> cap for several spells), so the sum is a stack count rather
    /// than an ability-book tier; the previous implementation compared the player's ability tier.
    /// Status: native body read; "+0x50 is a stack count" is correlated, not read. See Decomp/Analysis/PREREQUISITE_SPELL_STACK_COUNT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTier)]
    public class PrerequisiteCheckSpellTier : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters?.Target ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return PrerequisiteCompare.Compare(comparison, subject.GetActiveSpellStackCount(objectId), value);
        }
    }
}
