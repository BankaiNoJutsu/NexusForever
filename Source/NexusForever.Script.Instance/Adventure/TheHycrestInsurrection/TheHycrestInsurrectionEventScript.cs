using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection
{
    /// <summary>
    /// Parent adventure event. Branch sub-events PE 420-434 remain blocked until
    /// the retail branch-selection trigger and timing are mapped.
    /// </summary>
    [ScriptFilterOwnerId(419)]
    public class TheHycrestInsurrectionEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.LeadTheHycrestRebelsToVictory);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.LeadTheHycrestRebelsToVictory:
                    publicEvent.ActivateObjective(PublicEventObjective.LeadTheHycrestRebelsToVictory);
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

            if ((PublicEventObjective)objective.Entry.Id == PublicEventObjective.LeadTheHycrestRebelsToVictory)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
