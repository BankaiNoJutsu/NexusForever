using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;

namespace NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge
{
    /// <summary>
    /// Shared parent route for the mirrored Siege of Tempest Refuge adventure
    /// events. Exact wave producers, resource-pool damage, timed holdouts,
    /// rookie/medal rewards, and faction smoke remain blocked.
    /// </summary>
    public abstract class TheSiegeOfTempestRefugeEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool defendGeneratorPhaseStarted;
        private bool fallBackPhaseStarted;
        private bool holdOutPhaseStarted;

        protected IPublicEvent PublicEvent => publicEvent;

        protected abstract PublicEventObjective DefendAgainstAssaultObjective { get; }
        protected abstract PublicEventObjective DefendGeneratorObjective { get; }
        protected abstract PublicEventObjective FallBackToGeneratorObjective { get; }
        protected abstract PublicEventObjective HoldOutAgainstAssaultObjective { get; }

        protected abstract void ActivateDefendAgainstAssaultObjective();
        protected abstract void ActivateDefendGeneratorObjective();
        protected abstract void ActivateFallBackToGeneratorObjective();
        protected abstract void ActivateHoldOutAgainstAssaultObjective();

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.DefendAgainstAssault);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.DefendAgainstAssault:
                    ActivateDefendAgainstAssaultObjective();
                    break;
                case PublicEventPhase.DefendGenerator:
                    ActivateDefendGeneratorObjective();
                    break;
                case PublicEventPhase.FallBackToGenerator:
                    ActivateFallBackToGeneratorObjective();
                    break;
                case PublicEventPhase.HoldOutAgainstAssault:
                    ActivateHoldOutAgainstAssaultObjective();
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

            PublicEventObjective objectiveId = (PublicEventObjective)objective.Entry.Id;
            if (objectiveId == DefendAgainstAssaultObjective)
            {
                StartDefendGeneratorPhase();
                return;
            }

            if (objectiveId == DefendGeneratorObjective)
            {
                StartFallBackPhase();
                return;
            }

            if (objectiveId == FallBackToGeneratorObjective)
            {
                StartHoldOutPhase();
                return;
            }

            if (objectiveId == HoldOutAgainstAssaultObjective)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }

        private void StartDefendGeneratorPhase()
        {
            if (defendGeneratorPhaseStarted)
                return;

            defendGeneratorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefendGenerator);
        }

        private void StartFallBackPhase()
        {
            if (fallBackPhaseStarted)
                return;

            fallBackPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FallBackToGenerator);
        }

        private void StartHoldOutPhase()
        {
            if (holdOutPhaseStarted)
                return;

            holdOutPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.HoldOutAgainstAssault);
        }
    }
}
