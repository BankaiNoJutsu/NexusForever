using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientGetRealmTransferDestinationsHandler : IMessageHandler<IWorldSession, ClientGetRealmTransferDestinations>
    {
        private readonly ILogger<ClientGetRealmTransferDestinationsHandler> log;

        public ClientGetRealmTransferDestinationsHandler(ILogger<ClientGetRealmTransferDestinationsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGetRealmTransferDestinations _)
        {
            log.LogDebug("ClientGetRealmTransferDestinations: player={Player}", session.Player?.Guid);
            session.EnqueueMessageEncrypted(new ServerTransferDestinationRealmList());
        }
    }
}
