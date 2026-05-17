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
            session.Player.ResetAttributePoints();

            log.LogDebug("Reset attribute point allocations for player {PlayerGuid}: available {AvailableAttributePoints}.",
                session.Player?.Guid, session.Player?.GetAvailableAttributePoints());
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
            if (!session.Player.TrySpendAttributePoints(spendAttributePoints.AttributePoints, out uint availableAttributePoints))
            {
                log.LogWarning("Rejected attribute point spend request from player {PlayerGuid}: values [{AttributePoints}], available {AvailableAttributePoints}.",
                    session.Player?.Guid, string.Join(", ", spendAttributePoints.AttributePoints), availableAttributePoints);
                session.Player.SendAttributePoints();
                return;
            }

            log.LogDebug("Updated attribute point allocations for player {PlayerGuid}: values [{AttributePoints}], available {AvailableAttributePoints}.",
                session.Player?.Guid, string.Join(", ", spendAttributePoints.AttributePoints), availableAttributePoints);
        }
    }
}
