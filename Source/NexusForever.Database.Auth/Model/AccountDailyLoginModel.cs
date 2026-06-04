using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountDailyLoginModel
    {
        public uint Id { get; set; }
        public uint LoginDaysTotal { get; set; }
        public uint RewardsAvailable { get; set; }
        public uint LastClaimedLoginDay { get; set; }
        public uint LastRewardItemKey { get; set; }
        public uint PremiumKeyStatus { get; set; }
        public uint SecondsUntilNextKey { get; set; }
        public DateTime? LastClaimUtc { get; set; }
        public DateTime? LastDayIncrementUtc { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
