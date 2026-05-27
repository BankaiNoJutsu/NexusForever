using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.InitializationCoreY83.Script
{
    [ScriptFilterOwnerId(2681)]
    public class CentralAccessCorridorTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IGridTriggerEntity trigger;

        public CentralAccessCorridorTriggerScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to range check range.
        /// </summary>
        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer)
                return;

            if (trigger.Map is not IMapInstance mapInstance)
                return;

            // WIP-guessed from LaughingWS Instances-and-more owner 2681. The branch notes this may be a group trigger;
            // keep the broadcast map-scoped until live/client evidence confirms a different target set.
            ICommunicatorMessage message = globalQuestManager.GetCommunicatorMessage(CommunicatorMessage.Nurton1);
            foreach (IPlayer player in mapInstance.GetPlayers())
                message?.Send(player.Session);
        }
    }
}
