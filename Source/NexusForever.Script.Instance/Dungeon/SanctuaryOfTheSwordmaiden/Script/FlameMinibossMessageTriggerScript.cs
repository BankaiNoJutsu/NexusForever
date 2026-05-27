using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    [ScriptFilterOwnerId(504)]
    public class FlameMinibossMessageTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IGridTriggerEntity trigger;

        public FlameMinibossMessageTriggerScript(
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

            // WIP-guessed from LaughingWS Instances-and-more: trigger owner 504
            // appears to play Selene's flame miniboss warning. The branch sent
            // this for any entity entering range; keep it player-gated here
            // until exact trigger filtering and timing are smoke-tested.
            ICommunicatorMessage message = globalQuestManager.GetCommunicatorMessage(CommunicatorMessage.SpiritMotherSelene8);
            foreach (IPlayer player in mapInstance.GetPlayers())
                message?.Send(player.Session);
        }
    }
}
