using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientRaidInfoRequestHandler : IMessageHandler<IWorldSession, ClientRaidInfoRequest>
    {
        private readonly ILogger<ClientRaidInfoRequestHandler> log;

        public ClientRaidInfoRequestHandler(ILogger<ClientRaidInfoRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRaidInfoRequest _)
        {
            log.LogDebug("ClientRaidInfoRequest: player={Player}", session.Player?.Guid);
        }
    }
}
