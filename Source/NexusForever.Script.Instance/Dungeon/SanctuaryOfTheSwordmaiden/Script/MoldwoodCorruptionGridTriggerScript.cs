using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
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

            // WIP-guessed from LaughingWS Instances-and-more: trigger owner 3420
            // appears to satisfy the Moldwood corruption route script objective.
            // Exact route selection is still not retail-proven.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Script, 3420u, 1);
        }
    }
}
