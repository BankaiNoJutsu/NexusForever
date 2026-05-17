using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.GalacticArchive;

namespace NexusForever.WorldServer.Network.Message.Handler.GalacticArchive
{
    public class ClientGalacticArchiveUnlockHandler : IMessageHandler<IWorldSession, ClientGalacticArchiveUnlock>
    {
        private readonly ILogger<ClientGalacticArchiveUnlockHandler> log;

        public ClientGalacticArchiveUnlockHandler(ILogger<ClientGalacticArchiveUnlockHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGalacticArchiveUnlock archiveUnlock)
        {
            log.LogDebug("ClientGalacticArchiveUnlock: player={Player} articleId={ArticleId}",
                session.Player?.Guid, archiveUnlock.ArchiveArticleId);
        }
    }
}
