using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class CurrencyManager
    {
        private static readonly ILogger resourceConversionLog = LogManager.GetCurrentClassLogger();
        private readonly object currencyGraphGate = new();

        public bool TryApplyResourceConversion(IGameSession session, CurrencyType source, ulong sourceCost,
            CurrencyType surcharge, ulong surchargeCost, CurrencyType target, ulong grant)
        {
            var events = session?.Events;
            ulong character = player?.CharacterId ?? 0;
            bool OwnsActor() => session != null && ReferenceEquals(player?.Session, session)
                && player.CharacterId == character && ReferenceEquals(session.Events, events)
                && ReferenceEquals(player.CurrencyManager, this) && HasLoadedCurrencyOwner()
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            var debits = new Dictionary<CurrencyType, ulong>();
            if (sourceCost != 0) debits.Add(source, sourceCost);
            if (surcharge != CurrencyType.None && surchargeCost != 0)
            {
                ulong prior = debits.GetValueOrDefault(surcharge);
                if (surchargeCost > ulong.MaxValue - prior) return false;
                debits[surcharge] = prior + surchargeCost;
            }

            var rows = new Dictionary<CurrencyType, Currency>();
            Currency targetRow;
            lock (currencyGraphGate)
            {
                if (!OwnsActor() || IsMutationPublicationBlocked || ContractCurrencyBlocked(target)) return false;
                foreach (var debit in debits)
                {
                    if (ContractCurrencyBlocked(debit.Key) || !currencies.TryGetValue(debit.Key, out var currency)
                        || currency is not Currency row) return false;
                    rows.Add(debit.Key, row);
                }

                bool newTarget = !currencies.TryGetValue(target, out var storedTarget);
                var definition = gameTableManager.CurrencyType?.GetEntry((uint)target);
                if (definition == null || !newTarget && storedTarget is not Currency) return false;
                targetRow = newTarget ? CreateCurrencyOwner(definition) : (Currency)storedTarget;
                rows[target] = targetRow;

                // Hold the affected rows in a stable order while validating and applying the complete exchange.
                var ordered = rows.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
                int held = 0;
                try
                {
                    for (; held < ordered.Length; held++) Monitor.Enter(ordered[held].MutationGate);

                    var changes = new List<(Currency Row, ulong Amount)>();
                    foreach (var pair in rows)
                    {
                        ulong debit = debits.GetValueOrDefault(pair.Key);
                        ulong before = pair.Value.Amount;
                        if (debit > before) return false;
                        ulong remaining = before - debit;
                        ulong credit = pair.Key == target ? grant : 0;
                        if (credit > ulong.MaxValue - remaining) return false;
                        ulong after = remaining + credit;
                        try { pair.Value.ValidateAmountChange(after); }
                        catch (Exception error) when (error is InvalidOperationException or ArgumentOutOfRangeException)
                        {
                            return false;
                        }
                        changes.Add((pair.Value, after));
                    }

                    if (!OwnsActor()) return false;
                    foreach (var change in changes) change.Row.ApplyValidatedAmount(change.Amount);
                    if (newTarget) currencies.Add(target, targetRow);
                }
                finally
                {
                    while (held > 0) Monitor.Exit(ordered[--held].MutationGate);
                }
            }

            foreach (var row in rows)
                PublishBalance(row.Key, row.Value);
            try { CreditCurrencyProgress(targetRow, grant); }
            catch (Exception exception) { resourceConversionLog.Warn(exception, "Failed to publish progress for an accepted resource conversion."); }
            return true;

            void PublishBalance(CurrencyType type, Currency row)
            {
                if (!OwnsActor() || row.CharacterId != character || row.Id != type
                    || !currencies.TryGetValue(type, out var current) || !ReferenceEquals(current, row)) return;
                // A retry publishes the live balance, which may have changed since the conversion.
                try { PublishCurrencyAmount(row, session); }
                catch (Exception exception)
                {
                    resourceConversionLog.Warn(exception, "Failed to publish an accepted resource conversion balance.");
                    events?.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () => PublishBalance(type, row)));
                }
            }
        }
    }
}
