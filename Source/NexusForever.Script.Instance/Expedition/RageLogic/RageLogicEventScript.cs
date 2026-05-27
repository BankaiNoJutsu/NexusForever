using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.RageLogic
{
    [ScriptFilterOwnerId(213)]
    public class RageLogicEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;

            // WIP-guessed from LaughingWS Instances-and-more: the branch only
            // supplies this phase enum and no objective or vehicle routing script.
            // Set the visible vehicle-choice phase, but leave the actual choice
            // behavior, objective activation, rewards, and encounter flow blocked.
            publicEvent.SetPhase(PublicEventPhase.ChooseAVehicle);
        }
    }
}
