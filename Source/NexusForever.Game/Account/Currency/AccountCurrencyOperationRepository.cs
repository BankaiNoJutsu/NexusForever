using System.Security.Cryptography;
using System.Text;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    internal sealed record AccountWalletOperation(string Key, uint AccountId, AccountCurrencyType Currency,
        ulong Amount, bool Subtract, string SourceJson = "");

    // Called only inside the existing Auth save transaction. The account lock
    // serializes actor saves and future compensation receivers; receipts make
    // retries safe after either acknowledged or unacknowledged commits.
    internal sealed class AccountCurrencyOperationRepository
    {
        private readonly AuthContext context;
        private readonly uint accountId;
        private readonly byte currencyId;
        private readonly IReadOnlyList<AccountWalletOperation> operations;
        private readonly bool vendorLoyalty;
        private readonly List<(object Model, PropertyValues Values)> owned = [];
        private readonly List<(AccountWalletOperation Operation, ulong Before, ulong After)> witnesses = [];
        internal ulong Amount { get; private set; }
        internal bool HasNewOperation => owned.Any(row => row.Model is AccountCurrencyOperationModel);

        internal static string DatabaseKey(AuthContext context)
        {
            string provider = context.Database.ProviderName;
            if (provider != "Microsoft.EntityFrameworkCore.Sqlite" && provider?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true)
                throw new InvalidOperationException("Unsupported wallet persistence provider.");
            var connection = context.Database.GetDbConnection();
            var builder = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
            builder.Remove("Password"); builder.Remove("Pwd");
            var normalized = new DbConnectionStringBuilder();
            foreach (string key in builder.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                normalized[key.ToLowerInvariant()] = builder[key];
            string identity = provider + "\n" + normalized.ConnectionString;
            if (provider == "Microsoft.EntityFrameworkCore.Sqlite" && connection.DataSource == ":memory:")
                identity += "\n" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connection);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        }

        internal AccountCurrencyOperationRepository(AuthContext context, uint accountId, AccountCurrencyType currency,
            IReadOnlyList<AccountWalletOperation> operations, bool vendorLoyalty = false)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context)); this.accountId = accountId;
            if (accountId == 0 || (uint)currency is 0 or > byte.MaxValue || context.Database.CurrentTransaction == null
                || operations == null || operations.Count == 0 || operations.Any(operation => operation == null
                    || operation.AccountId != accountId || operation.Currency != currency || string.IsNullOrWhiteSpace(operation.Key)
                    || operation.Key.Length > 128 || operation.SourceJson == null || operation.SourceJson.Length > 131072)
                || operations.Select(operation => operation.Key).Distinct(StringComparer.Ordinal).Count() != operations.Count)
                throw new InvalidOperationException("Wallet operations require one owned currency and retained distinct identities.");
            if (vendorLoyalty && (currency != AccountCurrencyType.CosmicReward || operations.Count != 1))
                throw new InvalidOperationException("Vendor Loyalty settlement requires its single Cosmic operation.");
            currencyId = (byte)currency; this.operations = Array.AsReadOnly(operations.ToArray());
            this.vendorLoyalty = vendorLoyalty;
            DatabaseKey(context);
        }

        internal void Stage()
        {
            context.ReservePendingItemIds(accountId, 0);
            var wallet = ReadWallet();
            if (wallet == null) { wallet = new() { Id = accountId, CurrencyId = currencyId }; context.AccountCurrency.Add(wallet); }
            else context.Attach(wallet);
            Capture(wallet);
            ulong balance = wallet.Amount;
            foreach (var operation in this.operations)
            {
                var prior = ReadOperation(operation.Key);
                if (prior != null)
                {
                    ValidateReceipt(prior, operation);
                    if (vendorLoyalty) LoyaltyRepository.ValidateVendorPayment(context, prior);
                    witnesses.Add((operation, prior.BeforeAmount, prior.AfterAmount));
                    // Re-observe already applied keys without replaying them.
                    // A later legitimate payment may have changed the wallet.
                    continue;
                }
                ulong after = Apply(balance, operation.Amount, operation.Subtract);
                var receipt = new AccountCurrencyOperationModel { OperationKey = operation.Key, AccountId = accountId,
                    CurrencyId = currencyId, Amount = operation.Amount, Subtract = operation.Subtract,
                    BeforeAmount = balance, AfterAmount = after, SourceJson = operation.SourceJson };
                context.AccountCurrencyOperation.Add(receipt); Capture(receipt); balance = after;
                if (vendorLoyalty) LoyaltyRepository.StageVendorPayment(context, receipt, Capture);
            }
            wallet.Amount = balance; Amount = balance;
            owned[0] = (wallet, context.Entry(wallet).CurrentValues.Clone());
        }

        internal static ulong Apply(ulong before, ulong amount, bool subtract)
            => subtract ? checked(before - amount) : checked(before + amount);

        private void Capture(object model) => owned.Add((model, context.Entry(model).CurrentValues.Clone()));
        private bool MySql => context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
        private AccountCurrencyModel ReadWallet() => (MySql
            ? context.AccountCurrency.FromSqlInterpolated($"SELECT * FROM account_currency WHERE id={accountId} AND currencyId={currencyId} FOR UPDATE")
            : context.AccountCurrency.Where(row => row.Id == accountId && row.CurrencyId == currencyId)).AsNoTracking().SingleOrDefault();
        private AccountCurrencyOperationModel ReadOperation(string key) => (MySql
            ? context.AccountCurrencyOperation.FromSqlInterpolated($"SELECT * FROM account_currency_operation WHERE operationKey={key} FOR UPDATE")
            : context.AccountCurrencyOperation.Where(row => row.OperationKey == key)).AsNoTracking().SingleOrDefault();
        internal void Detach()
        {
            bool detect = context.ChangeTracker.AutoDetectChangesEnabled;
            try
            {
                // Invalid original keys must reject the unit, but must not run
                // change detection again while its terminal owners are settling.
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                foreach (var row in owned) context.Entry(row.Model).State = EntityState.Detached;
            }
            finally { context.ChangeTracker.AutoDetectChangesEnabled = detect; }
        }

        internal static void ValidateReceipt(AccountCurrencyOperationModel row, AccountWalletOperation operation)
        {
            if (row.OperationKey != operation.Key || row.AccountId != operation.AccountId || row.CurrencyId != (byte)operation.Currency
                || row.Amount != operation.Amount || row.Subtract != operation.Subtract || row.SourceJson != operation.SourceJson
                || Apply(row.BeforeAmount, row.Amount, row.Subtract) != row.AfterAmount)
                throw new InvalidOperationException("Wallet operation identity or durable arithmetic changed.");
        }

        internal bool CanCommit()
        {
            foreach (var row in owned)
            {
                var entry = context.Entry(row.Model);
                if (entry.State is EntityState.Detached or EntityState.Deleted
                    || row.Values.Properties.Any(property => !Equals(entry.CurrentValues[property.Name], row.Values[property.Name]))) return Reject();
                if (row.Model is AccountCurrencyModel && (entry.OriginalValues.GetValue<uint>(nameof(AccountCurrencyModel.Id)) != accountId
                    || entry.OriginalValues.GetValue<byte>(nameof(AccountCurrencyModel.CurrencyId)) != currencyId)) return Reject();
                if (row.Model is AccountCurrencyOperationModel receipt
                    && entry.OriginalValues.GetValue<string>(nameof(AccountCurrencyOperationModel.OperationKey)) != receipt.OperationKey) return Reject();
                object stored = row.Model switch
                {
                    AccountCurrencyModel => ReadWallet(),
                    AccountCurrencyOperationModel operation => ReadOperation(operation.OperationKey),
                    AccountLoyaltyModel => LoyaltyRepository.ReadLedger(context, accountId),
                    AccountCosmicOperationModel operation => LoyaltyRepository.ReadOperation(context, accountId, operation.OperationKey),
                    _ => null
                };
                if (stored == null || row.Values.Properties.Any(property => !Equals(context.Entry(stored).CurrentValues[property.Name], row.Values[property.Name]))) return Reject();
            }
            // Replayed receipts are witnesses as well, even when no entity was
            // added for them in this unit.
            foreach (var operation in operations)
            {
                var stored = ReadOperation(operation.Key);
                if (stored == null) return Reject();
                ValidateReceipt(stored, operation);
                if (vendorLoyalty) LoyaltyRepository.ValidateVendorPayment(context, stored);
                var witness = witnesses.SingleOrDefault(row => row.Operation.Key == operation.Key);
                if (witness.Operation != null && (stored.BeforeAmount != witness.Before || stored.AfterAmount != witness.After)) return Reject();
            }
            return true;
        }

        private bool Reject() { context.RejectSaveUnit(); return false; }
    }
}
