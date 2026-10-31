namespace NexusForever.Database.Character.Model
{
    public class CharacterContractRewardTrackModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public uint Points { get; set; }
        public ulong Revision { get; set; }
        public bool Active { get; set; }
        public bool SelectionInitialized { get; set; }
        public CharacterModel Character { get; set; }
    }

    public class CharacterContractCompletionModel
    {
        public ulong Id { get; set; }
        public string AcceptanceId { get; set; }
        public ushort QuestId { get; set; }
        public uint TrackId { get; set; }
        public uint BasePoints { get; set; }
        public uint Points { get; set; }
        public uint PayloadVersion { get; set; }
        public string PayloadJson { get; set; }
        public System.DateTime CreatedUtc { get; set; }
        public CharacterModel Character { get; set; }
    }

    public class CharacterContractRewardSlotModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public byte Ordinal { get; set; }
        public uint RewardRowId { get; set; }
        public uint RequiredPoints { get; set; }
        public string SelectionJson { get; set; }
        public bool Claimed { get; set; }
        public CharacterContractRewardTrackModel Track { get; set; }
    }

    public class CharacterContractRewardClaimModel
    {
        public ulong Id { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public byte Ordinal { get; set; }
        public uint RewardRowId { get; set; }
        public uint Choice { get; set; }
        public string ClaimId { get; set; }
        public uint AccountId { get; set; }
        public uint QuoteVersion { get; set; }
        public string QuoteJson { get; set; }
        public uint DeliveryVersion { get; set; }
        public string DeliveryJson { get; set; }
        public string CharacterCurrencyJson { get; set; }
        public System.DateTime CreatedUtc { get; set; }
        public CharacterModel Character { get; set; }
    }

    // No Character FK: a committed account compensation intent survives hard
    // deletion of its source Character until the original account is credited.
    public class CharacterContractAccountOutboxModel
    {
        public string ClaimId { get; set; }
        public ulong CharacterId { get; set; }
        public uint AccountId { get; set; }
        public ushort RealmId { get; set; }
        public uint TrackId { get; set; }
        public ulong CycleId { get; set; }
        public byte Ordinal { get; set; }
        public string PayloadJson { get; set; }
        public bool Delivered { get; set; }
    }
}
