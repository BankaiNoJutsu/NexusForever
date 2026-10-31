using System;

namespace NexusForever.Database.Character.Model
{
    public sealed class CharacterChallengeAttemptModel
    {
        public ulong Id { get; set; }
        public string AttemptId { get; set; }
        public ushort ChallengeId { get; set; }
        public uint TrackId { get; set; }
        public byte RewardMode { get; set; }
        public uint EarnedMedalMask { get; set; }
        public bool Terminal { get; set; }
        public CharacterModel Character { get; set; }
    }

    public sealed class CharacterChallengeRewardTrackModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public uint Points { get; set; }
        public ulong Revision { get; set; }
        public bool Active { get; set; }
        public CharacterModel Character { get; set; }
    }

    public sealed class CharacterChallengeRewardSlotModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public byte Ordinal { get; set; }
        public uint RewardRowId { get; set; }
        public uint RequiredPoints { get; set; }
        public bool Claimed { get; set; }
        public CharacterChallengeRewardTrackModel Track { get; set; }
    }

    public sealed class CharacterChallengeMedalReceiptModel
    {
        public ulong Id { get; set; }
        public string AttemptId { get; set; }
        public byte Medal { get; set; }
        public uint TrackId { get; set; }
        public uint Points { get; set; }
        public float Multiplier { get; set; }
        public long ModifierRevision { get; set; }
        public DateTime CreatedUtc { get; set; }
        public CharacterChallengeAttemptModel Attempt { get; set; }
    }

    public sealed class CharacterChallengeRewardClaimModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public byte Ordinal { get; set; }
        public uint RewardRowId { get; set; }
        public uint Choice { get; set; }
        public string ClaimId { get; set; }
        public uint DeliveryVersion { get; set; }
        public string DeliveryJson { get; set; }
        public DateTime CreatedUtc { get; set; }
        public CharacterChallengeRewardSlotModel Slot { get; set; }
    }
}
