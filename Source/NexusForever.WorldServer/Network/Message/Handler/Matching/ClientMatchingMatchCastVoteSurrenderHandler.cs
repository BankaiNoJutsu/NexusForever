using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchCastVoteSurrenderHandler : IMessageHandler<IWorldSession, ClientMatchingMatchCastVoteSurrender>
    {
        private readonly ILogger<ClientMatchingMatchCastVoteSurrenderHandler> log;

        public ClientMatchingMatchCastVoteSurrenderHandler(ILogger<ClientMatchingMatchCastVoteSurrenderHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchCastVoteSurrender castVoteSurrender)
        {
            log.LogDebug("ClientMatchingMatchCastVoteSurrender: player={Player}", session.Player?.Guid);
        }
    }
}
