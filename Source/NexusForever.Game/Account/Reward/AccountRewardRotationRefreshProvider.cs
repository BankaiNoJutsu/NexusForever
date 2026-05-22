using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.GameTable;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Account-aware reward rotation refresh: game-table schedule/content context plus persisted grant entry-state.
    /// </summary>
    public sealed class AccountRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        private readonly IAccount account;
        private readonly IGameTableManager gameTables;
        private readonly GameTableRewardRotationRefreshProvider gameTableProvider;

        public AccountRewardRotationRefreshProvider(IAccount account, IGameTableManager gameTables = null)
        {
            this.account = account ?? throw new ArgumentNullException(nameof(account));
            this.gameTables = gameTables ?? GameTableManager.Instance;
            gameTableProvider = new GameTableRewardRotationRefreshProvider(this.gameTables);
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            IAccountRewardRotationGrantManager grantManager = account.RewardRotationGrantManager;
            return gameTableProvider.Build(
                rewardRotationIndex,
                grantManager?.BuildEntryState(rewardRotationIndex));
        }
    }
}
