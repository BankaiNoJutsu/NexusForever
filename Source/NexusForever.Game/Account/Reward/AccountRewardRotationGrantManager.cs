using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    public sealed class AccountRewardRotationGrantManager : IAccountRewardRotationGrantManager
    {
        private readonly IAccount account;
        private readonly List<AccountRewardRotationGrantModel> grants = new();
        private bool dirty;

        public AccountRewardRotationGrantManager(IAccount account, AccountModel model)
        {
            this.account = account;
            if (model?.AccountRewardRotationGrant == null)
                return;

            foreach (AccountRewardRotationGrantModel grant in model.AccountRewardRotationGrant)
            {
                if (grant == null)
                    continue;

                grants.Add(new AccountRewardRotationGrantModel
                {
                    AccountId = grant.AccountId,
                    RewardRotationIndex = grant.RewardRotationIndex,
                    ContentId = grant.ContentId,
                    RewardKeyId = grant.RewardKeyId,
                    RewardType = grant.RewardType,
                    GrantFlags = grant.GrantFlags,
                    GrantedUtc = grant.GrantedUtc
                });
            }
        }

        public void Save(AuthContext context)
        {
            if (!dirty)
                return;

            List<AccountRewardRotationGrantModel> existing = context.AccountRewardRotationGrant
                .Where(grant => grant.AccountId == account.Id)
                .ToList();
            if (existing.Count > 0)
                context.AccountRewardRotationGrant.RemoveRange(existing);

            foreach (AccountRewardRotationGrantModel grant in grants)
            {
                context.AccountRewardRotationGrant.Add(new AccountRewardRotationGrantModel
                {
                    AccountId = account.Id,
                    RewardRotationIndex = grant.RewardRotationIndex,
                    ContentId = grant.ContentId,
                    RewardKeyId = grant.RewardKeyId,
                    RewardType = grant.RewardType,
                    GrantFlags = grant.GrantFlags,
                    GrantedUtc = grant.GrantedUtc
                });
            }

            dirty = false;
        }

        public ServerRewardRotationEntryStateArray BuildEntryState(uint rewardRotationIndex)
        {
            return RewardRotationEntryStateBuilder.BuildFromPersistedGrants(grants, rewardRotationIndex);
        }

        public void RecordGrant(uint rewardRotationIndex, uint contentId, uint rewardKeyId, byte rewardType, uint grantFlags)
        {
            if (contentId == 0u || rewardKeyId == 0u)
                return;

            AccountRewardRotationGrantModel existing = grants.SingleOrDefault(grant =>
                grant.RewardRotationIndex == rewardRotationIndex &&
                grant.ContentId == contentId &&
                grant.RewardKeyId == rewardKeyId &&
                grant.RewardType == rewardType);
            if (existing != null)
            {
                existing.GrantFlags = grantFlags;
                existing.GrantedUtc = DateTime.UtcNow;
            }
            else
            {
                grants.Add(new AccountRewardRotationGrantModel
                {
                    AccountId = account.Id,
                    RewardRotationIndex = rewardRotationIndex,
                    ContentId = contentId,
                    RewardKeyId = rewardKeyId,
                    RewardType = rewardType,
                    GrantFlags = grantFlags,
                    GrantedUtc = DateTime.UtcNow
                });
            }

            dirty = true;
        }
    }
}
