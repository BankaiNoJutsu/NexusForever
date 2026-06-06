using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 270: account loyalty/cosmic reward point threshold.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.LoyaltyRewards)]
    public class PrerequisiteCheckLoyaltyRewards : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            ulong amount = player.Account?.CurrencyManager?.GetCurrencyAmount(AccountCurrencyType.CosmicReward) ?? 0ul;
            return PrerequisiteCompare.Compare(comparison, (uint)Math.Min(amount, uint.MaxValue), value);
        }
    }
}
