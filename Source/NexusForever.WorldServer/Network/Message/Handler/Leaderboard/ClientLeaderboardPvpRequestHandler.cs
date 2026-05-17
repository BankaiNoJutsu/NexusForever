using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPvpRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPvpRequest>
    {
        private readonly ILogger<ClientLeaderboardPvpRequestHandler> log;

        public ClientLeaderboardPvpRequestHandler(ILogger<ClientLeaderboardPvpRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLeaderboardPvpRequest request)
        {
            log.LogDebug("Returning empty PvP leaderboard for player {PlayerGuid}: type {Type}.",
                session.Player?.Guid, request.Type);

            session.EnqueueMessageEncrypted(new ServerLeaderboardPvp
            {
                Type = request.Type
            });
        }
    }
}
