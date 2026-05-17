using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPveRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPveRequest>
    {
        private readonly ILogger<ClientLeaderboardPveRequestHandler> log;

        public ClientLeaderboardPveRequestHandler(ILogger<ClientLeaderboardPveRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLeaderboardPveRequest request)
        {
            log.LogDebug("Returning empty PvE leaderboard for player {PlayerGuid}: type {Type}, matching game map {MatchingGameMapId}, prime level {PrimeLevel}.",
                session.Player?.Guid, request.Type, request.MatchingGameMapdId, request.PrimeLevel);

            session.EnqueueMessageEncrypted(new ServerLeaderboardPve
            {
                Type              = request.Type,
                MatchingGameMapId = request.MatchingGameMapdId,
                PrimeLevel        = request.PrimeLevel
            });
        }
    }
}
