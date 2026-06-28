using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 1007 to TargetGroup 4516,
    /// whose Creature2 member is Hull Breach 36844.
    /// </summary>
    [ScriptFilterCreatureId(36844u)]
    public class HullBreachEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint HullBreachTargetGroupId = 4516u;

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
                HullBreachTargetGroupId,
                entity.QuestChecklistIdx);
        }
    }
}
