using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationGrantClaimServiceTests
{
    [Fact]
    public void TryRecordClaimFromScheduleRow_RecordsMatchingScheduleRow()
    {
        const uint accountId = 42u;
        IAccount account = new StubAccount(accountId);
        var manager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = accountId });

        var schedule = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    ContentId = 10u,
                    RewardKeyId = 99u,
                    Duration = 2f,
                    RewardType = RewardRotationScheduleBuilder.RewardTypeEssence,
                    Value = 1u
                }
            }
        };

        bool recorded = RewardRotationGrantClaimService.TryRecordClaimFromScheduleRow(
            manager,
            2u,
            schedule,
            10u,
            RewardRotationScheduleBuilder.RewardTypeEssence,
            out ServerRewardRotationEntryStateArray.EntryStateRow row);

        Assert.True(recorded);
        Assert.NotNull(row);
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagEssence, row.Value);

        ServerRewardRotationEntryStateArray entryState = manager.BuildEntryState(2u);
        Assert.Single(entryState.Entries);
        Assert.Equal(99u, entryState.Entries[0].RewardTypeId);
    }

    [Fact]
    public void TryRecordClaimFromScheduleRow_RejectsUnknownScheduleRow()
    {
        const uint accountId = 42u;
        IAccount account = new StubAccount(accountId);
        var manager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = accountId });

        bool recorded = RewardRotationGrantClaimService.TryRecordClaimFromScheduleRow(
            manager,
            1u,
            new ServerRewardRotationScheduleArray(),
            10u,
            RewardRotationScheduleBuilder.RewardTypeItem,
            out ServerRewardRotationEntryStateArray.EntryStateRow row);

        Assert.False(recorded);
        Assert.Null(row);
    }

    private sealed class StubAccount(uint id) : IAccount
    {
        public uint Id => id;
        public string Email => "reward-grant-claim@test.invalid";
        public IAccountRBACManager RbacManager => null;
        public IGenericUnlockManager GenericUnlockManager => null;
        public IAccountCurrencyManager CurrencyManager => null;
        public IAccountEntitlementManager EntitlementManager => null;
        public IAccountInventoryManager InventoryManager => null;
        public IAccountCostumeManager CostumeManager => null;
        public IRewardPropertyManager RewardPropertyManager => null;
        public IAccountRewardRotationGrantManager RewardRotationGrantManager => null;
        public IAccountKeybindingManager KeybindingManager => null;
        public AccountTier AccountTier => default;
        public IGameSession Session => null;

        public uint GetCREDDPendingOrderState() => 0u;

        public void Initialise(AccountModel model, IGameSession session)
        {
        }

        public void Save(AuthContext context)
        {
        }
    }
}
