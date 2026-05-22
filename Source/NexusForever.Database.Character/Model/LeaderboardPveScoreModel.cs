using System;

namespace NexusForever.Database.Character.Model
{
    public class LeaderboardPveScoreModel
    {
        public ulong Id { get; set; }
        public ulong CharacterId { get; set; }
        public ushort RealmId { get; set; }
        public byte Type { get; set; }
        public uint MatchingGameMapId { get; set; }
        public uint PrimeLevel { get; set; }
        public uint CompletionTime { get; set; }
        public uint RewardedTier { get; set; }
        public string PlayerName { get; set; }
        public byte PlayerClass { get; set; }
        public ulong GuildId { get; set; }
        public string TeamMembersJson { get; set; }
        public DateTime RecordedUtc { get; set; }
    }
}
