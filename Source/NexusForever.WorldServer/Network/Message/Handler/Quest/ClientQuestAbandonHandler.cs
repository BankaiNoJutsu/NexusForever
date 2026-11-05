using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Game.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestAbandonHandler : IMessageHandler<IWorldSession, ClientQuestAbandon>
    {
        public void HandleMessage(IWorldSession session, ClientQuestAbandon questAbandon)
        {
            var player = session.Player;
            if (player is Player owner && !ReferenceEquals(owner.Session, session)) return;

            if (player is Player && player.QuestManager is QuestManager quests)
                quests.QuestAbandonFromClient(questAbandon.QuestId);
            else
                player.QuestManager.QuestAbandon(questAbandon.QuestId);
        }
    }
}
