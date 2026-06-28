using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Build 16042 maps both Captain Weir talk objectives to TargetGroup 12996,
    /// whose Creature2 member is Captain Weir 70999.
    /// </summary>
    [ScriptFilterCreatureId(70999u)]
    public class CaptainWeirEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint CaptainWeirTalkTargetGroupId = 12996u;

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
                CaptainWeirTalkTargetGroupId,
                1);
        }
    }
}
