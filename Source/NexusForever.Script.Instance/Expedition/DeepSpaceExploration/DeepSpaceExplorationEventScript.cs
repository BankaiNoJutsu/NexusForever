using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.DeepSpaceExploration
{
    [ScriptFilterOwnerId(447)]
    public class DeepSpaceExplorationEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.TalkToCrewMembers);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToCrewMembers:
                    // WIP-guessed from LaughingWS Instances-and-more: the branch exposes
                    // this single phase/objective pairing but no event script. Keep the
                    // implementation to the first visible objective until route evidence
                    // proves the follow-up phases, doors, cinematics, and encounter order.
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToCrewMembers);
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
                case PublicEventObjective.TalkToCrewMembers:
                    // No safe follow-up phase exists in the branch. The objective can
                    // complete, but expedition routing remains blocked pending proof.
                    break;
            }
        }
    }
}
