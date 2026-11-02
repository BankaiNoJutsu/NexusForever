namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private IDisposable inventorySaveLease;
        private int directSaveOperations;
        internal bool IsNormalSaveInProgress => saveInProgress || System.Threading.Volatile.Read(ref directSaveOperations) != 0;

        private bool RewardInventorySaveBlocked => ChallengeManager?.HasPendingRewardOperation == true
            || CurrencyManager is CurrencyManager { HasPendingContractCurrency: true } or CurrencyManager { RequiresContractCurrencyReload: true }
            || QuestManager?.HasPendingRewardOperation == true || QuestManager?.RequiresAuthoritativeReload == true
            || ChallengeManager?.RequiresAuthoritativeReload == true
            || Inventory?.SupportsTransactionalSoulboundDelivery == true && Inventory.IsSaveBlocked;

        private bool RewardStateRequiresReload => Inventory?.RequiresAuthoritativeReload == true
            || CurrencyManager is CurrencyManager { RequiresContractCurrencyReload: true }
            || QuestManager?.RequiresAuthoritativeReload == true
            || ChallengeManager?.RequiresAuthoritativeReload == true;

        private bool TryAcquireInventorySave(out IDisposable lease)
        {
            lease = null;
            if (QuestManager is QuestManager { IsContractCompletionMutationInProgress: true }
                || CurrencyManager is CurrencyManager currencies && (currencies.RequiresContractCurrencyReload || currencies.HasPendingContractCurrency && !AuctionBidSaveAllowed && !BankSaveAllowed)
                || ChallengeManager?.HasPendingRewardOperation == true || ChallengeManager?.RequiresAuthoritativeReload == true
                || QuestManager?.HasPendingRewardOperation == true || QuestManager?.RequiresAuthoritativeReload == true) return false;
            return Inventory?.SupportsTransactionalSoulboundDelivery != true || Inventory.TryAcquireSaveLease(out lease);
        }

        private void ReleaseInventorySave()
        {
            inventorySaveLease?.Dispose();
            inventorySaveLease = null;
        }

        private void DeferInventorySave(Action callback, Action failureCallback)
        {
            saveRequestedDuringSave = true;
            deferredSaveCallback += callback;
            deferredSaveFailureCallback += failureCallback;
            forceSave = true;
        }

        private void CheckInventorySaveAllowed(bool frozen = false)
        {
            if (CurrencyManager is CurrencyManager currencies && (currencies.RequiresContractCurrencyReload || currencies.HasPendingContractCurrency && !AuctionBidSaveAllowed && !BankSaveAllowed))
                throw new InvalidOperationException("Player save overlaps pending or quarantined Contract currency work.");
            if (!frozen && QuestManager is QuestManager { HasContractCompletionWork: true })
                throw new InvalidOperationException("Player save requires the frozen Contract journal before Auth enrollment.");
            if (!frozen && (QuestManager?.HasPendingRewardOperation == true || QuestManager?.RequiresAuthoritativeReload == true))
                throw new InvalidOperationException("Player save overlaps pending or quarantined Quest reward work.");
            if (!frozen && (ChallengeManager?.HasPendingRewardOperation == true || ChallengeManager?.RequiresAuthoritativeReload == true))
                throw new InvalidOperationException("Player save overlaps pending or quarantined Challenge reward work.");
            if (Inventory?.SupportsTransactionalSoulboundDelivery == true
                && (Inventory.HasPendingRewardDelivery || Inventory.RequiresAuthoritativeReload))
                throw new InvalidOperationException("Player save overlaps a pending or quarantined reward delivery.");
        }
    }
}
