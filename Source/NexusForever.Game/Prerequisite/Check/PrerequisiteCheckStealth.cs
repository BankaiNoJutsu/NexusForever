using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 116 (client error text "Stealth property requirement not met"): evaluator case <c>0x74</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) is inline. A null unit is
    /// false; otherwise the flag is <c>1 &lt; *(int *)(unit + 0x54)</c>, returned for Equal and negated for NotEqual, with every other comparison false. Neither ObjectId nor Value is read.
    /// </summary>
    /// <remarks>
    /// What the field at <c>+0x54</c> holds was not read natively. That it is the stealth state is inferred from the error text and from the retail rows: 22 of 32 slots carry Value 28455, which the client
    /// ignores and which is Spell4 "Stalker - Optic Camouflage" (first effect: Stealth), and the rows are paired with UnderSpell checks for "Stalker - Combat Stealth" (39407).
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.Stealth)]
    public class PrerequisiteCheckStealth : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject != null && PrerequisiteCompare.CompareFlag(comparison, subject.IsStealthed);
    }
}
