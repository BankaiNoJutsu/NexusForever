using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 102 (enum name <c>SpellCooldownNodeOnUnit</c>, an older guess): native test <c>FUN_1404a4fe0</c> (evaluator case 0x66) returns false for a null unit, then whether a node of the list at
    /// <c>unit+0x15d0</c> has virtual <c>+0x30</c> equal to <c>ObjectId</c>; Equal returns the match, NotEqual its negation and any other comparison is false. <c>Value</c> is not read. 226 of the 290 distinct retail
    /// <c>ObjectId</c>s are <c>Spell4Effects</c> ids (34 are Spell4 ids, none a SpellCoolDown id), so the list holds the unit's active spell effects and the test is "an effect with this id is active on the unit".
    /// Retail: 353 slots, NotEqual 249, Equal 104, <c>Value</c> 0 in 254. The previous implementation looked up a cooldown node by <c>Value</c> and returned false for non-players.
    /// Status: Implemented (the id space is read from the table, not from the list's element class). See Decomp/Analysis/PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeOnUnit)]
    public class PrerequisiteCheckSpellCooldownNodeOnUnit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters?.Target ?? player, comparison, value, objectId, parameters);

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
