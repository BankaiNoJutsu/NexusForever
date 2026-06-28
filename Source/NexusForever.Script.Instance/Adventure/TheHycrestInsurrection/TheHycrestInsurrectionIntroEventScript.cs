using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection
{
    /// <summary>
    /// Drop-ship intro for The Hycrest Insurrection. Build 16042 objective rows
    /// map the opening route as report to 18365, listen to the briefing, then
    /// meet 17778 in Hycrest.
    /// </summary>
    [ScriptFilterOwnerId(418)]
    public class TheHycrestInsurrectionIntroEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool briefingPhaseStarted;
        private bool meetAgentPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.ReportToDropShip);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ReportToDropShip:
                    publicEvent.ActivateObjective(PublicEventObjective.ReportToDropShip);
                    break;
                case PublicEventPhase.ListenToDropShipBriefing:
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToDropShipBriefing);
                    publicEvent.UpdateObjective(PublicEventObjective.ListenToDropShipBriefing, 1);
                    break;
                case PublicEventPhase.MeetWithExileAgent:
                    publicEvent.ActivateObjective(PublicEventObjective.MeetWithExileAgent);
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
                case PublicEventObjective.ReportToDropShip:
                    StartBriefingPhase();
                    break;
                case PublicEventObjective.ListenToDropShipBriefing:
                    StartMeetAgentPhase();
                    break;
                case PublicEventObjective.MeetWithExileAgent:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartBriefingPhase()
        {
            if (briefingPhaseStarted)
                return;

            briefingPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ListenToDropShipBriefing);
        }

        private void StartMeetAgentPhase()
        {
            if (meetAgentPhaseStarted)
                return;

            meetAgentPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetWithExileAgent);
        }
    }
}
