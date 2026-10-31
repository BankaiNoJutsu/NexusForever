using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private readonly Func<AuthContext> contractAuthContexts;
        private Func<CharacterContext> contractCharacterContexts;
        private ContractAccountSync contractAccounts;
        private readonly HashSet<string> observedContractAccounts = new(StringComparer.Ordinal);
        private double accountPollSeconds = 30;

        private bool QueueContractAccounts(bool probe = false)
        {
            // Legacy/custom adapters retain their established path. Real Player
            // initialization supplies both factories and the durable wallet owner.
            if (contractAuthContexts == null || player.Account?.CurrencyManager is not AccountCurrencyManager) return false;
            if (contractAccounts != null) return true;
            if (player.QuestManager is not QuestManager) throw new InvalidOperationException("Contract account synchronization requires its Quest owner.");
            var operation = new ContractAccountSync(player, contractCharacterContexts, contractAuthContexts, new ContractRewardAwardCatalog(new ContractRewardCatalog(gameTableManager)),
                probe, probe ? observedContractAccounts : null);
            contractAccounts = operation;
            if (rewardQueue.EnqueueAccounts(operation)) return true;
            ReleaseContractAccounts(operation); throw new InvalidOperationException("Contract account synchronization lost its reward queue.");
        }
        private void ReleaseContractAccounts(ContractAccountSync operation)
        {
            operation?.Dispose();
            if (ReferenceEquals(contractAccounts, operation)) contractAccounts = null;
        }
        private void CompleteContractAccounts(ContractAccountSync operation)
        {
            if (operation == null) return;
            if (!ReferenceEquals(contractAccounts, operation) || !operation.CanPublish()) throw new InvalidOperationException("Contract drain completion lost its actor.");
            observedContractAccounts.UnionWith(operation.ObservedClaims); accountPollSeconds = 30;
        }
        private void UpdateContractAccounts(double lastTick)
        {
            if (contractAuthContexts == null || contractAccounts != null || HasPendingRewardOperation || !double.IsFinite(lastTick) || lastTick <= 0
                || player.Session is NetworkSession { IsDisconnecting: true } || player.LogoutManager?.State is LogoutState.Logout or LogoutState.Finished) return;
            accountPollSeconds -= lastTick;
            if (accountPollSeconds > 0) return;
            accountPollSeconds = 30;
            try { QueueContractAccounts(probe: true); }
            catch { RequireRewardReload(); player.Session?.ForceDisconnect(); }
        }
    }
}
