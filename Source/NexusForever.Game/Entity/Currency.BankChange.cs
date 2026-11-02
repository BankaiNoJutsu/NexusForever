using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    public partial class Currency
    {
        private BankChangeOwner bankChange;

        internal BankChangeOwner CaptureBankChange(ulong after)
            => CaptureChange(after, null);

        internal BankChangeOwner CaptureFrozenBankChange(ulong after, object token)
        {
            ArgumentNullException.ThrowIfNull(token);
            return CaptureChange(after, token);
        }

        private BankChangeOwner CaptureChange(ulong after, object token)
        {
            lock (persistence.Gate)
            {
                AssertIdentity(); persistence.AssertRewardOwner();
                if (bankChange != null || auctionOwner != null || Entry.CapAmount != 0 && after > Entry.CapAmount)
                    throw new InvalidOperationException("Currency already belongs to a bank operation or exceeds its cap.");
                var frozen = persistence.CaptureNormal(token);
                var acknowledgement = persistence.CaptureReward(value => value, value => value);
                frozen.Value = new(owner, currencyId, after); frozen.NeedsSave = true;
                bankChange = new(this, frozen, acknowledgement, amount, after, Entry.CapAmount);
                return bankChange;
            }
        }

        internal sealed class BankChangeOwner
        {
            private readonly Currency owner;
            private readonly QuestRowPersistence<CharacterCurrencyModel, CurrencyState>.NormalSnapshot snapshot;
            private readonly QuestRewardRow<CurrencyState> acknowledgement;
            private readonly ulong cap;
            internal ulong Before { get; }
            internal ulong After { get; }
            private bool adopted, ended;
            internal bool Reconciling { get; private set; }
            internal void Reconcile(bool committed, CharacterCurrencyModel stored)
            {
                lock (owner.persistence.Gate)
                {
                    Validate();
                    if (adopted || Reconciling || stored != null && (stored.Id != owner.owner || stored.CurrencyId != owner.currencyId))
                        throw new InvalidOperationException("Bank currency changed its reconciliation owner.");
                    var actual = stored == null ? null : new CurrencyState(stored.Id, stored.CurrencyId, stored.Amount);
                    if (!acknowledgement.Candidates().Contains(actual)) throw new InvalidOperationException("Bank currency changed its proven SQL baseline.");
                    if (committed) acknowledgement.Adopt(actual); else acknowledgement.Rebase(actual);
                    owner.amount = committed ? After : Before; adopted = Reconciling = true;
                }
            }

            internal BankChangeOwner(Currency owner, QuestRowPersistence<CharacterCurrencyModel, CurrencyState>.NormalSnapshot snapshot,
                QuestRewardRow<CurrencyState> acknowledgement, ulong before, ulong after, ulong cap)
            { this.owner = owner; this.snapshot = snapshot; this.acknowledgement = acknowledgement; Before = before; After = after; this.cap = cap; }

            private void Validate()
            {
                owner.AssertIdentity();
                if (ended || !ReferenceEquals(owner.bankChange, this) || owner.Entry.CapAmount != cap)
                    throw new InvalidOperationException("Bank currency lost its retained owner or cap.");
            }
            internal void Stage(CharacterContext context)
            {
                lock (owner.persistence.Gate) { Validate(); owner.AssertLoadedDatabase(context); owner.persistence.Save(context, force: true, frozen: snapshot); }
            }
            internal void Adopt()
            {
                lock (owner.persistence.Gate)
                {
                    Validate();
                    if (adopted || owner.amount != Before) throw new InvalidOperationException("Bank currency lost its captured live amount.");
                    var proof = acknowledgement.Candidates();
                    if (!owner.persistence.WasAttempted || proof.Count != 1 || proof[0] != snapshot.Value)
                        throw new InvalidOperationException("Bank currency has no confirmed SQL settlement.");
                    acknowledgement.Adopt(snapshot.Value); owner.amount = After; adopted = true;
                }
            }
            internal void ApplyDeferred(ulong next)
            {
                lock (owner.persistence.Gate)
                {
                    Validate();
                    if (!adopted || cap != 0 && next > cap) throw new InvalidOperationException("Bank currency cannot replay before adoption.");
                    owner.amount = next; owner.persistence.Dirty((int)CurrencySaveMask.Amount);
                }
            }
            internal void End()
            { lock (owner.persistence.Gate) { Validate(); ended = true; owner.bankChange = null; } }
            internal void Quarantine()
            { lock (owner.persistence.Gate) if (ended) owner.persistence.RetireRewardOwner(); }
        }
    }
}
