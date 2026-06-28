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
            publicEvent.SetPhase(PublicEventPhase.TalkToCaptainTyrania);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToCaptainTyrania:
                    // Build 16042 PublicEventObjective 1844 is the opening Captain Tyrania
                    // TalkTo step before the aggregate crew checklist.
                    publicEvent.ActivateObjective(PublicEventObjective.TalkTo48900InTheGalacticObserversStarhelmCommandDeck);
                    break;
                case PublicEventPhase.TalkToCrewMembers:
                    // WIP-guessed from LaughingWS Instances-and-more: the branch exposes
                    // this phase/objective pairing but no event script. Keep the
                    // follow-up route to the build-16042 containment-cell objective until
                    // stronger proof maps the later doors, cinematics, and encounter order.
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToCrewMembers);
                    break;
                case PublicEventPhase.DisableSpecimenContainmentCells:
                    publicEvent.ActivateObjective(PublicEventObjective.DisableSpecimenContainmentCells);
                    break;
                case PublicEventPhase.KillSteelfinForces:
                    // Build 16042 PublicEventObjective 1846 is the immediate
                    // post-containment Steel Serpent combat step, phase 647.
                    publicEvent.ActivateObjective(PublicEventObjective.KillSteelfinForces);
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
                case PublicEventObjective.TalkTo48900InTheGalacticObserversStarhelmCommandDeck:
                    publicEvent.SetPhase(PublicEventPhase.TalkToCrewMembers);
                    break;
                case PublicEventObjective.TalkToCrewMembers:
                    publicEvent.SetPhase(PublicEventPhase.DisableSpecimenContainmentCells);
                    break;
                case PublicEventObjective.DisableSpecimenContainmentCells:
                    publicEvent.SetPhase(PublicEventPhase.KillSteelfinForces);
                    break;
                case PublicEventObjective.KillSteelfinForces:
                    // Stop at the first proven Steel Serpent combat objective.
                    // Later rescue, Engineer Clamp, mainframe, and bridge routing
                    // remain blocked pending retail interaction/encounter proof.
                    break;
            }
        }
    }
}
