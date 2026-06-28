using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness.Script
{
    /// <summary>
    /// Build 16042 maps objective 1579 to TargetGroup 6342,
    /// whose Creature2 member is Captain Tero 45900.
    /// </summary>
    [ScriptFilterCreatureId(45900u)]
    public class CaptainTeroEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint CaptainTeroTalkTargetGroupId = 6342u;

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
                CaptainTeroTalkTargetGroupId,
                1);
        }
    }
}
