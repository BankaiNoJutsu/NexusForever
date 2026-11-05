using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Game.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestRetryHandler : IMessageHandler<IWorldSession, ClientQuestRetry>
    {
        public void HandleMessage(IWorldSession session, ClientQuestRetry questRetry)
        {
            var player = session.Player;
            if (player is Player owner && !ReferenceEquals(owner.Session, session)) return;

            if (player is Player && player.QuestManager is QuestManager quests)
                quests.QuestRetryFromClient(questRetry.QuestId);
            else
                player.QuestManager.QuestRetry(questRetry.QuestId);
        }
    }
}
