using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 293: client address <c>14049d3a0</c> reads
    /// <c>accountItemList+0xd0+objectId0*8</c>, the account-currency slot written by
    /// AccountItem accountCurrencyEnum add/remove paths, then compares against <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.AccountCurrencyAmount)]
    public class PrerequisiteCheckAccountCurrencyAmount : IPrerequisiteCheck
    {
        private const uint MaxClientAccountCurrencyType = 18u;

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (objectId > MaxClientAccountCurrencyType)
                return PrerequisiteCompare.Compare(comparison, 0u, value);

            ulong amount = player.Account?.CurrencyManager?.GetCurrencyAmount((AccountCurrencyType)objectId) ?? 0ul;
            return PrerequisiteCompare.Compare(comparison, (uint)Math.Min(amount, uint.MaxValue), value);
        }
    }
}
