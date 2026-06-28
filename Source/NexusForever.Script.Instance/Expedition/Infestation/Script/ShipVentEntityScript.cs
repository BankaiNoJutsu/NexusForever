using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 228 to TargetGroup 2324,
    /// whose Creature2 member is Shiphand Infestation vent 22493.
    /// </summary>
    [ScriptFilterCreatureId(22493u)]
    public class ShipVentEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ShipVentTargetGroupId = 2324u;

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
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                ShipVentTargetGroupId,
                entity.QuestChecklistIdx);
        }
    }
}
