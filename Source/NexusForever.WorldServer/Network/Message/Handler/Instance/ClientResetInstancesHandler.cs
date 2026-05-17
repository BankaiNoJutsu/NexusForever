using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientResetInstancesHandler : IMessageHandler<IWorldSession, ClientResetInstances>
    {
        private readonly ILogger<ClientResetInstancesHandler> log;

        public ClientResetInstancesHandler(ILogger<ClientResetInstancesHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientResetInstances _)
        {
            log.LogDebug("Rejecting all-instance reset from player {PlayerGuid}.", session.Player?.Guid);

            session.EnqueueMessageEncrypted(new ServerInstanceResetResult
            {
                Success = false
            });
        }
    }
}
