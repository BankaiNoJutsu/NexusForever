using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.RiotInTheVoid
{
    /// <summary>
    /// Running the Asylum intro for Riot in the Void. Build 16042 objective rows
    /// map the opening route as Agent Triphon report, briefing, Warden holostation,
    /// Warden situation briefing, then the first Warden's Office movement gate.
    /// </summary>
    [ScriptFilterOwnerId(178)]
    public class RiotInTheVoidIntroEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool missionBriefingPhaseStarted;
        private bool reportToWardenPhaseStarted;
        private bool situationBriefingPhaseStarted;
        private bool crossRiotPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.ReportToAgentTriphon);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ReportToAgentTriphon:
                    publicEvent.ActivateObjective(PublicEventObjective.ReportToAgentTriphon);
                    break;
                case PublicEventPhase.ReceiveMissionBriefing:
                    publicEvent.ActivateObjective(PublicEventObjective.ReceiveMissionBriefing);
                    publicEvent.UpdateObjective(PublicEventObjective.ReceiveMissionBriefing, 1);
                    break;
                case PublicEventPhase.ReportToWardenViaHolostation:
                    publicEvent.ActivateObjective(PublicEventObjective.ReportToWardenViaHolostation);
                    break;
                case PublicEventPhase.ReceiveSituationBriefing:
                    publicEvent.ActivateObjective(PublicEventObjective.ReceiveSituationBriefing);
                    publicEvent.UpdateObjective(PublicEventObjective.ReceiveSituationBriefing, 1);
                    break;
                case PublicEventPhase.CrossRiotAndReachWardensOffice:
                    publicEvent.ActivateObjective(PublicEventObjective.CrossRiotAndReachWardensOffice);
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
                case PublicEventObjective.ReportToAgentTriphon:
                    StartMissionBriefingPhase();
                    break;
                case PublicEventObjective.ReceiveMissionBriefing:
                    StartReportToWardenPhase();
                    break;
                case PublicEventObjective.ReportToWardenViaHolostation:
                    StartSituationBriefingPhase();
                    break;
                case PublicEventObjective.ReceiveSituationBriefing:
                    StartCrossRiotPhase();
                    break;
                case PublicEventObjective.CrossRiotAndReachWardensOffice:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartMissionBriefingPhase()
        {
            if (missionBriefingPhaseStarted)
                return;

            missionBriefingPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReceiveMissionBriefing);
        }

        private void StartReportToWardenPhase()
        {
            if (reportToWardenPhaseStarted)
                return;

            reportToWardenPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReportToWardenViaHolostation);
        }

        private void StartSituationBriefingPhase()
        {
            if (situationBriefingPhaseStarted)
                return;

            situationBriefingPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReceiveSituationBriefing);
        }

        private void StartCrossRiotPhase()
        {
            if (crossRiotPhaseStarted)
                return;

            crossRiotPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.CrossRiotAndReachWardensOffice);
        }
    }
}
