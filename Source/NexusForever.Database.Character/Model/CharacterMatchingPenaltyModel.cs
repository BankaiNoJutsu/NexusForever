using System;

namespace NexusForever.Database.Character.Model
{
    public class CharacterMatchingPenaltyModel
    {
        public ulong Id { get; set; }
        public bool IsPvp { get; set; }
        public byte MatchType { get; set; }
        public uint Spell4Id { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
