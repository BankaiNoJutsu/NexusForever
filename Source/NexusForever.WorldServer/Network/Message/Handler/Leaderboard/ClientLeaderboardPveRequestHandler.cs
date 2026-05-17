using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPveRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPveRequest>
    {
        public void HandleMessage(IWorldSession session, ClientLeaderboardPveRequest message)
        {
            // TODO: implement PvE leaderboard request
        }
    }
}
