using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public interface ILeaderboardProvider
    {
        ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request);
        ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request);
    }
}
