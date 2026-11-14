using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 38 (enum name <c>IsCreature</c>): native test <c>FUN_14049ecc0</c> (vtable <c>+0x350</c>) reads the integer at the start of the structure at <c>unit+0x18</c> (0 when the unit has none) and compares
    /// it for equality with <c>Value</c>; Equal returns that, NotEqual its negation and every other comparison is false. Retail <c>Value</c>s are Creature2 ids (16623, 20814, 27445, ...; 0 in 7 slots), so the test is
    /// "the unit is the creature with this id". The previous implementation compared a creature-kind flag (1 or 0) with <c>Value</c>, which only held for the 7 zero slots.
    /// See Decomp/Analysis/PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md.
    /// Both entry points retain the supplied primary unit; Target is relationship context, not a replacement subject (PREREQUISITE_PRIMARY_SUBJECT_20261114.md).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsCreature)]
    public class PrerequisiteCheckIsCreature : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            bool match = subject.CreatureId == value;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => match,
                PrerequisiteComparison.NotEqual => !match,
                _                               => false
            };
        }
    }
}
