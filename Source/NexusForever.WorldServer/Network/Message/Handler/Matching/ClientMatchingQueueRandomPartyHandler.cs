using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingQueueRandomPartyHandler : IMessageHandler<IWorldSession, ClientMatchingQueueRandomParty>
    {
        #region Dependency Injection

        private readonly IMatchingManager matchingManager;

        public ClientMatchingQueueRandomPartyHandler(
            IMatchingManager matchingManager)
        {
            this.matchingManager = matchingManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientMatchingQueueRandomParty queueRandomParty)
        {
            matchingManager.JoinRandomPartyQueue(session.Player, queueRandomParty.Roles, queueRandomParty.MatchType, queueRandomParty.Flags);
        }
    }
}
