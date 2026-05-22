using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;
using NexusForever.WorldServer.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPveRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPveRequest>
    {
        private readonly ILogger<ClientLeaderboardPveRequestHandler> log;
        private readonly ILeaderboardProvider leaderboardProvider;

        public ClientLeaderboardPveRequestHandler(
            ILogger<ClientLeaderboardPveRequestHandler> log,
            ILeaderboardProvider leaderboardProvider)
        {
            this.log                 = log;
            this.leaderboardProvider = leaderboardProvider;
        }

        public void HandleMessage(IWorldSession session, ClientLeaderboardPveRequest request)
        {
            log.LogDebug("Returning PvE leaderboard for player {PlayerGuid}: type {Type}, matching game map {MatchingGameMapId}, prime level {PrimeLevel}.",
                session.Player?.Guid, request.Type, request.MatchingGameMapdId, request.PrimeLevel);

            session.EnqueueMessageEncrypted(leaderboardProvider.BuildPve(request, session.Player));
        }
    }
}
