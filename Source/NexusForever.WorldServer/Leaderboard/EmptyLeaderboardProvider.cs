using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class EmptyLeaderboardProvider : ILeaderboardProvider
    {
        public ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request, IPlayer viewer = null)
        {
            return new ServerLeaderboardPve
            {
                Type              = request.Type,
                MatchingGameMapId = request.MatchingGameMapdId,
                PrimeLevel        = request.PrimeLevel
            };
        }

        public ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request, IPlayer viewer = null)
        {
            return new ServerLeaderboardPvp
            {
                Type = request.Type
            };
        }
    }
}
