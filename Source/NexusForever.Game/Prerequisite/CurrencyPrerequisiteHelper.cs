using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Prerequisite
{
    internal static class CurrencyPrerequisiteHelper
    {
        public static ulong GetCurrencyAmount(IPlayer player, CurrencyType currencyType)
        {
            foreach (ICurrency currency in player.CurrencyManager)
            {
                if (currency.Id == currencyType)
                    return currency.Amount;
            }

            return 0ul;
        }
    }
}
