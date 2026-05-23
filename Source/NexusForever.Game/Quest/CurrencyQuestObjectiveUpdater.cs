using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits EarnCurrency quest objectives when currency is granted.
    /// Evidence: QuestObjective type 42 <c>Data</c> matches <see cref="CurrencyType"/> ids.
    /// </summary>
    public static class CurrencyQuestObjectiveUpdater
    {
        public static void OnCurrencyAdded(IPlayer player, CurrencyType currencyType, ulong addedAmount)
        {
            if (player?.QuestManager == null || addedAmount == 0ul)
                return;

            uint progress = addedAmount > uint.MaxValue ? uint.MaxValue : (uint)addedAmount;
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.EarnCurrency, (uint)currencyType, progress);
        }
    }
}
