using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits BeginMatrix quest objectives when the account engages with Primal Matrix progression.
    /// Evidence: type 48 uses <c>Data=0</c>; achievement type 152 (Primal Matrix essence totals) advances on
    /// account grants of Crimson/Cobalt/Viridian/Violet essence (AccountCurrencyType 15-18).
    /// Client matrix UI open packets remain unmapped; essence grants are the server-owned hook used here.
    /// </summary>
    public static class PrimalMatrixQuestObjectiveUpdater
    {
        public static void OnPrimalEssenceAccountCurrencyGranted(IPlayer player, AccountCurrencyType currencyType, ulong amount)
        {
            if (player?.QuestManager == null || amount == 0ul || !IsPrimalEssence(currencyType))
                return;

            CreditBeginMatrix(player);
        }

        public static void SyncBeginMatrixForQuest(IQuest quest)
        {
            if (quest?.Player?.QuestManager == null)
                return;

            if (!QuestHasIncompleteBeginMatrix(quest))
                return;

            if (!AccountHasPrimalEssence(quest.Player.Account))
                return;

            CreditBeginMatrix(quest.Player);
        }

        private static void CreditBeginMatrix(IPlayer player)
        {
            player.QuestManager?.ObjectiveUpdate(QuestObjectiveType.BeginMatrix, 0u, 1u);
        }

        private static bool QuestHasIncompleteBeginMatrix(IQuest quest)
        {
            foreach (IQuestObjective objective in quest)
            {
                if (objective.IsComplete())
                    continue;

                if (objective.ObjectiveInfo.Type == QuestObjectiveType.BeginMatrix)
                    return true;
            }

            return false;
        }

        private static bool AccountHasPrimalEssence(IAccount account)
        {
            if (account?.CurrencyManager == null)
                return false;

            return account.CurrencyManager.CanAfford(AccountCurrencyType.CrimsonEssence, 1ul)
                || account.CurrencyManager.CanAfford(AccountCurrencyType.CobaltEssence, 1ul)
                || account.CurrencyManager.CanAfford(AccountCurrencyType.ViridianEssence, 1ul)
                || account.CurrencyManager.CanAfford(AccountCurrencyType.VioletEssence, 1ul);
        }

        private static bool IsPrimalEssence(AccountCurrencyType currencyType)
        {
            return currencyType is AccountCurrencyType.CrimsonEssence
                or AccountCurrencyType.CobaltEssence
                or AccountCurrencyType.ViridianEssence
                or AccountCurrencyType.VioletEssence;
        }
    }
}
