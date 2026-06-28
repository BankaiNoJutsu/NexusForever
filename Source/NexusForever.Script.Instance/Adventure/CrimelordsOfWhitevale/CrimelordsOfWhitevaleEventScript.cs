using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale
{
    /// <summary>
    /// Parent adventure event. Criminal Enterprise, War in the Streets, and
    /// final clubhouse branch events remain blocked until retail routing,
    /// loyalty, medal, and gang-branch producers are mapped.
    /// </summary>
    [ScriptFilterOwnerId(146)]
    public class CrimelordsOfWhitevaleEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool findKillerPhaseStarted;
        private bool biggestGangstersPhaseStarted;
        private bool returnClubhousePhaseStarted;
        private bool finalOnslaughtPhaseStarted;
        private bool avengePhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.GetOnYourHoverbike);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.GetOnYourHoverbike:
                    publicEvent.ActivateObjective(PublicEventObjective.GetOnYourHoverbike);
                    break;
                case PublicEventPhase.FindOutWhoKilledTheBloodScions:
                    publicEvent.ActivateObjective(PublicEventObjective.FindOutWhoKilledTheBloodScions);
                    break;
                case PublicEventPhase.BecomeTheBiggestGangstersInThermock:
                    publicEvent.ActivateObjective(PublicEventObjective.BecomeTheBiggestGangstersInThermock);
                    break;
                case PublicEventPhase.ReturnToTheBloodScionsClubhouse:
                    publicEvent.ActivateObjective(PublicEventObjective.ReturnToTheBloodScionsClubhouse);
                    break;
                case PublicEventPhase.SurviveTheFinalOnslaught:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheFinalOnslaught);
                    break;
                case PublicEventPhase.AvengeTheBloodScions:
                    publicEvent.ActivateObjective(PublicEventObjective.AvengeTheBloodScions);
                    break;
            }
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.GetOnYourHoverbike:
                    StartFindKillerPhase();
                    break;
                case PublicEventObjective.FindOutWhoKilledTheBloodScions:
                    StartBiggestGangstersPhase();
                    break;
                case PublicEventObjective.BecomeTheBiggestGangstersInThermock:
                    StartReturnClubhousePhase();
                    break;
                case PublicEventObjective.ReturnToTheBloodScionsClubhouse:
                    StartFinalOnslaughtPhase();
                    break;
                case PublicEventObjective.SurviveTheFinalOnslaught:
                    StartAvengePhase();
                    break;
                case PublicEventObjective.AvengeTheBloodScions:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartFindKillerPhase()
        {
            if (findKillerPhaseStarted)
                return;

            findKillerPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FindOutWhoKilledTheBloodScions);
        }

        private void StartBiggestGangstersPhase()
        {
            if (biggestGangstersPhaseStarted)
                return;

            biggestGangstersPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.BecomeTheBiggestGangstersInThermock);
        }

        private void StartReturnClubhousePhase()
        {
            if (returnClubhousePhaseStarted)
                return;

            returnClubhousePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReturnToTheBloodScionsClubhouse);
        }

        private void StartFinalOnslaughtPhase()
        {
            if (finalOnslaughtPhaseStarted)
                return;

            finalOnslaughtPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.SurviveTheFinalOnslaught);
        }

        private void StartAvengePhase()
        {
            if (avengePhaseStarted)
                return;

            avengePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.AvengeTheBloodScions);
        }
    }
}
