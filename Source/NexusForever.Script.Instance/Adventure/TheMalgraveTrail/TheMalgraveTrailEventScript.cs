using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheMalgraveTrail
{
    /// <summary>
    /// Parent adventure event. Caravan stop events, mine routing, resource
    /// collection, teleporter routing, and rookie/medal objectives remain
    /// blocked until retail route producers are mapped.
    /// </summary>
    [ScriptFilterOwnerId(53)]
    public class TheMalgraveTrailEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.LeadTheCaravanToFortWestwatchSafely);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.LeadTheCaravanToFortWestwatchSafely:
                    publicEvent.ActivateObjective(PublicEventObjective.LeadTheCaravanToFortWestwatchSafely);
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

            if ((PublicEventObjective)objective.Entry.Id == PublicEventObjective.LeadTheCaravanToFortWestwatchSafely)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
