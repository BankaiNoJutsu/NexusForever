using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    /// <summary>Mint a shared durable epoch; never reuse an uncertain committed allocation.</summary>
    public sealed class AccountWorldSessionAdmission(Func<AuthContext> contexts)
    {
        private string database;
        private readonly Func<AuthContext> create = contexts ?? throw new ArgumentNullException(nameof(contexts));

        public Task<AccountModel> AdmitAsync(uint accountId, string email, string sessionKey)
            => Task.Run(async () =>
            {
                if (accountId == 0 || string.IsNullOrEmpty(email) || !TryKey(sessionKey, out var supplied)) return null;
                await using var strategyContext = Fresh();
                return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = Fresh();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    bool sqlite = context.Database.IsSqlite();
                    if (!sqlite && context.Database.ProviderName is not ("Microting.EntityFrameworkCore.MySql" or "Pomelo.EntityFrameworkCore.MySql"))
                        throw new NotSupportedException("Unsupported World session admission provider.");
                    // Never send the credential as a SQL parameter: Auth enables
                    // sensitive EF logging. Read by public identity, then compare
                    // the private key in memory while this owner row is locked.
                    var owner = sqlite
                        ? await context.Account.AsNoTracking().SingleOrDefaultAsync(row => row.Id == accountId && row.Email == email)
                        : (await context.Account.FromSqlInterpolated($"SELECT * FROM `account` WHERE `id` = {accountId} AND `email` = {email} FOR UPDATE")
                            .AsNoTracking().ToListAsync()).SingleOrDefault();
                    if (owner == null || !TryKey(owner.SessionKey, out var stored) || !CryptographicOperations.FixedTimeEquals(stored, supplied))
                        return null;
                    var counter = await context.AccountWorldSession.SingleOrDefaultAsync(row => row.AccountId == accountId);
                    if (counter == null) { counter = new() { AccountId = accountId }; context.AccountWorldSession.Add(counter); }
                    ulong allocated = checked(counter.Epoch + 1);
                    counter.Epoch = allocated;
                    // Match the existing Realm account lookup's complete graph.
                    var account = await AccountGraph(context).SingleAsync(row => row.Id == accountId);
                    context.EnlistSaveWork(unit =>
                    {
                        if (counter.AccountId != accountId || counter.Epoch != allocated
                            || unit.ChangeTracker.Entries().Any(entry => !ReferenceEquals(entry.Entity, counter)))
                            throw new InvalidOperationException("World epoch staging lost its row ownership.");
                    }, _ => { }, () => counter.AccountId == accountId && counter.Epoch == allocated
                        && context.ChangeTracker.Entries().All(entry => ReferenceEquals(entry.Entity, counter) && entry.State == EntityState.Unchanged)
                        && context.AccountWorldSession.AsNoTracking().SingleOrDefault(row => row.AccountId == accountId)?.Epoch == allocated);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    account.WorldSessionEpoch = allocated;
                    return account;
                });
            });

        private AuthContext Fresh()
        {
            var context = create() ?? throw new InvalidOperationException("World admission context factory returned null.");
            try
            {
                if (context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("World admission requires fresh untracked contexts.");
                string identity = context.Database.ProviderName + ":" + Convert.ToHexString(SHA256.HashData(
                    Encoding.UTF8.GetBytes(context.Database.GetDbConnection().ConnectionString)));
                string expected = Interlocked.CompareExchange(ref database, identity, null) ?? identity;
                if (expected != identity) throw new InvalidOperationException("World admission changed its database identity.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private static bool TryKey(string key, out byte[] bytes)
        {
            bytes = null;
            if (key?.Length != 32) return false;
            try { bytes = Convert.FromHexString(key); return true; }
            catch (FormatException) { return false; }
        }

        private static IQueryable<AccountModel> AccountGraph(AuthContext context)
            => context.Account.AsNoTracking().AsSplitQuery()
                .Include(a => a.AccountCostumeUnlock).Include(a => a.AccountCurrency)
                .Include(a => a.AccountGenericUnlock).Include(a => a.AccountInventory)
                .Include(a => a.AccountPendingItem).Include(a => a.Loyalty).Include(a => a.LoyaltySlots)
                .Include(a => a.AccountDailyLogin).Include(a => a.AccountRewardRotationGrant)
                .Include(a => a.AccountFortuneSession).Include(a => a.AccountItemCooldown)
                .Include(a => a.AccountKeybinding).Include(a => a.AccountEntitlement)
                .Include(a => a.AccountPermission).Include(a => a.AccountRole);
    }
}
