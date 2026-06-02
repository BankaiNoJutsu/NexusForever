using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 55: handler table <c>14049e7e0</c> reads player currency amount then
    /// <c>PrerequisiteManager_ApplyComparison</c> (<c>1404a2090</c>). <c>objectId0</c> is
    /// <see cref="CurrencyType"/> id; <c>value0</c> is required amount.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Currency)]
    [PrerequisiteCheck(PrerequisiteType.Unknown240)]
    public class PrerequisiteCheckCurrency : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            ulong amount = CurrencyPrerequisiteHelper.GetCurrencyAmount(player, (CurrencyType)objectId);
            return PrerequisiteCompare.Compare(comparison, (uint)Math.Min(amount, uint.MaxValue), value);
        }
    }
}
