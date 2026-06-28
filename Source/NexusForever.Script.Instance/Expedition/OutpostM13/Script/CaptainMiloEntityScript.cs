using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.OutpostM13.Script
{
    /// <summary>
    /// Build 16042 maps objective 1441 to TargetGroup 5308,
    /// whose reviewed opening placement uses Captain Milo Creature2 41201.
    /// </summary>
    [ScriptFilterCreatureId(41201u)]
    public class CaptainMiloEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint CaptainMiloTalkTargetGroupId = 5308u;

        private IWorldEntity entity;

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
            entity.Map.PublicEventManager.UpdateObjective(
                player,
                PublicEventObjectiveType.TalkTo,
                CaptainMiloTalkTargetGroupId,
                1);
        }
    }
}
