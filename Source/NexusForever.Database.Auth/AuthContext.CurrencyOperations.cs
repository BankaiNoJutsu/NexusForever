using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        public DbSet<AccountCurrencyOperationModel> AccountCurrencyOperation { get; set; }

        private static void ConfigureCurrencyOperations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountCurrencyOperationModel>(entity =>
            {
                entity.ToTable("account_currency_operation");
                entity.HasKey(row => row.OperationKey).HasName("PRIMARY");
                entity.Property(row => row.OperationKey).HasColumnName("operationKey").HasColumnType("varchar(128)").HasMaxLength(128).IsRequired();
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.CurrencyId).HasColumnName("currencyId").HasColumnType("tinyint(3) unsigned");
                entity.Property(row => row.Amount).HasColumnName("amount").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Subtract).HasColumnName("subtract").HasColumnType("tinyint(1)");
                entity.Property(row => row.BeforeAmount).HasColumnName("beforeAmount").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.AfterAmount).HasColumnName("afterAmount").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.SourceJson).HasColumnName("sourceJson").HasColumnType("longtext").IsRequired();
                entity.HasIndex(row => new { row.AccountId, row.CurrencyId }).HasDatabaseName("IX_currency_operation_owner");
            });
        }
    }
}
