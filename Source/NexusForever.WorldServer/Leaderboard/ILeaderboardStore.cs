using System.Collections.Generic;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public interface ILeaderboardStore
    {
        IReadOnlyList<LeaderboardPveEntryRecord> GetPveEntries(LeaderboardType type, uint matchingGameMapId, uint primeLevel);
        IReadOnlyList<LeaderboardPvpEntryRecord> GetPvpEntries(LeaderboardType type);
    }
}
