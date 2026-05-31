using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public sealed class DailyLoginRewardManager
    {
        private const uint SecondsPerDay = 86400u;
        private const float NextRewardDayDuration = 1f;

        private readonly IAccount account;
        private uint loginDaysTotal;
        private uint rewardsAvailable;
        private uint lastRewardItemKey;
        private uint premiumKeyStatus;
        private uint secondsUntilNextKey;
        private DateTime? lastClaimUtc;
        private DateTime? lastDayIncrementUtc;
        private bool dirty;

        public DailyLoginRewardManager(IAccount account, AccountModel model)
        {
            this.account = account;
            AccountDailyLoginModel dailyLogin = model.AccountDailyLogin;
            if (dailyLogin == null)
                return;

            loginDaysTotal      = dailyLogin.LoginDaysTotal;
            rewardsAvailable    = dailyLogin.RewardsAvailable;
            lastRewardItemKey   = dailyLogin.LastRewardItemKey;
            premiumKeyStatus    = dailyLogin.PremiumKeyStatus;
            secondsUntilNextKey = dailyLogin.SecondsUntilNextKey;
            lastClaimUtc        = dailyLogin.LastClaimUtc;
            lastDayIncrementUtc = dailyLogin.LastDayIncrementUtc;
        }

        public void Save(AuthContext context)
        {
            if (!dirty)
                return;

            AccountDailyLoginModel existing = context.AccountDailyLogin.SingleOrDefault(d => d.Id == account.Id);
            if (existing != null)
                context.AccountDailyLogin.Remove(existing);

            context.AccountDailyLogin.Add(new AccountDailyLoginModel
            {
                Id                  = account.Id,
                LoginDaysTotal      = loginDaysTotal,
                RewardsAvailable    = rewardsAvailable,
                LastRewardItemKey   = lastRewardItemKey,
                PremiumKeyStatus    = premiumKeyStatus,
                SecondsUntilNextKey = secondsUntilNextKey,
                LastClaimUtc        = lastClaimUtc,
                LastDayIncrementUtc = lastDayIncrementUtc
            });

            dirty = false;
        }

        public void SendDailyLoginUpdate()
        {
            RefreshDayCounters();
            account.Session.EnqueueMessageEncrypted(BuildUpdatePacket());
        }

        public AccountOperationResult TryClaimReward()
        {
            RefreshDayCounters();

            if (rewardsAvailable == 0u)
                return AccountOperationResult.AlreadyClaimed;

            uint rewardLoginDay = GetNextClaimLoginDay();
            DailyLoginRewardEntry rewardEntry = GetRewardForDay(rewardLoginDay);
            if (rewardEntry == null || rewardEntry.RewardObjectValue == 0u)
                return AccountOperationResult.GenericFail;

            uint accountItemId = rewardEntry.RewardObjectValue;
            if (!account.InventoryManager.CanAddItem(accountItemId))
                return AccountOperationResult.InvalidAccountItem;

            account.InventoryManager.AddItem(accountItemId);
            rewardsAvailable--;
            lastRewardItemKey = accountItemId;
            lastClaimUtc      = DateTime.UtcNow;
            dirty             = true;

            account.Session.EnqueueMessageEncrypted(BuildUpdatePacket());
            return AccountOperationResult.Ok;
        }

        private void RefreshDayCounters()
        {
            DateTime now = DateTime.UtcNow;
            if (lastDayIncrementUtc.HasValue && (now - lastDayIncrementUtc.Value).TotalSeconds < SecondsPerDay)
                return;

            loginDaysTotal++;
            rewardsAvailable = Math.Min(rewardsAvailable + 1u, loginDaysTotal);
            lastDayIncrementUtc = now;
            secondsUntilNextKey = SecondsPerDay;
            dirty               = true;
        }

        private ServerDailyLoginUpdate BuildUpdatePacket()
        {
            return new ServerDailyLoginUpdate
            {
                Value0     = loginDaysTotal,
                Value1     = rewardsAvailable,
                Value2     = GetLastClaimedLoginDay(),
                Value3     = 0u,
                Value4     = secondsUntilNextKey,
                FloatValue = NextRewardDayDuration,
                UInt3Value = premiumKeyStatus
            };
        }

        private uint GetNextClaimLoginDay()
        {
            return Math.Min(GetLastClaimedLoginDay() + 1u, loginDaysTotal);
        }

        private uint GetLastClaimedLoginDay()
        {
            return rewardsAvailable >= loginDaysTotal ? 0u : loginDaysTotal - rewardsAvailable;
        }

        private static DailyLoginRewardEntry GetRewardForDay(uint loginDay)
        {
            return GameTableManager.Instance.DailyLoginReward.Entries
                .Where(e => e.LoginDay <= loginDay)
                .OrderByDescending(e => e.LoginDay)
                .FirstOrDefault();
        }
    }
}
