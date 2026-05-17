using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingQueueRandomPartyHandler : IMessageHandler<IWorldSession, ClientMatchingQueueRandomParty>
    {
        private readonly ILogger<ClientMatchingQueueRandomPartyHandler> log;

        public ClientMatchingQueueRandomPartyHandler(ILogger<ClientMatchingQueueRandomPartyHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingQueueRandomParty queueRandomParty)
        {
            log.LogDebug("ClientMatchingQueueRandomParty: player={Player} matchType={MatchType}",
                session.Player?.Guid, queueRandomParty.MatchType);
        }
    }
}
