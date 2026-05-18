using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.GalacticArchive;

namespace NexusForever.WorldServer.Network.Message.Handler.GalacticArchive
{
    public class ClientGalacticArchiveViewedHandler : IMessageHandler<IWorldSession, ClientGalacticArchiveViewed>
    {
        private readonly ILogger<ClientGalacticArchiveViewedHandler> log;

        public ClientGalacticArchiveViewedHandler(ILogger<ClientGalacticArchiveViewedHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGalacticArchiveViewed archiveViewed)
        {
            bool viewed = session.Player?.GalacticArchiveManager.MarkArticleViewed(archiveViewed.ArchiveArticleId) ?? false;
            log.LogDebug("ClientGalacticArchiveViewed: player={Player} articleId={ArticleId}",
                session.Player?.Guid, archiveViewed.ArchiveArticleId);

            if (!viewed)
                log.LogDebug("Rejected Galactic Archive viewed request from player {Player}: invalid or locked article {ArticleId}.",
                    session.Player?.Guid, archiveViewed.ArchiveArticleId);
        }
    }
}
