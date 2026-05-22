using System.Collections.Generic;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class LeaderboardPveEntryRecord
    {
        public ulong CharacterId { get; init; }
        public string Name { get; init; }
        public Class Class { get; init; }
        public ulong GuildId { get; init; }
        public LeaderboardType Type { get; init; }
        public uint MatchingGameMapId { get; init; }
        public uint PrimeLevel { get; init; }
        public uint RewardedTier { get; init; }
        public uint CompletionTime { get; init; }
        public uint LastRank { get; init; }
        public IReadOnlyList<LeaderboardTeamMemberRecord> TeamMembers { get; init; } = [];
    }

    public sealed class LeaderboardPvpEntryRecord
    {
        public ulong CharacterId { get; init; }
        public string Name { get; init; }
        public Class Class { get; init; }
        public ulong GuildId { get; init; }
        public LeaderboardType Type { get; init; }
        public uint Rating { get; init; }
        public uint LastRank { get; init; }
        public IReadOnlyList<LeaderboardTeamMemberRecord> TeamMembers { get; init; } = [];
    }

    public sealed class LeaderboardTeamMemberRecord
    {
        public string Name { get; init; }
        public Class Class { get; init; }
    }
}
