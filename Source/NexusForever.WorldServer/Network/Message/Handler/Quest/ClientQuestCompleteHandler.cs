using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestCompleteHandler : IMessageHandler<IWorldSession, ClientQuestComplete>
    {
        private readonly ILogger<ClientQuestCompleteHandler> log;

        public ClientQuestCompleteHandler(ILogger<ClientQuestCompleteHandler> log = null)
        {
            this.log = log ?? NullLogger<ClientQuestCompleteHandler>.Instance;
        }

        public void HandleMessage(IWorldSession session, ClientQuestComplete questComplete)
        {
            bool publishingAfterSave = false;
            try
            {
                if (session.Player is Player actor && !ReferenceEquals(actor.Session, session)) return;
                if (session.Player is Player && session.Player.QuestManager is QuestManager quests)
                    publishingAfterSave = quests.QuestCompleteFromClient(questComplete.QuestId, questComplete.RewardSelection,
                        questComplete.IsCommunicatorMsg, () => DialogSessionState.EndActiveDialog(session));
                else
                    session.Player.QuestManager.QuestComplete(questComplete.QuestId, questComplete.RewardSelection, questComplete.IsCommunicatorMsg);
            }
            catch (QuestException exception)
            {
                log.LogWarning(exception,
                    "Rejected quest completion from player {PlayerGuid}: quest={QuestId}, rewardSelection={RewardSelection}, communicator={IsCommunicator}.",
                    session.Player?.Guid,
                    questComplete.QuestId,
                    questComplete.RewardSelection,
                    questComplete.IsCommunicatorMsg);
                session.Player?.SendGenericError(GenericError.Params);
                session.Player?.QuestManager.SendQuestState(questComplete.QuestId);
                return;
            }

            if (!publishingAfterSave && !questComplete.IsCommunicatorMsg)
                DialogSessionState.EndActiveDialog(session);
        }
    }
}
