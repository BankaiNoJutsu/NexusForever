using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.OutpostM13.Script
{
    /// <summary>
    /// Build 16042 maps objective 262 to TargetGroup 5294,
    /// whose Creature2 member is Exit Panel 23470.
    /// </summary>
    [ScriptFilterCreatureId(23470u)]
    public class ExitPanelEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ExitPanelTargetGroupId = 5294u;

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
                ExitPanelTargetGroupId,
                1);
        }
    }
}
