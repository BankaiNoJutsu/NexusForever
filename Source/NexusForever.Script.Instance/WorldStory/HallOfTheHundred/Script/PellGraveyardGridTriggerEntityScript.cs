using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    /// <summary>
    /// Build 16042 maps side-event objective 4451 to ParticipantsInTriggerVolume
    /// object 7895 at WorldLocation2 48675; main-event objective 5295 is the
    /// paired Script marker at nearby WorldLocation2 48673.
    /// </summary>
    [ScriptFilterOwnerId(7895)]
    public class PellGraveyardGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint PellGraveyardObjectId = 7895u;

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
            trigger.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ParticipantsInTriggerVolume,
                PellGraveyardObjectId,
                1);
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.InvestigatePellGraveyardParent, 1);
        }
    }
}
