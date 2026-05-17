using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateVoteToSurrenderHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateVoteToSurrender>
    {
        private readonly ILogger<ClientMatchingMatchInitiateVoteToSurrenderHandler> log;

        public ClientMatchingMatchInitiateVoteToSurrenderHandler(ILogger<ClientMatchingMatchInitiateVoteToSurrenderHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateVoteToSurrender initiateVoteToSurrender)
        {
            log.LogDebug("ClientMatchingMatchInitiateVoteToSurrender: player={Player}", session.Player?.Guid);
        }
    }
}
