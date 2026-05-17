using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientPtrCopyHandler : IMessageHandler<IWorldSession, ClientPtrCopy>
    {
        private readonly ILogger<ClientPtrCopyHandler> log;

        public ClientPtrCopyHandler(ILogger<ClientPtrCopyHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPtrCopy _)
        {
            log.LogDebug("ClientPtrCopy: player={Player}", session.Player?.Guid);
        }
    }
}
