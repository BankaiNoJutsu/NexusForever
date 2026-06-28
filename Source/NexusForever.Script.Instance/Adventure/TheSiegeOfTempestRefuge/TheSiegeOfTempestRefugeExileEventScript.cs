using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge
{
    /// <summary>
    /// Exile-side defense against the Dominion assault.
    /// </summary>
    [ScriptFilterOwnerId(173)]
    public class TheSiegeOfTempestRefugeExileEventScript : TheSiegeOfTempestRefugeEventScript
    {
        protected override PublicEventObjective DefendAgainstAssaultObjective => PublicEventObjective.DefendAgainstTheDominionAssault;
        protected override PublicEventObjective DefendGeneratorObjective => PublicEventObjective.DefendTheGeneratorAgainstDominionAttacks;
        protected override PublicEventObjective FallBackToGeneratorObjective => PublicEventObjective.FallBackToTheGeneratorDominionAssault;
        protected override PublicEventObjective HoldOutAgainstAssaultObjective => PublicEventObjective.HoldOutAgainstTheDominionAssault;

        protected override void ActivateDefendAgainstAssaultObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.DefendAgainstTheDominionAssault);
        }

        protected override void ActivateDefendGeneratorObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.DefendTheGeneratorAgainstDominionAttacks);
        }

        protected override void ActivateFallBackToGeneratorObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.FallBackToTheGeneratorDominionAssault);
        }

        protected override void ActivateHoldOutAgainstAssaultObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.HoldOutAgainstTheDominionAssault);
        }
    }
}
