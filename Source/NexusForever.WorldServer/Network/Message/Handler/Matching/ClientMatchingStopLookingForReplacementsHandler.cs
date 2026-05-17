using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingStopLookingForReplacementsHandler : IMessageHandler<IWorldSession, ClientMatchingStopLookingForReplacements>
    {
        private readonly ILogger<ClientMatchingStopLookingForReplacementsHandler> log;

        public ClientMatchingStopLookingForReplacementsHandler(ILogger<ClientMatchingStopLookingForReplacementsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingStopLookingForReplacements stopLookingForReplacements)
        {
            log.LogDebug("ClientMatchingStopLookingForReplacements: player={Player}", session.Player?.Guid);
        }
    }
}
