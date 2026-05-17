using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Character
{
    public class ClientResetAttributePointsHandler : IMessageHandler<IWorldSession, ClientResetAttributePoints>
    {
        private readonly ILogger<ClientResetAttributePointsHandler> log;

        public ClientResetAttributePointsHandler(ILogger<ClientResetAttributePointsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientResetAttributePoints _)
        {
            log.LogDebug("Ignoring unsupported attribute point reset request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientSpendAttributePointsHandler : IMessageHandler<IWorldSession, ClientSpendAttributePoints>
    {
        private readonly ILogger<ClientSpendAttributePointsHandler> log;

        public ClientSpendAttributePointsHandler(ILogger<ClientSpendAttributePointsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSpendAttributePoints spendAttributePoints)
        {
            log.LogDebug("Ignoring unsupported attribute point spend request from player {PlayerGuid}: values [{AttributePoints}].",
                session.Player?.Guid, string.Join(", ", spendAttributePoints.AttributePoints));
        }
    }
}
