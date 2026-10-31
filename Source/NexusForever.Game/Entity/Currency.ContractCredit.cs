using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;

namespace NexusForever.Game.Entity
{
    public partial class Currency
    {
        // This owner never crosses to the SQL worker. Only its immutable Before
        // witness is enrolled in the command; executor proof returns to the actor.
        internal ContractCreditOwner CaptureContractCredit(bool existed, ulong cap)
        {
            lock (persistence.Gate)
            {
                AssertIdentity(); persistence.AssertRewardOwner();
                if (Entry.CapAmount != cap || existed && persistence.NeedsSave || !existed && amount != 0)
                    throw new InvalidOperationException("Contract credit requires its clean loaded wallet baseline.");
                var before = new ContractCurrencyBefore(existed, amount);
                var row = persistence.CaptureReward(value => new ContractCurrencyBefore(value != null, value?.Amount ?? 0),
                    value => value?.Exists == true ? new CurrencyState(owner, currencyId, value.Amount) : null);
                var candidates = row.Candidates();
                if (candidates.Count != 1 || candidates[0] != before)
                    throw new InvalidOperationException("Contract credit cannot adopt an uncertain currency baseline.");
                row.Retire(); return new(this, row, before, cap);
            }
        }

        internal sealed class ContractCreditOwner(Currency owner, QuestRewardRow<ContractCurrencyBefore> row,
            ContractCurrencyBefore before, ulong cap)
        {
            internal ContractCurrencyBefore Before { get; } = before;
            private bool ended;
            internal void Validate()
            {
                lock (owner.persistence.Gate)
                {
                    owner.AssertIdentity();
                    if (ended || owner.Entry.CapAmount != cap || owner.amount != Before.Amount)
                        throw new InvalidOperationException("Contract currency lost its retained live baseline.");
                }
            }
            internal void Adopt(ulong after)
            {
                lock (owner.persistence.Gate)
                {
                    Validate();
                    if (cap != 0 && after > cap) throw new InvalidOperationException("Contract currency exceeds its captured cap.");
                    row.Adopt(new(true, after)); owner.amount = after; row.Retire(false); ended = true;
                }
            }
            internal void Cancel()
            {
                lock (owner.persistence.Gate) { Validate(); row.Retire(false); ended = true; }
            }
        }
    }
}
