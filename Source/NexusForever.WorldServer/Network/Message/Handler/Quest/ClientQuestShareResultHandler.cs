using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Game.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestShareResultHandler : IMessageHandler<IWorldSession, ClientQuestShareResult>
    {
        public void HandleMessage(IWorldSession session, ClientQuestShareResult questShareResult)
        {
            var player = session.Player;
            if (player is Player actor && !ReferenceEquals(actor.Session, session)) return;
            player.QuestManager.QuestShareResult(questShareResult.QuestId, questShareResult.Result);
        }
    }
}
