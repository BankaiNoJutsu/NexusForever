using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation.Script
{
    /// <summary>
    /// Build 16042 maps objective 230 to TargetGroup 2323,
    /// whose Creature2 member is Medical Supplies 22489.
    /// </summary>
    [ScriptFilterCreatureId(22489u)]
    public class MedicalSuppliesEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint MedicalSuppliesTargetGroupId = 2323u;

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
                PublicEventObjectiveType.ActivateTargetGroup,
                MedicalSuppliesTargetGroupId,
                1);
        }
    }
}
