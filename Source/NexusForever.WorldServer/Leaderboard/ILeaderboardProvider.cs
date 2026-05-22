using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public interface ILeaderboardProvider
    {
        ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request, IPlayer viewer = null);
        ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request, IPlayer viewer = null);
    }
}
