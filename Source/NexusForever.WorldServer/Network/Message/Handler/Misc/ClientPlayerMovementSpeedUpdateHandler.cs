using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientPlayerMovementSpeedUpdateHandler : IMessageHandler<IWorldSession, ClientPlayerMovementSpeedUpdate>
    {
        private readonly ILogger<ClientPlayerMovementSpeedUpdateHandler> log;

        public ClientPlayerMovementSpeedUpdateHandler(ILogger<ClientPlayerMovementSpeedUpdateHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPlayerMovementSpeedUpdate speedUpdate)
        {
            log.LogDebug("ClientPlayerMovementSpeedUpdate: player={Player} value={Value}",
                session.Player?.Guid, speedUpdate.Value);
        }
    }
}
