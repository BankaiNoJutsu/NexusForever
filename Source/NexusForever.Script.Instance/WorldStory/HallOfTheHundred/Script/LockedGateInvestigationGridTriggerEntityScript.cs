using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    [ScriptFilterOwnerId(7834)]
    public class LockedGateInvestigationGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint VaregorPassNavigationObjectId = 7879u;

        private bool entered;
        private IGridTriggerEntity trigger;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to range check range.
        /// </summary>
        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer)
                return;

            if (entered)
                return;

            entered = true;
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.InvestigateLockedGate, 1);
            trigger.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ParticipantsInTriggerVolume,
                VaregorPassNavigationObjectId,
                1);
        }
    }
}
