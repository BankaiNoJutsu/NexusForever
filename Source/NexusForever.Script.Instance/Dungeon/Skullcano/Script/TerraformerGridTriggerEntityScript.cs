using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(8218)]
    public class TerraformerGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // WIP-guessed from LaughingWS Instances-and-more: the branch ties
            // trigger owner 8218 to the "Reach the Eldan Terraformer" objective.
            // Current UpdateObjective<T> expects a progress count, so use one
            // completion tick instead of copying the branch's owner id as count.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ReachTheEldanTerraformer, 1);
        }
    }
}
