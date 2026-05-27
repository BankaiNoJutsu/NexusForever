using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.EventInstances.ProtostarsSuperMallInTheSky
{
    [ScriptFilterOwnerId(679)]
    public class ProtostarsSuperMallInTheSkyEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Protostar SuperMall in the Sky requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    // WIP-guessed from LaughingWS Instances-and-more plus the WIP
                    // instance seed's event 679 phase-0 gather marker. The deeper
                    // random store/event routing remains blocked pending retail smoke.
                    publicEvent.ActivateObjective(
                        PublicEventObjective.GatherToSpeakToTheProtostarMallGreeter,
                        mapInstance.PlayerCount);
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
                case PublicEventObjective.GatherToSpeakToTheProtostarMallGreeter:
                    // WIP-guessed branch handoff: the branch exposes RandomPath phases
                    // but no event script. Advance only to the coarse random-path gate
                    // and leave store selection/objective routing blocked.
                    publicEvent.SetPhase(PublicEventPhase.RandomPath);
                    break;
            }
        }
    }
}
