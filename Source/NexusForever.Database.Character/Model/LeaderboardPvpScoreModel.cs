using System;

namespace NexusForever.Database.Character.Model
{
    public class LeaderboardPvpScoreModel
    {
        public ulong Id { get; set; }
        public ulong CharacterId { get; set; }
        public ushort RealmId { get; set; }
        public byte Type { get; set; }
        public uint Rating { get; set; }
        public string PlayerName { get; set; }
        public byte PlayerClass { get; set; }
        public ulong GuildId { get; set; }
        public string TeamMembersJson { get; set; }
        public DateTime RecordedUtc { get; set; }
    }
}
