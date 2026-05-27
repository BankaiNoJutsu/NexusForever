using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    [ScriptFilterOwnerId(1831)]
    public class StopTheThundercallHighPriestGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // WIP-guessed from LaughingWS Instances-and-more owner 1831. The branch passed the owner id as progress;
            // this port records one trigger tick until retail packet evidence confirms the exact objective count.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.StopTheThundercallHighPriest, 1);
        }
    }
}
