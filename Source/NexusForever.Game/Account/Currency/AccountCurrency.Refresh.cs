using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrency
    {
        private RefreshReservation pendingRefresh;
        internal bool IsPersistenceBusy { get { lock (saveGate) return pendingSave != null || pendingRefresh != null; } }

        internal RefreshReservation CaptureRefresh(AccountWalletOperation required = null)
        {
            lock (saveGate)
            {
                EnsureCurrent();
                if (pendingSave != null || pendingRefresh != null || account.Id == 0 || (uint)CurrencyId is 0 or > byte.MaxValue
                    || CurrencyId == AccountCurrencyType.CosmicReward && account.LoyaltyManager != null
                    || required != null && (required.AccountId != account.Id || required.Currency != CurrencyId))
                    throw new InvalidOperationException("Wallet observation requires its idle original account/currency owner.");
                pendingRefresh = new(this, account.Id, CurrencyId, database, operations.ToArray(),
                    (saveMask & AccountCurrencySaveMask.Create) != 0, required);
                return pendingRefresh;
            }
        }

        // The worker sees only copied identities and operations. Its private
        // result is consumed on the actor; it never mutates the live wallet.
        internal sealed class RefreshReservation(AccountCurrency owner, uint accountId, AccountCurrencyType currency,
            string expectedDatabase, AccountWalletOperation[] captured, bool allowMissing, AccountWalletOperation required) : IDisposable
        {
            private sealed record Observation(string Database, ulong Amount, bool Exists, HashSet<string> Applied);
            private Task<Observation> observation;
            private bool adopted;
            internal Task ReadAsync(Func<AuthContext> contexts, Task beforeRead = null, ulong? epoch = null)
            {
                ArgumentNullException.ThrowIfNull(contexts);
                lock (owner.saveGate)
                {
                    if (!ReferenceEquals(owner.pendingRefresh, this) || observation != null)
                        throw new InvalidOperationException("Wallet observation is no longer the retained read owner.");
                    return observation = Task.Run(async () =>
                    {
                        if (beforeRead != null) await beforeRead;
                        return await Read(contexts, epoch);
                    });
                }
            }

            private async Task<Observation> Read(Func<AuthContext> contexts, ulong? epoch)
            {
                string retained = expectedDatabase;
                AuthContext Fresh()
                {
                    var context = contexts() ?? throw new InvalidOperationException("Missing wallet observation context.");
                    try
                    {
                        string key = AccountCurrencyOperationRepository.DatabaseKey(context);
                        retained ??= key;
                        if (key != retained || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                            throw new InvalidOperationException("Wallet observation changed its database or context owner.");
                        return context;
                    }
                    catch { context.Dispose(); throw; }
                }
                await using var strategy = Fresh();
                return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = Fresh();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
                    // Same account-before-wallet lock order as the writer. All
                    // reads share one transaction; no allocator or grant runs.
                    var account = await (mysql
                        ? context.Account.FromSqlInterpolated($"SELECT * FROM account WHERE id={accountId} FOR UPDATE")
                        : context.Account.Where(row => row.Id == accountId)).AsNoTracking().SingleOrDefaultAsync();
                    if (account == null) throw new InvalidOperationException("Wallet observation lost its account.");
                    if (epoch.HasValue && !await context.AccountWorldSession.AnyAsync(row => row.AccountId == accountId && row.Epoch == epoch.Value))
                        throw new InvalidOperationException("Wallet observation lost its admitted account epoch.");
                    byte id = (byte)currency;
                    var wallet = await (mysql
                        ? context.AccountCurrency.FromSqlInterpolated($"SELECT * FROM account_currency WHERE id={accountId} AND currencyId={id} FOR UPDATE")
                        : context.AccountCurrency.Where(row => row.Id == accountId && row.CurrencyId == id)).AsNoTracking().SingleOrDefaultAsync();
                    if (wallet == null && !allowMissing) throw new InvalidOperationException("A retained wallet disappeared.");
                    var applied = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var operation in captured.Concat(required == null ? [] : new[] { required }))
                    {
                        var receipt = await (mysql
                            ? context.AccountCurrencyOperation.FromSqlInterpolated($"SELECT * FROM account_currency_operation WHERE operationKey={operation.Key} FOR UPDATE")
                            : context.AccountCurrencyOperation.Where(row => row.OperationKey == operation.Key)).AsNoTracking().SingleOrDefaultAsync();
                        if (receipt == null)
                        {
                            if (ReferenceEquals(operation, required)) throw new InvalidOperationException("Required account credit has no durable receipt.");
                            continue;
                        }
                        AccountCurrencyOperationRepository.ValidateReceipt(receipt, operation);
                        if (wallet == null) throw new InvalidOperationException("Wallet receipts survived without their wallet.");
                        applied.Add(operation.Key);
                    }
                    await transaction.CommitAsync();
                    return new Observation(retained, wallet?.Amount ?? 0, wallet != null, applied);
                });
            }

            internal bool TryAdopt()
            {
                lock (owner.saveGate)
                {
                    if (!ReferenceEquals(owner.pendingRefresh, this) || adopted) return false;
                    try
                    {
                        owner.EnsureCurrent();
                        if (observation?.IsCompletedSuccessfully != true || owner.pendingSave != null || owner.account.Id != accountId
                            || owner.CurrencyId != currency || expectedDatabase != null && owner.database != expectedDatabase)
                            throw new InvalidOperationException("Wallet observation has no matching completed read.");
                        var read = observation.Result;
                        if (owner.database != null && owner.database != read.Database)
                            throw new InvalidOperationException("Wallet observation changed its retained database.");
                        // A save cannot run during this reservation. Applied
                        // captured keys are already in SQL; all other, including
                        // later, semantic operations still need exactly one save.
                        ulong visible = read.Amount;
                        foreach (var operation in owner.operations.Where(row => !read.Applied.Contains(row.Key)))
                            visible = AccountCurrencyOperationRepository.Apply(visible, operation.Amount, operation.Subtract);
                        owner.amount = visible; owner.database ??= read.Database;
                        owner.operations.RemoveAll(row => read.Applied.Contains(row.Key));
                        owner.saveMask = owner.operations.Count == 0 ? AccountCurrencySaveMask.None : AccountCurrencySaveMask.Amount;
                        if (!read.Exists) owner.saveMask |= AccountCurrencySaveMask.Create;
                        adopted = true; return true;
                    }
                    catch { owner.requiresReload = true; owner.pendingRefresh = null; return false; }
                }
            }

            internal void Quarantine()
            {
                lock (owner.saveGate)
                {
                    if (ReferenceEquals(owner.pendingRefresh, this)) owner.pendingRefresh = null;
                    owner.requiresReload = true;
                }
            }
            public void Dispose()
            {
                // A canceled read never writes SQL or acknowledges operations.
                lock (owner.saveGate)
                    if (ReferenceEquals(owner.pendingRefresh, this)) owner.pendingRefresh = null;
            }
        }
    }
}
