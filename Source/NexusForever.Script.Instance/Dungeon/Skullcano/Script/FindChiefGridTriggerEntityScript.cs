using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(362)]
    public class FindChiefGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // WIP-guessed from LaughingWS Instances-and-more: branch maps the
            // Find Chief trigger to script objective 3953. The cave route now
            // has objective-phase scaffolding, but trigger placement and Chief
            // Kaskalak choreography still need smoke proof.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Script, 3953u, 1);
        }
    }
}
