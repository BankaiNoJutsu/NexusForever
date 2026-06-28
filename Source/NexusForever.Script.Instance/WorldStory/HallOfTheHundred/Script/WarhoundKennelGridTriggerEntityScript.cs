using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    /// <summary>
    /// Build 16042 maps side-event objective 4403 to ParticipantsInTriggerVolume
    /// object 8426 at WorldLocation2 48660. Main-event objective 5294 is the
    /// matching Script parent marker at the same location.
    /// </summary>
    [ScriptFilterOwnerId(8426)]
    public class WarhoundKennelGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint WarhoundKennelObjectId = 8426u;

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
                WarhoundKennelObjectId,
                1);
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ExploreWarhoundKennelParent, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps side-event objective 4434 to ParticipantsInTriggerVolume
    /// object 7881 at WorldLocation2 48661.
    /// </summary>
    [ScriptFilterOwnerId(7881)]
    public class WarhoundKennelDepthGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint WarhoundKennelDepthObjectId = 7881u;

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
                WarhoundKennelDepthObjectId,
                1);
        }
    }
}
