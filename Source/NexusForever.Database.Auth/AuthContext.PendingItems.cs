using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        /// <summary>
        /// Serialize pending-item changes with this account's durable allocator.
        /// The counter never moves backwards when rows are consumed. Existing rows
        /// bootstrap upgraded accounts without renumbering their inventory.
        /// </summary>
        public ulong ReservePendingItemIds(uint accountId, uint count)
        {
            if (Database.CurrentTransaction == null)
                throw new InvalidOperationException("Pending item allocation requires a transaction.");
            pendingSaveAccount ??= accountId;

            Database.ExecuteSqlInterpolated($"UPDATE account SET nextPendingItemId = nextPendingItemId WHERE id = {accountId}");
            IQueryable<AccountModel> accounts = IsMySqlPendingProvider
                ? Account.FromSqlInterpolated($"SELECT * FROM account WHERE id = {accountId} FOR UPDATE")
                : Account.Where(a => a.Id == accountId);
            ulong? counter = accounts.AsNoTracking().Select(a => (ulong?)a.NextPendingItemId).SingleOrDefault();
            if (!counter.HasValue)
                throw new InvalidOperationException($"Account {accountId} does not exist.");

            // Materialize only this account's IDs: SQLite cannot translate MAX(ulong).
            ulong highestId = LoadPendingItemRows(accountId).Select(p => p.PendingItemId).DefaultIfEmpty(0ul).Max();
            ulong next = Math.Max(counter.Value, checked(highestId + 1ul));
            if (next == 0ul)
                throw new InvalidOperationException("No pending account item IDs are available.");
            ulong following = checked(next + count);
            Database.ExecuteSqlInterpolated($"UPDATE account SET nextPendingItemId = {following} WHERE id = {accountId}");
            return next;
        }

        private bool IsMySqlPendingProvider => Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;

        public System.Collections.Generic.List<AccountPendingItemModel> LoadPendingItemRows(uint accountId)
        {
            IQueryable<AccountPendingItemModel> query = IsMySqlPendingProvider
                ? AccountPendingItem.FromSqlInterpolated($"SELECT * FROM account_pending_item WHERE id = {accountId} FOR UPDATE")
                : AccountPendingItem.Where(row => row.Id == accountId);
            return query.AsNoTracking().ToList();
        }

        /// <summary>
        /// Idempotently append a pending row in the current account save unit. The
        /// caller supplies a stable operation key across retries. An existing receipt
        /// with no pending row means the grant was already consumed, never a regrant.
        /// </summary>
        public (ulong Id, bool Available) StagePendingItemAppend(string deliveryKey, AccountPendingItemModel row)
        {
            ArgumentNullException.ThrowIfNull(row);
            if (string.IsNullOrWhiteSpace(deliveryKey) || deliveryKey.Length > 128)
                throw new ArgumentException("A stable pending delivery key of at most 128 characters is required.", nameof(deliveryKey));
            ReservePendingItemIds(row.Id, 0);
            IQueryable<AccountPendingItemDeliveryModel> receipts = IsMySqlPendingProvider
                ? AccountPendingItemDelivery.FromSqlInterpolated($"SELECT * FROM account_pending_item_delivery WHERE accountId = {row.Id} AND deliveryKey = {deliveryKey} FOR UPDATE")
                : AccountPendingItemDelivery.Where(receipt => receipt.AccountId == row.Id && receipt.DeliveryKey == deliveryKey);
            AccountPendingItemDeliveryModel existing = AccountPendingItemDelivery.Local.FirstOrDefault(receipt => receipt.AccountId == row.Id && receipt.DeliveryKey == deliveryKey)
                ?? receipts.AsNoTracking().SingleOrDefault();
            if (existing != null)
                return (existing.PendingItemId, AccountPendingItem.Local.Any(p => p.Id == row.Id && p.PendingItemId == existing.PendingItemId)
                    || LoadPendingItemRows(row.Id).Any(p => p.PendingItemId == existing.PendingItemId));

            row.PendingItemId = ReservePendingItemIds(row.Id, 1);
            AccountPendingItemDelivery.Add(new AccountPendingItemDeliveryModel { AccountId = row.Id, DeliveryKey = deliveryKey, PendingItemId = row.PendingItemId });
            AccountPendingItem.Add(row);
            return (row.PendingItemId, true);
        }
    }
}
