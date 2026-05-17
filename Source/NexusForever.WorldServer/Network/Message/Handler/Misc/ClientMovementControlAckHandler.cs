using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientMovementControlAckHandler : IMessageHandler<IWorldSession, ClientMovementControlAck>
    {
        private readonly ILogger<ClientMovementControlAckHandler> log;

        public ClientMovementControlAckHandler(ILogger<ClientMovementControlAckHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMovementControlAck ack)
        {
            log.LogDebug("ClientMovementControlAck: player={Player}, ticket={Ticket}.",
                session.Player?.Guid, ack.Ticket);
        }
    }
}
