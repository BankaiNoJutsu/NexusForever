using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Info;

namespace NexusForever.WorldServer.Network.Message.Handler.Info
{
    public class ClientRealmInfoRequestHandler : IMessageHandler<IWorldSession, ClientRealmInfoRequest>
    {
        private readonly ILogger<ClientRealmInfoRequestHandler> log;

        public ClientRealmInfoRequestHandler(ILogger<ClientRealmInfoRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRealmInfoRequest realmInfoRequest)
        {
            log.LogDebug("ClientRealmInfoRequest: player={Player} realmId={RealmId}",
                session.Player?.Guid, realmInfoRequest.RealmId);
        }
    }
}
