using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    [ScriptFilterOwnerId(3420)]
    public class MoldwoodCorruptionGridTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
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

            // Build 16042 objective 614 is the Moldwood corruption Turnstile row
            // for objectId 3420. Credit the direct route objective; exact route
            // selection is still not retail-proven.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ReachTheMoldwoodCorruption, 1);
        }
    }
}
