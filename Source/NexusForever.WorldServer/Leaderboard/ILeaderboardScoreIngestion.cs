using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public interface ILeaderboardScoreIngestion
    {
        void RecordPveCompletion(
            IPlayer player,
            LeaderboardType type,
            uint matchingGameMapId,
            uint primeLevel,
            uint completionTime,
            uint rewardedTier,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers = null);

        void RecordPvpRating(
            IPlayer player,
            LeaderboardType type,
            uint rating,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers = null);
    }
}
