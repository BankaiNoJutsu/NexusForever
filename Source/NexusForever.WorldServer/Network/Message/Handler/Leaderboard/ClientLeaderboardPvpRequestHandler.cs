using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;
using NexusForever.WorldServer.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPvpRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPvpRequest>
    {
        private readonly ILogger<ClientLeaderboardPvpRequestHandler> log;
        private readonly ILeaderboardProvider leaderboardProvider;

        public ClientLeaderboardPvpRequestHandler(
            ILogger<ClientLeaderboardPvpRequestHandler> log,
            ILeaderboardProvider leaderboardProvider)
        {
            this.log                 = log;
            this.leaderboardProvider = leaderboardProvider;
        }

        public void HandleMessage(IWorldSession session, ClientLeaderboardPvpRequest request)
        {
            log.LogDebug("Returning PvP leaderboard for player {PlayerGuid}: type {Type}.",
                session.Player?.Guid, request.Type);

            session.EnqueueMessageEncrypted(leaderboardProvider.BuildPvp(request, session.Player));
        }
    }
}
