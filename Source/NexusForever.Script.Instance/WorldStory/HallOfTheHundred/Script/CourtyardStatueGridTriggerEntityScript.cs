using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    /// <summary>
    /// Build 16042 maps objective 4320 to ParticipantsInTriggerVolume object
    /// 7894. QuestDirection 2443 resolves to WorldLocation2 50605 in world 3009.
    /// </summary>
    [ScriptFilterOwnerId(7894)]
    public class CourtyardStatueGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private const uint CourtyardStatueObjectId = 7894u;

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
                CourtyardStatueObjectId,
                1);
        }
    }
}
