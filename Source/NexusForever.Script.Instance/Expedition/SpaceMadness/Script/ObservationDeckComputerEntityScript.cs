using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness.Script
{
    /// <summary>
    /// Build 16042 maps objective 1589 to TargetGroup 6356,
    /// whose Creature2 member is the Lab Computer 45972.
    /// </summary>
    [ScriptFilterCreatureId(45972u)]
    public class ObservationDeckComputerEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ObservationDeckComputerTargetGroupId = 6356u;

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
                ObservationDeckComputerTargetGroupId,
                1);
        }
    }
}
