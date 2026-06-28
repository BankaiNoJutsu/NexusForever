using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheMalgraveTrail
{
    /// <summary>
    /// Exodus setup event for The Malgrave Trail. Build 16042 PE 56 opens with
    /// the survivor rally and Caravan Master Braithwait story sequence, then
    /// routes players toward the first supply-gathering gate.
    /// </summary>
    [ScriptFilterOwnerId(56)]
    public class TheMalgraveTrailExodusEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool talkInThirstyCreekPhaseStarted;
        private bool talkNearKurgPhaseStarted;
        private bool listenToStoryPhaseStarted;
        private bool rallyAroundInnPhaseStarted;
        private bool rallyNearWreckedShipPhaseStarted;
        private bool talkInTownCenterPhaseStarted;
        private bool collectSuppliesPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.RallySurvivorsAtTownCenter);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.RallySurvivorsAtTownCenter:
                    publicEvent.ActivateObjective(PublicEventObjective.RallySurvivorsAtTownCenter);
                    break;
                case PublicEventPhase.TalkToBraithwaitInThirstyCreek:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToBraithwaitInThirstyCreek);
                    break;
                case PublicEventPhase.TalkToBraithwaitNearKurg:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToBraithwaitNearKurg);
                    break;
                case PublicEventPhase.ListenToBraithwaitStory:
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToBraithwaitStory);
                    break;
                case PublicEventPhase.RallySurvivorsAroundInn:
                    publicEvent.ActivateObjective(PublicEventObjective.RallySurvivorsAroundInn);
                    publicEvent.UpdateObjective(PublicEventObjective.RallySurvivorsAroundInn, 1);
                    break;
                case PublicEventPhase.RallySurvivorsNearWreckedShip:
                    publicEvent.ActivateObjective(PublicEventObjective.RallySurvivorsNearWreckedShip);
                    publicEvent.UpdateObjective(PublicEventObjective.RallySurvivorsNearWreckedShip, 1);
                    break;
                case PublicEventPhase.TalkToBraithwaitInTownCenter:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToBraithwaitInTownCenter);
                    break;
                case PublicEventPhase.CollectFeedSacksWaterBarrelsAndFoodCrates:
                    publicEvent.ActivateObjective(PublicEventObjective.CollectFeedSacksWaterBarrelsAndFoodCrates);
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
                case PublicEventObjective.RallySurvivorsAtTownCenter:
                    StartTalkInThirstyCreekPhase();
                    break;
                case PublicEventObjective.TalkToBraithwaitInThirstyCreek:
                    StartTalkNearKurgPhase();
                    break;
                case PublicEventObjective.TalkToBraithwaitNearKurg:
                    StartListenToStoryPhase();
                    break;
                case PublicEventObjective.ListenToBraithwaitStory:
                    StartRallyAroundInnPhase();
                    break;
                case PublicEventObjective.RallySurvivorsAroundInn:
                    StartRallyNearWreckedShipPhase();
                    break;
                case PublicEventObjective.RallySurvivorsNearWreckedShip:
                    StartTalkInTownCenterPhase();
                    break;
                case PublicEventObjective.TalkToBraithwaitInTownCenter:
                    StartCollectSuppliesPhase();
                    break;
            }
        }

        private void StartTalkInThirstyCreekPhase()
        {
            if (talkInThirstyCreekPhaseStarted)
                return;

            talkInThirstyCreekPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.TalkToBraithwaitInThirstyCreek);
        }

        private void StartTalkNearKurgPhase()
        {
            if (talkNearKurgPhaseStarted)
                return;

            talkNearKurgPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.TalkToBraithwaitNearKurg);
        }

        private void StartListenToStoryPhase()
        {
            if (listenToStoryPhaseStarted)
                return;

            listenToStoryPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ListenToBraithwaitStory);
        }

        private void StartRallyAroundInnPhase()
        {
            if (rallyAroundInnPhaseStarted)
                return;

            rallyAroundInnPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.RallySurvivorsAroundInn);
        }

        private void StartRallyNearWreckedShipPhase()
        {
            if (rallyNearWreckedShipPhaseStarted)
                return;

            rallyNearWreckedShipPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.RallySurvivorsNearWreckedShip);
        }

        private void StartTalkInTownCenterPhase()
        {
            if (talkInTownCenterPhaseStarted)
                return;

            talkInTownCenterPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.TalkToBraithwaitInTownCenter);
        }

        private void StartCollectSuppliesPhase()
        {
            if (collectSuppliesPhaseStarted)
                return;

            collectSuppliesPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.CollectFeedSacksWaterBarrelsAndFoodCrates);
        }
    }
}
