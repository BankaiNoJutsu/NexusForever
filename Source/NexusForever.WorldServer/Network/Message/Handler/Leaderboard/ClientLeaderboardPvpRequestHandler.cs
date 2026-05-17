using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Network.Message.Handler.Leaderboard
{
    public class ClientLeaderboardPvpRequestHandler : IMessageHandler<IWorldSession, ClientLeaderboardPvpRequest>
    {
        public void HandleMessage(IWorldSession session, ClientLeaderboardPvpRequest message)
        {
            // TODO: implement PvP leaderboard request
        }
    }
}
