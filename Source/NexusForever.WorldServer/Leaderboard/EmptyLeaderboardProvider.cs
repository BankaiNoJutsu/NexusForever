using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class EmptyLeaderboardProvider : ILeaderboardProvider
    {
        public ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request)
        {
            return new ServerLeaderboardPve
            {
                Type              = request.Type,
                MatchingGameMapId = request.MatchingGameMapdId,
                PrimeLevel        = request.PrimeLevel
            };
        }

        public ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request)
        {
            return new ServerLeaderboardPvp
            {
                Type = request.Type
            };
        }
    }
}
