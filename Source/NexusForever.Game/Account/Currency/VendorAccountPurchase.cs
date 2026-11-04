using System.Data;
using System.Collections.Immutable;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    internal enum VendorAccountAuthorization { Authorized, Delivered, Cancelled }

    /// <summary>Durable account payment and compensation for a single Character delivery transaction.</summary>
    public sealed class VendorAccountPurchase
    {
        private const string DebitPrefix = "vendor-debit:";
        private const string DeliveredPrefix = "vendor-account-delivered:";
        private const string CancelledPrefix = "vendor-account-cancelled:";

        private readonly Func<CharacterContext> characters;
        private readonly Func<AuthContext> auth;
        private readonly ushort realm;

        internal VendorAccountPurchase(Func<CharacterContext> characters, Func<AuthContext> auth, ushort realm)
        {
            this.characters = characters;
            this.auth = auth;
            this.realm = realm;
        }

        public static Task RecoverAtAdmissionAsync(AccountModel account, AuthDatabase auth, CharacterDatabase characters, ushort realm)
            => new VendorAccountPurchase(characters.CreateContext, auth.CreateContext, realm).RecoverAdmissionAsync(account);

        private async Task RecoverAdmissionAsync(AccountModel account)
        {
            if (realm == 0 || account.Id == 0 || account.WorldSessionEpoch == 0)
                throw new InvalidOperationException("Vendor recovery requires an admitted account and realm.");

            await using var reader = auth();
            string route = AccountCurrencyOperationRepository.DatabaseKey(reader);
            var anchors = await reader.AccountCurrencyOperation.AsNoTracking()
                .Where(row => row.AccountId == account.Id && row.Subtract
                    && row.OperationKey.StartsWith(DebitPrefix) && row.OperationKey.EndsWith(":0"))
                .OrderBy(row => row.OperationKey).ToArrayAsync();
            var local = anchors.Select(row => (Debit: row, Intent: ReadIntent(row)))
                .Where(entry => entry.Intent.Realm == realm).ToArray();
            if (local.Length == 0) return;

            if (local.Any(entry => entry.Intent.AuthRoute != route))
                throw new InvalidOperationException("Vendor recovery found a debit from a different Auth route.");
            if (!await reader.AccountWorldSession.AnyAsync(row => row.AccountId == account.Id && row.Epoch == account.WorldSessionEpoch))
                throw new InvalidOperationException("Vendor recovery lost its admitted account epoch.");

            foreach (var entry in local)
                await RecoverAsync(entry.Debit, entry.Intent);

            byte[] affected = local.SelectMany(entry => entry.Intent.Costs).Select(cost => (byte)cost.Currency).Distinct().ToArray();
            await using var fresh = auth();
            if (AccountCurrencyOperationRepository.DatabaseKey(fresh) != route)
                throw new InvalidOperationException("Vendor recovery changed its admitted Auth route.");
            var refreshed = await fresh.Account.AsNoTracking()
                .Where(row => row.Id == account.Id && fresh.AccountWorldSession.Any(epoch => epoch.AccountId == row.Id && epoch.Epoch == account.WorldSessionEpoch))
                .Select(row => new { Wallets = row.AccountCurrency.Where(wallet => affected.Contains(wallet.CurrencyId)).ToArray() })
                .SingleOrDefaultAsync();
            if (refreshed == null || refreshed.Wallets.Length != affected.Length)
                throw new InvalidOperationException("Vendor recovery lost its admitted account or wallet rows.");
            account.AccountCurrency = account.AccountCurrency.Where(wallet => !affected.Contains(wallet.CurrencyId)).Concat(refreshed.Wallets).ToList();
        }

        internal sealed record Cost(AccountCurrencyType Currency, ulong Amount);
        internal sealed record Intent(string Id, uint Account, ulong Character, ushort Realm, string CharacterRoute, string AuthRoute,
            uint Item, uint Quantity, string QuoteFingerprint, ImmutableArray<Cost> Costs);

        internal Intent Create(uint account, ulong character, uint item, uint quantity, string quoteFingerprint, IEnumerable<Cost> costs)
        {
            using var source = characters();
            using var wallet = auth();
            RequireDurableStorage(wallet.Database.GetDbConnection());
            var intent = new Intent(Guid.NewGuid().ToString("N"), account, character, realm, CharacterRoute(source),
                AccountCurrencyOperationRepository.DatabaseKey(wallet), item, quantity, quoteFingerprint, costs.OrderBy(cost => cost.Currency).ToImmutableArray());
            Validate(intent);
            return intent;
        }

        // Enrollment runs once for a new attempt. The delivery, its Character
        // payment and the decision are owned by the same Character transaction.
        internal async Task<VendorAccountAuthorization> AuthorizeAsync(CharacterContext context, Intent intent,
            Action<CharacterContext> enrollDelivery)
        {
            ArgumentNullException.ThrowIfNull(context);
            try
            {
                ArgumentNullException.ThrowIfNull(enrollDelivery);
                Validate(intent);
                RequireRoute(context, intent);
                var transaction = context.Database.CurrentTransaction;
                if (transaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    throw new InvalidOperationException("Vendor authorization requires a serializable Character transaction.");
                string payload = JsonSerializer.Serialize(intent);
                await LockCharacter(context, intent, requireActive: true);
                var marker = await ReadMarker(context, intent.Id);
                if (marker?.Payload == CancelledPrefix + payload) return VendorAccountAuthorization.Cancelled;
                if (marker != null)
                {
                    if (marker.Payload != DeliveredPrefix + payload || !await Observed(intent, subtract: true))
                        throw new InvalidOperationException("Vendor delivery has no matching durable payment.");
                    return VendorAccountAuthorization.Delivered;
                }

                bool authorized = false;
                context.OnSaveValidation(() =>
                {
                    if (!authorized) throw new InvalidOperationException("Vendor delivery cannot flush before its account debit is confirmed.");
                });
                enrollDelivery(context);
                if (context.IsSaveRejected || !ReferenceEquals(context.Database.CurrentTransaction, transaction))
                    throw new InvalidOperationException("Vendor delivery lost its Character enrollment transaction.");

                await Apply(intent, subtract: true);
                StageMarker(context, intent.Id, DeliveredPrefix + payload);
                authorized = true;
                return VendorAccountAuthorization.Authorized;
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        // A fresh process can reconstruct the purchase from an immutable debit
        // receipt. Cancellation and delivery share the same Character lock and
        // key, so a late attempt cannot deliver after a refund.
        internal Task<VendorAccountAuthorization> RecoverAsync(AccountCurrencyOperationModel debit) => RecoverAsync(debit, ReadIntent(debit));

        private static Intent ReadIntent(AccountCurrencyOperationModel debit)
        {
            var intent = JsonSerializer.Deserialize<Intent>(debit.SourceJson);
            if (intent == null || intent.Realm == 0 || intent.Account != debit.AccountId)
                throw new InvalidOperationException("Vendor recovery found a debit with a foreign account or realm.");
            return intent;
        }

        private async Task<VendorAccountAuthorization> RecoverAsync(AccountCurrencyOperationModel debit, Intent intent)
        {
            Validate(intent);
            var operation = Operations(intent, subtract: true).SingleOrDefault(candidate => candidate.Key == debit.OperationKey);
            if (operation == null) throw new InvalidOperationException("Unknown vendor debit identity.");
            AccountCurrencyOperationRepository.ValidateReceipt(debit, operation);
            if (!await Observed(intent, subtract: true))
                throw new InvalidOperationException("Vendor recovery requires the complete durable debit.");

            string payload = JsonSerializer.Serialize(intent);
            await using var driver = characters();
            RequireRoute(driver, intent);
            var outcome = await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var context = characters();
                RequireRoute(context, intent);
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await LockCharacter(context, intent, requireActive: false);
                var marker = await ReadMarker(context, intent.Id);
                var result = VendorAccountAuthorization.Cancelled;
                if (marker == null)
                {
                    StageMarker(context, intent.Id, CancelledPrefix + payload);
                    await context.SaveChangesAsync();
                }
                else if (marker.Payload == DeliveredPrefix + payload) result = VendorAccountAuthorization.Delivered;
                else if (marker.Payload != CancelledPrefix + payload)
                    throw new InvalidOperationException("Vendor recovery found a foreign Character marker.");
                await transaction.CommitAsync();
                return result;
            });
            if (outcome == VendorAccountAuthorization.Cancelled) await Apply(intent, subtract: false);
            return outcome;
        }

        private async Task Apply(Intent intent, bool subtract)
        {
            if (await Observed(intent, subtract)) return;
            var operations = Operations(intent, subtract);
            try
            {
                await using var context = OpenAuth(intent);
                foreach (var operation in operations)
                {
                    AccountCurrencyOperationRepository repository = null;
                    context.EnlistSaveWork(unit =>
                    {
                        repository?.Detach();
                        repository = new(unit, operation.AccountId, operation.Currency, [operation]);
                        repository.Stage();
                    }, _ => { }, () => repository?.CanCommit() == true);
                }
                await context.SaveChangesAsync();
            }
            catch
            {
                // A lost Auth commit reply leaves the exact keys observable in a
                // fresh context. A partial or conflicting witness fails closed.
                if (!await Observed(intent, subtract)) throw;
            }
        }

        private async Task<bool> Observed(Intent intent, bool subtract)
        {
            await using var context = OpenAuth(intent);
            var operations = Operations(intent, subtract);
            string[] keys = operations.Select(operation => operation.Key).ToArray();
            var rows = await context.AccountCurrencyOperation.AsNoTracking().Where(row => keys.Contains(row.OperationKey)).ToArrayAsync();
            foreach (var row in rows)
                AccountCurrencyOperationRepository.ValidateReceipt(row, operations.Single(operation => operation.Key == row.OperationKey));
            if (rows.Length != 0 && rows.Length != operations.Length)
                throw new InvalidOperationException("Vendor wallet settlement has a partial durable witness.");
            return rows.Length == operations.Length;
        }

        private AuthContext OpenAuth(Intent intent)
        {
            var context = auth();
            try
            {
                RequireDurableStorage(context.Database.GetDbConnection());
                if (AccountCurrencyOperationRepository.DatabaseKey(context) == intent.AuthRoute) return context;
                throw new InvalidOperationException("Vendor purchase changed its Auth route.");
            }
            catch { context?.Dispose(); throw; }
        }

        private static AccountWalletOperation[] Operations(Intent intent, bool subtract)
        {
            string payload = JsonSerializer.Serialize(intent);
            string prefix = subtract ? DebitPrefix : "vendor-refund:";
            // The first key anchors the entire quote, even if a conflicting
            // retry supplies a different currency set under the same ID.
            return intent.Costs.Select((cost, index) => new AccountWalletOperation($"{prefix}{intent.Id}:{index}",
                intent.Account, cost.Currency, cost.Amount, subtract, payload)).ToArray();
        }

        private void Validate(Intent intent)
        {
            if (intent == null || !Guid.TryParseExact(intent.Id, "N", out var id) || id == Guid.Empty || id.ToString("N") != intent.Id
                || realm == 0 || intent.Realm != realm || intent.Account == 0 || intent.Character == 0 || intent.Item == 0 || intent.Quantity == 0
                || intent.AuthRoute?.Length != 64 || intent.CharacterRoute?.Length != 64 || intent.QuoteFingerprint?.Length != 64
                || intent.Costs.IsDefaultOrEmpty || intent.Costs.Length > 2
                || intent.Costs.Any(cost => cost == null || cost.Amount == 0 || !Enum.IsDefined(cost.Currency)
                    || cost.Currency is AccountCurrencyType.None or AccountCurrencyType.CosmicReward)
                || !intent.Costs.Select(cost => cost.Currency).SequenceEqual(intent.Costs.Select(cost => cost.Currency).Distinct().Order()))
                throw new InvalidOperationException("Vendor payment requires a canonical purchase and its original wallet owners.");
        }

        private static string CharacterRoute(CharacterContext context)
        {
            RequireDurableStorage(context.Database.GetDbConnection());
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CharacterDatabaseIdentity.Get(context))));
        }

        private static void RequireDurableStorage(DbConnection connection)
        {
            if (connection is not SqliteConnection sqlite) return;
            var configuration = new SqliteConnectionStringBuilder(sqlite.ConnectionString);
            if (string.IsNullOrEmpty(configuration.DataSource) || configuration.DataSource == ":memory:" || configuration.Mode == SqliteOpenMode.Memory)
                throw new InvalidOperationException("Vendor recovery requires durable database storage.");
        }

        private static void RequireRoute(CharacterContext context, Intent intent)
        {
            if (CharacterRoute(context) != intent.CharacterRoute)
                throw new InvalidOperationException("Vendor purchase changed its Character route.");
        }

        private static bool IsMySql(CharacterContext context)
            => context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;

        private static async Task LockCharacter(CharacterContext context, Intent intent, bool requireActive)
        {
            if (!IsMySql(context) && !context.Database.IsSqlite())
                throw new InvalidOperationException("Unsupported vendor Character provider.");
            var owner = await (IsMySql(context)
                ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={intent.Character} FOR UPDATE")
                : context.Character.Where(row => row.Id == intent.Character)).AsNoTracking().SingleOrDefaultAsync();
            if ((owner != null && owner.AccountId != intent.Account) || (requireActive && (owner == null || owner.DeleteTime != null)))
                throw new InvalidOperationException("Vendor purchase changed its Character owner.");
        }

        private static Task<BankReconciliationReceiptModel> ReadMarker(CharacterContext context, string id)
            => Markers(context, id).SingleOrDefaultAsync();

        private static IQueryable<BankReconciliationReceiptModel> Markers(CharacterContext context, string id)
            => (IsMySql(context)
                ? context.BankReconciliationReceipt.FromSqlInterpolated($"SELECT * FROM bank_reconciliation_receipt WHERE id={id} FOR UPDATE")
                : context.BankReconciliationReceipt.Where(row => row.Id == id)).AsNoTracking();

        private static void StageMarker(CharacterContext context, string id, string payload)
        {
            var marker = new BankReconciliationReceiptModel { Id = id, Payload = payload, CreatedUtc = DateTime.UtcNow };
            context.BankReconciliationReceipt.Add(marker);
            context.OnSaveValidation(phase =>
            {
                if (marker.Id != id || marker.Payload != payload || context.Entry(marker).State is EntityState.Detached or EntityState.Deleted
                    || (phase != CharacterSaveValidationPhase.BeforeFlush
                    && Markers(context, id).SingleOrDefault()?.Payload != payload))
                    throw new InvalidOperationException("Vendor purchase marker differs from its frozen decision.");
            });
        }
    }
}
