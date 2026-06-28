using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.JourneyIntoOMNICore1
{
    [ScriptFilterOwnerId(605)]
    public class JourneyIntoOMNICore1EventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool belleSpokenTo;
        private bool axisSpokenTo;
        private bool listenPhaseStarted;
        private bool belleListenedTo;
        private bool axisListenedTo;
        private bool choosePathPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.SpeakWithBelleAndAxis);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.SpeakWithBelleAndAxis:
                    // Build 16042 objectives 2740/2741 are TalkTo rows for
                    // TargetGroups 10592/10593: Belle Walker and Axis Pheydra.
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithBelleWalker);
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithAxisPheydra);
                    break;
                case PublicEventPhase.ListenToBelleAndAxis:
                    // Build 16042 objectives 2742/2743 are count-one Script rows
                    // in quest direction 2219. Credit them directly until the exact
                    // companion dialog timing and cinematic payload are proven.
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToBelleWalker);
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToAxisPheydra);
                    publicEvent.UpdateObjective(PublicEventObjective.ListenToBelleWalker, 1);
                    publicEvent.UpdateObjective(PublicEventObjective.ListenToAxisPheydra, 1);
                    break;
                case PublicEventPhase.ChoosePath:
                    // Build 16042 objective 2744 is the first unresolved branch gate.
                    // Stop here pending evidence for route selection and downstream
                    // firewall/Power Continuum choreography.
                    publicEvent.ActivateObjective(PublicEventObjective.ChoosePathAOrB);
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
                case PublicEventObjective.SpeakWithBelleWalker:
                    belleSpokenTo = true;
                    StartListenPhaseWhenReady();
                    break;
                case PublicEventObjective.SpeakWithAxisPheydra:
                    axisSpokenTo = true;
                    StartListenPhaseWhenReady();
                    break;
                case PublicEventObjective.ListenToBelleWalker:
                    belleListenedTo = true;
                    StartChoosePathPhaseWhenReady();
                    break;
                case PublicEventObjective.ListenToAxisPheydra:
                    axisListenedTo = true;
                    StartChoosePathPhaseWhenReady();
                    break;
            }
        }

        private void StartListenPhaseWhenReady()
        {
            if (!belleSpokenTo || !axisSpokenTo || listenPhaseStarted)
                return;

            listenPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ListenToBelleAndAxis);
        }

        private void StartChoosePathPhaseWhenReady()
        {
            if (!belleListenedTo || !axisListenedTo || choosePathPhaseStarted)
                return;

            choosePathPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ChoosePath);
        }
    }
}
