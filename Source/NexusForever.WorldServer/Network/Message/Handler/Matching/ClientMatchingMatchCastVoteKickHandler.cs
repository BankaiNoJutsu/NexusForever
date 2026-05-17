using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchCastVoteKickHandler : IMessageHandler<IWorldSession, ClientMatchingMatchCastVoteKick>
    {
        private readonly ILogger<ClientMatchingMatchCastVoteKickHandler> log;

        public ClientMatchingMatchCastVoteKickHandler(ILogger<ClientMatchingMatchCastVoteKickHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchCastVoteKick castVoteKick)
        {
            log.LogDebug("ClientMatchingMatchCastVoteKick: player={Player}", session.Player?.Guid);
        }
    }
}
