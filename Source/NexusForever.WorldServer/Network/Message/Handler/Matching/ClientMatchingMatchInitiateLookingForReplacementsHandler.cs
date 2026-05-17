using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingMatchInitiateLookingForReplacementsHandler : IMessageHandler<IWorldSession, ClientMatchingMatchInitiateLookingForReplacements>
    {
        private readonly ILogger<ClientMatchingMatchInitiateLookingForReplacementsHandler> log;

        public ClientMatchingMatchInitiateLookingForReplacementsHandler(ILogger<ClientMatchingMatchInitiateLookingForReplacementsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingMatchInitiateLookingForReplacements initiateLookingForReplacements)
        {
            log.LogDebug("ClientMatchingMatchInitiateLookingForReplacements: player={Player}", session.Player?.Guid);
        }
    }
}
