using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.OutpostM13.Script
{
    /// <summary>
    /// Build 16042 maps objective 1440 to TargetGroup 5295,
    /// whose Creature2 member is Foreman Krause 29468.
    /// </summary>
    [ScriptFilterCreatureId(29468u)]
    public class ForemanKrauseEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ForemanKrauseTalkTargetGroupId = 5295u;

        private IWorldEntity entity;
        private bool credited;

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
        public void OnActivateSuccess(IPlayer player)
        {
            if (credited)
                return;

            credited = true;
            entity.Map.PublicEventManager.UpdateObjective(
                player,
                PublicEventObjectiveType.TalkTo,
                ForemanKrauseTalkTargetGroupId,
                1);
        }
    }
}
