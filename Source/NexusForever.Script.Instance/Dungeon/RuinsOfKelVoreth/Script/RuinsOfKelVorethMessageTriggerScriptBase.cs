using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Script.Template;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    public abstract class RuinsOfKelVorethMessageTriggerScriptBase : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly CommunicatorMessage defaultMessage;
        private readonly CommunicatorMessage? dominionMessage;

        private IGridTriggerEntity trigger;

        protected RuinsOfKelVorethMessageTriggerScriptBase(
            IGlobalQuestManager globalQuestManager,
            CommunicatorMessage defaultMessage,
            CommunicatorMessage? dominionMessage = null)
        {
            this.globalQuestManager = globalQuestManager;
            this.defaultMessage     = defaultMessage;
            this.dominionMessage    = dominionMessage;
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

            // WIP-guessed from LaughingWS Instances-and-more Ruins trigger scaffolds. The branch lists
            // paired Avra/Toric messages but leaves the faction routing as a TODO, so this port keeps
            // trigger delivery player-gated, sends Avra/default for unknown or Exile players, and maps
            // Dominion players to Toric only where the branch supplied a paired message.
            foreach (IPlayer player in mapInstance.GetPlayers())
                Send(player, SelectMessage(player));
        }

        private CommunicatorMessage SelectMessage(IPlayer player)
        {
            if (dominionMessage != null && player.Faction1 == Faction.Dominion)
                return dominionMessage.Value;

            return defaultMessage;
        }

        private void Send(IPlayer player, CommunicatorMessage communicatorMessageId)
        {
            ICommunicatorMessage message = globalQuestManager.GetCommunicatorMessage(communicatorMessageId);
            message?.Send(player.Session);
        }
    }
}
