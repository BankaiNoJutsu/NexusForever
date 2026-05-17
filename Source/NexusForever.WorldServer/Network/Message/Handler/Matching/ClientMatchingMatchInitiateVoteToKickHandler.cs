using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateVoteToKickHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateVoteToKick>
    {
        private readonly ILogger<ClientMatchingMatchInitiateVoteToKickHandler> log;

        public ClientMatchingMatchInitiateVoteToKickHandler(ILogger<ClientMatchingMatchInitiateVoteToKickHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateVoteToKick initiateVoteToKick)
        {
            log.LogDebug("ClientMatchingMatchInitiateVoteToKick: player={Player}", session.Player?.Guid);
        }
    }
}
