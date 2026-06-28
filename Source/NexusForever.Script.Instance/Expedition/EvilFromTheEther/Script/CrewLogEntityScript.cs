using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterCreatureId(71234)]
    public class CrewLogEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool downloaded;

        #region Dependency Injection

        private readonly IGlobalQuestManager globalQuestManager;

        public CrewLogEntityScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IWorldEntity entity)
        {
            this.entity = entity;
        }

        /// <summary>
        /// Invoked when <see cref="IWorldEntity"/> is successfully activated by <see cref="IPlayer"/>.
        /// </summary>
        public void OnActivateSuccess(IPlayer _)
        {
            if (entity.Map is not IMapInstance mapInstance)
                return;

            if (!TryGetCommunicatorMessage(entity.QuestChecklistIdx, out CommunicatorMessage communicatorMessageId))
                return;

            // Creature 71234 maps checklist indexes 0..6 to CrewLog1..CrewLog7 in the
            // current slice. Exact playback targeting and any retail filtering remain
            // unverified.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(communicatorMessageId);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);

            if (downloaded)
                return;

            downloaded = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.DownloadCrewLogs, 1);
        }

        private static bool TryGetCommunicatorMessage(byte checklistIndex, out CommunicatorMessage communicatorMessage)
        {
            communicatorMessage = checklistIndex switch
            {
                0 => CommunicatorMessage.CrewLog1,
                1 => CommunicatorMessage.CrewLog2,
                2 => CommunicatorMessage.CrewLog3,
                3 => CommunicatorMessage.CrewLog4,
                4 => CommunicatorMessage.CrewLog5,
                5 => CommunicatorMessage.CrewLog6,
                6 => CommunicatorMessage.CrewLog7,
                _ => 0
            };

            return communicatorMessage != 0;
        }
    }
}
