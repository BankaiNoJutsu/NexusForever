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
        private const string DailyLoginRewardTableName = "DailyLoginReward.tbl";

        private readonly IAccount account;
        private readonly IGameTableManager gameTableManager;
        private uint loginDaysTotal;
        private uint rewardsAvailable;
        private uint lastClaimedLoginDay;
        private uint lastRewardItemKey;
        private uint premiumKeyStatus;
        private uint secondsUntilNextKey;
        private DateTime? lastClaimUtc;
        private DateTime? lastDayIncrementUtc;
        private bool dirty;

        public DailyLoginRewardManager(IAccount account, AccountModel model, IGameTableManager gameTableManager)
        {
            this.account          = account;
            this.gameTableManager = gameTableManager;
            AccountDailyLoginModel dailyLogin = model.AccountDailyLogin;
            if (dailyLogin == null)
                return;

            loginDaysTotal       = dailyLogin.LoginDaysTotal;
            rewardsAvailable     = dailyLogin.RewardsAvailable;
            lastClaimedLoginDay  = dailyLogin.LastClaimedLoginDay != 0u
                ? dailyLogin.LastClaimedLoginDay
                : InferLastClaimedLoginDay(dailyLogin.LoginDaysTotal, dailyLogin.RewardsAvailable);
            lastRewardItemKey    = dailyLogin.LastRewardItemKey;
            premiumKeyStatus     = dailyLogin.PremiumKeyStatus;
            secondsUntilNextKey  = dailyLogin.SecondsUntilNextKey;
            lastClaimUtc         = dailyLogin.LastClaimUtc;
            lastDayIncrementUtc  = dailyLogin.LastDayIncrementUtc;
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
                LastClaimedLoginDay = lastClaimedLoginDay,
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

            DailyLoginRewardEntry rewardEntry = GetNextClaimableReward();
            if (rewardEntry == null || rewardEntry.RewardObjectValue == 0u)
                return AccountOperationResult.GenericFail;

            uint accountItemId = rewardEntry.RewardObjectValue;
            if (!account.InventoryManager.CanAddItem(accountItemId))
                return AccountOperationResult.InvalidAccountItem;

            account.InventoryManager.AddItem(accountItemId);
            lastClaimedLoginDay = rewardEntry.LoginDay;
            rewardsAvailable    = GetClaimableRewardCount();
            lastRewardItemKey   = accountItemId;
            lastClaimUtc        = DateTime.UtcNow;
            dirty               = true;

            account.Session.EnqueueMessageEncrypted(BuildUpdatePacket());
            return AccountOperationResult.Ok;
        }

        public uint GetLoginDaysTotal()
        {
            RefreshDayCounters();
            return loginDaysTotal;
        }

        public uint GetRewardsAvailable()
        {
            RefreshDayCounters();
            return rewardsAvailable;
        }

        private void RefreshDayCounters()
        {
            DateTime now = DateTime.UtcNow;
            bool changed = false;
            if (!lastDayIncrementUtc.HasValue || (now - lastDayIncrementUtc.Value).TotalSeconds >= SecondsPerDay)
            {
                loginDaysTotal++;
                lastDayIncrementUtc = now;
                secondsUntilNextKey = SecondsPerDay;
                changed             = true;
            }

            uint highestEligibleRewardDay = GetHighestEligibleRewardDay();
            if (lastClaimedLoginDay > highestEligibleRewardDay)
            {
                lastClaimedLoginDay = highestEligibleRewardDay;
                changed             = true;
            }

            uint claimableRewardCount = GetClaimableRewardCount();
            if (rewardsAvailable != claimableRewardCount)
            {
                rewardsAvailable = claimableRewardCount;
                changed          = true;
            }

            if (changed)
                dirty = true;
        }

        private ServerDailyLoginUpdate BuildUpdatePacket()
        {
            return new ServerDailyLoginUpdate
            {
                Value0     = loginDaysTotal,
                Value1     = rewardsAvailable,
                Value2     = lastClaimedLoginDay,
                Value3     = 0u,
                Value4     = secondsUntilNextKey,
                FloatValue = NextRewardDayDuration,
                UInt3Value = premiumKeyStatus
            };
        }

        private uint GetClaimableRewardCount()
        {
            return (uint)GetClaimableRewards().Count();
        }

        private uint GetHighestEligibleRewardDay()
        {
            return GetRewardEntries()
                .Where(e => e.LoginDay <= loginDaysTotal)
                .Select(e => e.LoginDay)
                .DefaultIfEmpty()
                .Max();
        }

        private DailyLoginRewardEntry GetNextClaimableReward()
        {
            return GetClaimableRewards()
                .OrderBy(e => e.LoginDay)
                .ThenBy(e => e.Id)
                .FirstOrDefault();
        }

        private IEnumerable<DailyLoginRewardEntry> GetClaimableRewards()
        {
            return GetRewardEntries()
                .Where(e => e.RewardObjectValue != 0u)
                .Where(e => e.LoginDay > lastClaimedLoginDay && e.LoginDay <= loginDaysTotal);
        }

        private IEnumerable<DailyLoginRewardEntry> GetRewardEntries()
        {
            if (gameTableManager.DailyLoginReward?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    DailyLoginRewardTableName,
                    nameof(DailyLoginRewardManager) + "." + nameof(GetRewardEntries),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve daily login rewards.");

            return gameTableManager.DailyLoginReward?.Entries ?? [];
        }

        private static uint InferLastClaimedLoginDay(uint loginDaysTotal, uint rewardsAvailable)
        {
            return rewardsAvailable >= loginDaysTotal ? 0u : loginDaysTotal - rewardsAvailable;
        }
    }
}
