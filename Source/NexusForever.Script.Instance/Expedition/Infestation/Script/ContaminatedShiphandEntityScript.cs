using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 231 to "Use medicine to heal $m(creature=23837)".
    /// Exact medicine cast/CSI timing still needs client smoke.
    /// </summary>
    [ScriptFilterCreatureId(23837u)]
    public class ContaminatedShiphandEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool activated;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked when this entity activation succeeds.
        /// </summary>
        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.HealContaminatedShiphand, 1);
        }
    }
}
