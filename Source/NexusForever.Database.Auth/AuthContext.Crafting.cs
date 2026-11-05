using Microsoft.EntityFrameworkCore;
using System.Linq;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        public DbSet<AccountCraftingReceiptModel> AccountCraftingReceipt { get; set; }

        // Uses the same account-before-item lock order as every existing item
        // save. Receipts keep consumed IDs out of circulation after upgrades.
        public ulong ReserveInventoryIds(uint accountId, ulong count)
        {
            if (accountId == 0 || Database.CurrentTransaction == null)
                throw new System.InvalidOperationException("Account inventory allocation requires its owned transaction.");
            if (pendingSaveAccount.HasValue && pendingSaveAccount != accountId)
            {
                RejectSaveUnit();
                throw new System.InvalidOperationException("Account inventory allocation changed its save owner.");
            }
            ReservePendingItemIds(accountId, 0);
            var accounts = IsMySqlPendingProvider
                ? Account.FromSqlInterpolated($"SELECT * FROM account WHERE id={accountId} FOR UPDATE")
                : Account.Where(row => row.Id == accountId);
            var account = accounts.AsNoTracking().Single();
            var rows = IsMySqlPendingProvider
                ? AccountInventory.FromSqlInterpolated($"SELECT * FROM account_inventory WHERE id={accountId} FOR UPDATE")
                : AccountInventory.Where(row => row.Id == accountId);
            var receipts = IsMySqlPendingProvider
                ? AccountInventoryReceipt.FromSqlInterpolated($"SELECT * FROM account_inventory_receipt WHERE accountId={accountId} FOR UPDATE")
                : AccountInventoryReceipt.Where(row => row.AccountId == accountId);
            ulong highest = rows.AsNoTracking().AsEnumerable().Select(row => row.InventoryId)
                .Concat(receipts.AsNoTracking().AsEnumerable().Select(row => row.InventoryId)).DefaultIfEmpty(0ul).Max();
            ulong next = System.Math.Max(account.NextInventoryId, checked(highest + 1ul));
            ulong following = checked(next + count);
            Database.ExecuteSqlInterpolated($"UPDATE account SET nextInventoryId={following} WHERE id={accountId}");
            commitGuards.Add(() => accounts.AsNoTracking().Select(row => row.NextInventoryId).Single() >= following);
            return next;
        }

        private static void ConfigureCraftingReceipts(ModelBuilder builder)
        {
            builder.Entity<Model.AccountModel>().Property(row => row.NextInventoryId)
                .HasColumnName("nextInventoryId").HasColumnType("bigint(20) unsigned").HasDefaultValue(1ul);
            builder.Entity<AccountCraftingReceiptModel>(entity =>
            {
                entity.ToTable("account_crafting_receipt");
                entity.HasKey(row => row.Id).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RealmId).HasColumnName("realmId").HasColumnType("smallint(5) unsigned");
                entity.Property(row => row.PayloadJson).HasColumnName("payloadJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.Delivered).HasColumnName("delivered").HasColumnType("tinyint(1)");
                entity.Property(row => row.Cancelled).HasColumnName("cancelled").HasColumnType("tinyint(1)");
                entity.Property(row => row.Completed).HasColumnName("completed").HasColumnType("tinyint(1)");
                entity.HasIndex(row => new { row.AccountId, row.RealmId, row.Completed }).HasDatabaseName("IX_crafting_receipt_account");
            });
        }
    }
}
