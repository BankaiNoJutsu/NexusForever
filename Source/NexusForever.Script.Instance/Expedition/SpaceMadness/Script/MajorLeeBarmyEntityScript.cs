using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness.Script
{
    /// <summary>
    /// Build 16042 maps objective 1588 to TargetGroup 6358,
    /// whose Creature2 member is Major Lee Barmy 45812.
    /// </summary>
    [ScriptFilterCreatureId(45812u)]
    public class MajorLeeBarmyEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint MajorLeeBarmyTalkTargetGroupId = 6358u;

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
                MajorLeeBarmyTalkTargetGroupId,
                1);
        }
    }
}
