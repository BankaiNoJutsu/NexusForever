using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    /// <summary>
    /// Build 16042 maps side-event objective 4379 to ParticipantsInTriggerVolume
    /// object 7855 at WorldLocation2 48428. Main-event objective 5292 is the
    /// paired Script marker for exploring the same Yeti Cave route.
    /// </summary>
    [ScriptFilterOwnerId(7855)]
    public class YetiCaveExitGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint YetiCaveExitObjectId = 7855u;

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
                YetiCaveExitObjectId,
                1);
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ExploreYetiCaveParent, 1);
        }
    }
}
