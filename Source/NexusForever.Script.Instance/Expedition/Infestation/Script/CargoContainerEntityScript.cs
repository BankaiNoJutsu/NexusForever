using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 4699 to TargetGroup 12593,
    /// whose Creature2 member is Cargo Container 69877.
    /// </summary>
    [ScriptFilterCreatureId(69877u)]
    public class CargoContainerEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint CargoContainerTargetGroupId = 12593u;

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
                CargoContainerTargetGroupId,
                entity.QuestChecklistIdx);
        }
    }
}
