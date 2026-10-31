using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
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
        private bool partySizeDirty;

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
                    foreach (var objective in (publicEvent.GetTeams() ?? []).SelectMany(t => t.GetObjectives()))
                        if (objective.Entry.Id == (uint)PublicEventObjective.ListenToDropShipBriefing)
                            objective.EnableTimedWinCompletion();
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToDropShipBriefing);
                    break;
                case PublicEventPhase.MeetWithExileAgent:
                    publicEvent.ActivateObjective(PublicEventObjective.MeetWithExileAgent, GetPartySize());
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

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IPlayer) partySizeDirty = true;
        }

        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IPlayer) partySizeDirty = true;
        }

        public void Update(double lastTick)
        {
            // Reconcile after grid actions and visibility cleanup, so departed
            // players cannot leave a stale trigger count that finishes the intro.
            if (!partySizeDirty || !meetAgentPhaseStarted || publicEvent.IsFinalised) return;
            partySizeDirty = false;
            foreach (var objective in publicEvent.GetTeams().SelectMany(t => t.GetObjectives()))
                if (objective.Entry.Id == (uint)PublicEventObjective.MeetWithExileAgent)
                    objective.SetDynamicMax(GetPartySize());
        }

        private uint GetPartySize()
        {
            return publicEvent.Map is IMapInstance map
                ? Math.Max(1u, (uint)map.GetPlayers().Select(p => p.CharacterId).Distinct().Count()) : 1;
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
