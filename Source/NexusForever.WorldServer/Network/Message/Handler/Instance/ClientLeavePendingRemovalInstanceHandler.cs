using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientLeavePendingRemovalInstanceHandler : IMessageHandler<IWorldSession, ClientLeavePendingRemovalInstance>
    {
        private readonly ILogger<ClientLeavePendingRemovalInstanceHandler> log;

        public ClientLeavePendingRemovalInstanceHandler(ILogger<ClientLeavePendingRemovalInstanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLeavePendingRemovalInstance _)
        {
            log.LogDebug("ClientLeavePendingRemovalInstance: player={Player}", session.Player?.Guid);
        }
    }
}
