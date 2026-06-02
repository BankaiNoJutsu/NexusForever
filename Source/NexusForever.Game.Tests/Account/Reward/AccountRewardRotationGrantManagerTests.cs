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

public class AccountRewardRotationGrantManagerTests
{
    [Fact]
    public void BuildEntryState_ReturnsOnlyPersistedGrantsForRequestedIndex()
    {
        const uint accountId = 42u;
        IAccount account = new StubAccount(accountId);
        var model = new AccountModel
        {
            Id = accountId,
            AccountRewardRotationGrant =
            [
                new AccountRewardRotationGrantModel
                {
                    AccountId = accountId,
                    RewardRotationIndex = 2u,
                    ContentId = 10u,
                    RewardKeyId = 99u,
                    RewardType = RewardRotationScheduleBuilder.RewardTypeEssence,
                    GrantFlags = RewardRotationEntryStateBuilder.GrantFlagEssence,
                    GrantedUtc = DateTime.UtcNow
                },
                new AccountRewardRotationGrantModel
                {
                    AccountId = accountId,
                    RewardRotationIndex = 4u,
                    ContentId = 20u,
                    RewardKeyId = 1u,
                    RewardType = RewardRotationScheduleBuilder.RewardTypeItem,
                    GrantFlags = RewardRotationEntryStateBuilder.GrantFlagItemOrModifier,
                    GrantedUtc = DateTime.UtcNow
                }
            ]
        };

        var manager = new AccountRewardRotationGrantManager(account, model);

        ServerRewardRotationEntryStateArray entryState = manager.BuildEntryState(2u);

        Assert.Single(entryState.Entries);
        Assert.Equal(10u, entryState.Entries[0].ContentId);
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagEssence, entryState.Entries[0].Value);
    }

    [Fact]
    public void RecordGrant_UpdatesEntryStateForMatchingIndex()
    {
        const uint accountId = 42u;
        IAccount account = new StubAccount(accountId);
        var manager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = accountId });

        manager.RecordGrant(1u, 12u, 3u, RewardRotationScheduleBuilder.RewardTypeItem, RewardRotationEntryStateBuilder.GrantFlagItemOrModifier);

        ServerRewardRotationEntryStateArray entryState = manager.BuildEntryState(1u);

        Assert.Single(entryState.Entries);
        Assert.Equal(12u, entryState.Entries[0].ContentId);
        Assert.Equal(3u, entryState.Entries[0].RewardTypeId);
    }

    private sealed class StubAccount(uint id) : IAccount
    {
        public uint Id => id;
        public string Email => "reward-grant@test.invalid";
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
