using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.RageLogic
{
    [ScriptFilterOwnerId(213)]
    public class RageLogicVehicleChoiceEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.ChooseAVehicle);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            if ((PublicEventPhase)phase != PublicEventPhase.ChooseAVehicle)
                return;

            publicEvent.ActivateObjective(PublicEventObjective.ChooseVehicle);
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            if ((PublicEventObjective)objective.Entry.Id == PublicEventObjective.ChooseVehicle)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
