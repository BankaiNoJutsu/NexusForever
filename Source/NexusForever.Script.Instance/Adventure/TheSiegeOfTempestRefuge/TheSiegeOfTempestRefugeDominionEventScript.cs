using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge
{
    /// <summary>
    /// Dominion-side defense against the Exile assault.
    /// </summary>
    [ScriptFilterOwnerId(174)]
    public class TheSiegeOfTempestRefugeDominionEventScript : TheSiegeOfTempestRefugeEventScript
    {
        protected override PublicEventObjective DefendAgainstAssaultObjective => PublicEventObjective.DefendAgainstTheExileAssault;
        protected override PublicEventObjective DefendGeneratorObjective => PublicEventObjective.DefendTheGeneratorAgainstExileAttackers;
        protected override PublicEventObjective FallBackToGeneratorObjective => PublicEventObjective.FallBackToTheGeneratorExileAssault;
        protected override PublicEventObjective HoldOutAgainstAssaultObjective => PublicEventObjective.HoldOutAgainstTheExileAssault;

        protected override void ActivateDefendAgainstAssaultObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.DefendAgainstTheExileAssault);
        }

        protected override void ActivateDefendGeneratorObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.DefendTheGeneratorAgainstExileAttackers);
        }

        protected override void ActivateFallBackToGeneratorObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.FallBackToTheGeneratorExileAssault);
        }

        protected override void ActivateHoldOutAgainstAssaultObjective()
        {
            PublicEvent.ActivateObjective(PublicEventObjective.HoldOutAgainstTheExileAssault);
        }
    }
}
