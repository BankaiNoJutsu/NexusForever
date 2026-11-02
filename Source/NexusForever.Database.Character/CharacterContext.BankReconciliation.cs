using System;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        internal Func<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry, bool> BankRowOwner { get; set; }
        public DbSet<BankReconciliationReceiptModel> BankReconciliationReceipt { get; set; }
        private static void ConfigureBankReconciliation(ModelBuilder builder)
        {
            builder.Entity<BankReconciliationReceiptModel>(entity =>
            {
                entity.ToTable("bank_reconciliation_receipt");
                entity.HasKey(row => row.Id).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("char(32)").HasMaxLength(32).IsRequired().ValueGeneratedNever();
                entity.Property(row => row.Payload).HasColumnName("payload").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
            });
        }
    }
}
