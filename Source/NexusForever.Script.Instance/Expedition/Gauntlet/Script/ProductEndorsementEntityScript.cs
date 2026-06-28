using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Gauntlet.Script
{
    /// <summary>
    /// Build 16042 maps objective 2001 to TargetGroup 7038, whose members are
    /// the five endorsement products 49218, 49429, 49486, 49536, and 49573.
    /// </summary>
    [ScriptFilterCreatureId(49218u, 49429u, 49486u, 49536u, 49573u)]
    public class ProductEndorsementEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ProductEndorsementTargetGroupId = 7038u;

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
                ProductEndorsementTargetGroupId,
                1);
        }
    }
}
