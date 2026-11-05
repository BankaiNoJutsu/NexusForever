using NexusForever.Database.Auth;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrencyManager
    {
        internal VendorWallets CaptureVendorWallets(IEnumerable<VendorAccountPurchase.Cost> costs)
        {
            lock (currencyGate)
            {
                if (walletRefresh != null || account.WorldSessionEpoch == 0 || !ReferenceEquals(account.CurrencyManager, this))
                    throw new InvalidOperationException("Vendor payment requires its original admitted wallets.");
                var rows = new List<(AccountCurrency Wallet, AccountCurrency.RefreshReservation Refresh)>();
                try
                {
                    foreach (var cost in costs)
                    {
                        if (!currencies.TryGetValue(cost.Currency, out var value) || value is not AccountCurrency wallet || !wallet.CanAfford(cost.Amount))
                            throw new InvalidOperationException("Vendor payment lost its original wallet or available balance.");
                        rows.Add((wallet, wallet.CaptureRefresh(journal: cost.Currency == AccountCurrencyType.CosmicReward)));
                    }
                    return new(this, account.WorldSessionEpoch, account.Session, rows);
                }
                catch
                {
                    foreach (var row in rows) row.Refresh.Dispose();
                    throw;
                }
            }
        }

        internal sealed class VendorWallets(AccountCurrencyManager manager, ulong epoch, IGameSession session,
            List<(AccountCurrency Wallet, AccountCurrency.RefreshReservation Refresh)> rows) : IDisposable
        {
            private bool Current() => manager.account.WorldSessionEpoch == epoch && ReferenceEquals(manager.account.Session, session)
                && ReferenceEquals(manager.account.CurrencyManager, manager) && session is not NetworkSession { IsDisconnecting: true };

            internal Task ReadAsync(Func<AuthContext> contexts)
                => Task.WhenAll(rows.Select(row => row.Refresh.ReadAsync(contexts, epoch: epoch)));

            internal void Adopt()
            {
                lock (manager.currencyGate)
                {
                    if (!Current())
                        throw new InvalidOperationException("Vendor payment cannot adopt its original wallet observations.");
                    (ulong Wallet, ulong Revision)? loyalty = null;
                    foreach (var row in rows)
                    {
                        if (!row.Refresh.TryAdopt(out var journal))
                            throw new InvalidOperationException("Vendor payment cannot adopt its original wallet observations.");
                        loyalty ??= journal;
                    }
                    Dispose();
                    if (loyalty.HasValue)
                        manager.CompleteCosmicMutations([], loyalty.Value.Wallet, loyalty.Value.Revision, publish: false);
                }
            }

            internal void Publish(Func<bool> currentActor, Action<IWritable> send)
            {
                foreach (var row in rows)
                {
                    if (!Current() || !currentActor()) throw new InvalidOperationException("Vendor wallet publication lost its original session.");
                    send(BuildCurrencyGrant(row.Wallet, 0));
                    if (!Current() || !currentActor()) throw new InvalidOperationException("Vendor wallet publication lost its original session.");
                    send(new ServerWalletUpdate((uint)row.Wallet.CurrencyId));
                }
            }

            internal IEnumerable<Func<IWritable>> CapturePublication()
            {
                foreach (var row in rows)
                {
                    var packet = BuildCurrencyGrant(row.Wallet, 0);
                    byte currency = packet.AccountCurrency.AccountCurrencyType;
                    ulong amount = packet.AccountCurrency.Amount, reason = packet.Reason, reserved = packet.Reserved;
                    yield return () =>
                    {
                        if (!Current()) throw new InvalidOperationException("Wallet publication changed its original actor.");
                        return new ServerAccountCurrencyGrant { AccountCurrency = new() { AccountCurrencyType = currency, Amount = amount }, Reason = reason, Reserved = reserved };
                    };
                    yield return () =>
                    {
                        if (!Current()) throw new InvalidOperationException("Wallet publication changed its original actor.");
                        return new ServerWalletUpdate(currency);
                    };
                }
            }

            internal void Quarantine()
            {
                foreach (var row in rows) row.Refresh.Quarantine();
            }

            public void Dispose()
            {
                foreach (var row in rows) row.Refresh.Dispose();
            }
        }
    }
}
