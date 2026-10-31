using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        public DbSet<AccountWorldSessionModel> AccountWorldSession { get; set; }

        private static void ConfigureWorldSessions(ModelBuilder builder)
        {
            builder.Entity<AccountWorldSessionModel>(entity =>
            {
                entity.ToTable("account_world_session");
                entity.HasKey(row => row.AccountId).HasName("PRIMARY");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.Epoch).HasColumnName("epoch").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.HasOne(row => row.Account).WithMany().HasForeignKey(row => row.AccountId)
                    .HasConstraintName("FK__account_world_session_accountId__account_id");
            });
        }
    }
}
