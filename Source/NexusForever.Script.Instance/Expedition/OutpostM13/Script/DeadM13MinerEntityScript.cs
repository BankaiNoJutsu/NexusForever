using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.OutpostM13.Script
{
    /// <summary>
    /// Build 16042 maps objective 656 to VirtualCollect 113. The reward-pane
    /// TargetGroup 10832 includes Dead M-13 Miner 29512, which has reviewed
    /// Outpost M-13 public-event and spawn evidence.
    /// </summary>
    [ScriptFilterCreatureId(29512u)]
    public class DeadM13MinerEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint DatachronVirtualItemId = 113u;

        private IWorldEntity entity;
        private bool collected;

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
            if (collected)
                return;

            collected = true;
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.VirtualCollect,
                DatachronVirtualItemId,
                1);
            entity.RemoveFromMap();
        }
    }
}
