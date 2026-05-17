using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastPositionHandler : IMessageHandler<IWorldSession, ClientActivateUnitCastPosition>
    {
        private readonly ILogger<ClientActivateUnitCastPositionHandler> log;
        private readonly ClientActivateUnitCastHandler activateUnitCastHandler;

        public ClientActivateUnitCastPositionHandler(
            ILogger<ClientActivateUnitCastPositionHandler> log,
            ClientActivateUnitCastHandler activateUnitCastHandler)
        {
            this.log                     = log;
            this.activateUnitCastHandler = activateUnitCastHandler;
        }

        public void HandleMessage(IWorldSession session, ClientActivateUnitCastPosition activateUnitCastPosition)
        {
            log.LogTrace("Handling activate-unit position cast from player {PlayerGuid}: target {TargetEntityId}, selectors {SelectorA}/{SelectorB}, position {Position}.",
                session.Player?.Guid,
                activateUnitCastPosition.TargetEntityId,
                activateUnitCastPosition.SelectorA,
                activateUnitCastPosition.SelectorB,
                activateUnitCastPosition.Position);

            activateUnitCastHandler.HandleMessageInternal(
                session,
                activateUnitCastPosition.TargetEntityId,
                activateUnitCastPosition.ContextToken,
                nameof(ClientActivateUnitCastPosition));
        }
    }
}
