using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientDialogOpenedHandler : IMessageHandler<IWorldSession, ClientDialogOpened>
    {
        private readonly ILogger<ClientDialogOpenedHandler> log;

        public ClientDialogOpenedHandler(ILogger<ClientDialogOpenedHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client sends this zero-payload notification when an NPC dialog window opens.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientDialogOpened dialogOpened)
        {
            log.LogDebug("ClientDialogOpened: player={Player}", session.Player?.Guid);
        }
    }
}
