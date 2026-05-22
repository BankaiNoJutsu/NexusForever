using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountRewardRotationGrantModel
    {
        public uint AccountId { get; set; }
        public uint RewardRotationIndex { get; set; }
        public uint ContentId { get; set; }
        public uint RewardKeyId { get; set; }
        public byte RewardType { get; set; }
        public uint GrantFlags { get; set; }
        public DateTime GrantedUtc { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
