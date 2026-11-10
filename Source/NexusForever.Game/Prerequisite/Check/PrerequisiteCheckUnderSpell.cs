using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 15: evaluator case <c>0xf</c> calls <c>Prerequisite_CheckUnderSpell</c> (<c>1404a4ec0</c>) with the comparison and the slot <b>Value</b> word only. A null unit is false; otherwise it
    /// walks the unit active-spell list at <c>+0x15c8</c> for a spell whose id equals Value; Equal returns the match, NotEqual its negation, any other comparison false. ObjectId is never read.
    /// Retail: 3,711 slots; one slot (prerequisite 29145: NotEqual, ObjectId 167471, Value 0) has Value 0 with ObjectId set, which natively asks for spell 0 and is therefore always met.
    /// The previous implementation fell back to ObjectId when Value was 0. See Decomp/Analysis/PREREQUISITE_HANDLER_TABLE_AUDIT_20261110.md. No retail parity claim.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnderSpell)]
    public class PrerequisiteCheckUnderSpell : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckUnderSpell> log;

        public PrerequisiteCheckUnderSpell(
            ILogger<PrerequisiteCheckUnderSpell> log)
        {
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            // spell id 0 never matches an active spell
            bool isUnderSpell = value != 0u && subject.HasTrackedSpellState(value);

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return isUnderSpell;
                case PrerequisiteComparison.NotEqual:
                    return !isUnderSpell;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.UnderSpell}!");
                    return false;
            }
        }
    }
}
