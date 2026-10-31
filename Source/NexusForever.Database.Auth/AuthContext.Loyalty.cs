using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        public DbSet<AccountLoyaltyModel> AccountLoyalty { get; set; }
        public DbSet<AccountLoyaltySlotModel> AccountLoyaltySlot { get; set; }
        public DbSet<AccountCosmicOperationModel> AccountCosmicOperation { get; set; }
        public DbSet<AccountInventoryReceiptModel> AccountInventoryReceipt { get; set; }

        private static void ConfigureLoyalty(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountInventoryReceiptModel>(entity =>
            {
                entity.ToTable("account_inventory_receipt");
                entity.HasKey(row => new { row.AccountId, row.DeliveryKey }).HasName("PRIMARY");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.DeliveryKey).HasColumnName("deliveryKey").HasColumnType("varchar(64)").IsRequired();
                entity.Property(row => row.InventoryId).HasColumnName("inventoryId").HasColumnType("bigint(20) unsigned");
                entity.HasOne(row => row.Account).WithMany().HasForeignKey(row => row.AccountId)
                    .HasConstraintName("FK__account_inventory_receipt_accountId__account_id");
            });
            modelBuilder.Entity<AccountLoyaltyModel>(entity =>
            {
                entity.ToTable("account_loyalty");
                entity.HasKey(row => row.AccountId).HasName("PRIMARY");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.EarnedPoints).HasColumnName("earnedPoints").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.BootstrapWalletAmount).HasColumnName("bootstrapWalletAmount").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.ActiveTrackId).HasColumnName("activeTrackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Revision).HasColumnName("revision").HasColumnType("bigint(20) unsigned");
                entity.HasOne(row => row.Account).WithOne(account => account.Loyalty).HasForeignKey<AccountLoyaltyModel>(row => row.AccountId)
                    .HasConstraintName("FK__account_loyalty_accountId__account_id");
            });
            modelBuilder.Entity<AccountLoyaltySlotModel>(entity =>
            {
                entity.ToTable("account_loyalty_slot");
                entity.HasKey(row => new { row.AccountId, row.TrackId, row.Position }).HasName("PRIMARY");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Position).HasColumnName("position").HasColumnType("tinyint(3) unsigned");
                entity.Property(row => row.RewardRowId).HasColumnName("rewardRowId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Claimed).HasColumnName("claimed").HasDefaultValue(false);
                entity.Property(row => row.Choice).HasColumnName("choice").HasColumnType("int(10) unsigned");
                entity.Property(row => row.DeliveryKey).HasColumnName("deliveryKey").HasColumnType("varchar(128)");
                entity.HasOne(row => row.Account).WithMany(account => account.LoyaltySlots).HasForeignKey(row => row.AccountId)
                    .HasConstraintName("FK__account_loyalty_slot_accountId__account_id");
            });
            modelBuilder.Entity<AccountCosmicOperationModel>(entity =>
            {
                entity.ToTable("account_cosmic_operation");
                entity.HasKey(row => new { row.AccountId, row.OperationKey }).HasName("PRIMARY");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.OperationKey).HasColumnName("operationKey").HasColumnType("varchar(64)").IsRequired();
                entity.Property(row => row.Amount).HasColumnName("amount").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Subtract).HasColumnName("subtract");
                entity.Property(row => row.Earned).HasColumnName("earned");
                entity.HasOne(row => row.Account).WithMany().HasForeignKey(row => row.AccountId)
                    .HasConstraintName("FK__account_cosmic_operation_accountId__account_id");
            });
            modelBuilder.Entity<AccountPendingItemModel>().Property(row => row.ForceSoulbind).HasColumnName("forceSoulbind").HasDefaultValue(false);
            modelBuilder.Entity<AccountInventoryModel>().Property(row => row.ForceSoulbind).HasColumnName("forceSoulbind").HasDefaultValue(false);
            modelBuilder.Entity<AccountInventoryModel>().Property(row => row.CreationKey).HasColumnName("creationKey").HasColumnType("varchar(64)");
        }
    }
}
