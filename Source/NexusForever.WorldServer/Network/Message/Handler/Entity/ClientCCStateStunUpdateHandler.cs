using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientCCStateStunUpdateHandler : IMessageHandler<IWorldSession, ClientCCStateStunUpdate>
    {
        private readonly ILogger<ClientCCStateStunUpdateHandler> log;

        public ClientCCStateStunUpdateHandler(ILogger<ClientCCStateStunUpdateHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client reports directional inputs pressed/held while the player is stunned.
        /// The server uses its own authoritative CC state; this is diagnostic only.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCCStateStunUpdate ccStateUpdate)
        {
            log.LogDebug("ClientCCStateStunUpdate: player={Player}, pressed={Pressed}, held={Held}",
                session.Player?.Guid, ccStateUpdate.InputPressed, ccStateUpdate.InputHeld);
        }
    }
}
