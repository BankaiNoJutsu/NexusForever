using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientCastPathExplorerSearchingHandler : IMessageHandler<IWorldSession, ClientPathExplorerCastSearching>
    {
        private readonly ILogger<ClientCastPathExplorerSearchingHandler> log;

        public ClientCastPathExplorerSearchingHandler(ILogger<ClientCastPathExplorerSearchingHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathExplorerCastSearching castSearching)
        {
            log.LogDebug("ClientCastPathExplorerSearching: player={Player} contextToken={Token} band={Band} clueId={ClueId}",
                session.Player?.Guid, castSearching.ContextToken, castSearching.SearchRadiusBand, castSearching.PathExplorerScavengerClueId);
        }
    }
}
