using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(2909)]
    public class PlatformTriggerGuidEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // WIP-guessed from LaughingWS Instances-and-more: trigger owner 2909
            // appears to satisfy the Redmoon platform script objective. Exact
            // platform choreography still needs manual dungeon smoke.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Script, 2909u, 1);
        }
    }
}
