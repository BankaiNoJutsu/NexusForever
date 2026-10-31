using System.Collections.Generic;

namespace NexusForever.Database.Auth.Model
{
    public sealed class AccountLoyaltyModel
    {
        public uint AccountId { get; set; }
        public ulong EarnedPoints { get; set; }
        public ulong BootstrapWalletAmount { get; set; }
        public uint ActiveTrackId { get; set; }
        public ulong Revision { get; set; }
        public AccountModel Account { get; set; }
    }

    public sealed class AccountLoyaltySlotModel
    {
        public uint AccountId { get; set; }
        public uint TrackId { get; set; }
        public byte Position { get; set; }
        public uint RewardRowId { get; set; }
        public bool Claimed { get; set; }
        public uint Choice { get; set; }
        public string DeliveryKey { get; set; }
        public AccountModel Account { get; set; }
    }

    public sealed class AccountCosmicOperationModel
    {
        public uint AccountId { get; set; }
        public string OperationKey { get; set; }
        public ulong Amount { get; set; }
        public bool Subtract { get; set; }
        public bool Earned { get; set; }
        public AccountModel Account { get; set; }
    }
}
