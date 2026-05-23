using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Achievement
{
    public static class AccountCurrencyAchievementUpdater
    {
        public static void Update(IPlayer player, AccountCurrencyType currencyType, ulong amount)
        {
            uint count = ToAchievementCount(amount);

            player.AchievementManager.CheckAchievements(player, AchievementType.AccountCurrencyEarned, (uint)currencyType, count: count);

            if (IsPrimalEssence(currencyType))
            {
                player.AchievementManager.CheckAchievements(player, AchievementType.PrimalEssenceEarned, 0u, count: count);
                PrimalMatrixQuestObjectiveUpdater.OnPrimalEssenceAccountCurrencyGranted(player, currencyType, amount);
            }
        }

        private static bool IsPrimalEssence(AccountCurrencyType currencyType)
        {
            return currencyType is AccountCurrencyType.CrimsonEssence
                or AccountCurrencyType.CobaltEssence
                or AccountCurrencyType.ViridianEssence
                or AccountCurrencyType.VioletEssence;
        }

        private static uint ToAchievementCount(ulong amount)
        {
            return amount > uint.MaxValue ? uint.MaxValue : (uint)amount;
        }
    }
}
