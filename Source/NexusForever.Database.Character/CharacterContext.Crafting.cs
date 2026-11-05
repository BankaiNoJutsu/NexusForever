using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public DbSet<CharacterCraftingAccountOutboxModel> CharacterCraftingAccountOutbox { get; set; }

        private static void ConfigureCraftingAccounts(ModelBuilder builder)
        {
            builder.Entity<CharacterCraftingAccountOutboxModel>(entity =>
            {
                entity.ToTable("character_crafting_account_outbox");
                entity.HasKey(row => row.Id).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.CharacterId).HasColumnName("characterId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RealmId).HasColumnName("realmId").HasColumnType("smallint(5) unsigned");
                entity.Property(row => row.PayloadJson).HasColumnName("payloadJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.Delivered).HasColumnName("delivered").HasColumnType("tinyint(1)");
                entity.Property(row => row.Cancelled).HasColumnName("cancelled").HasColumnType("tinyint(1)");
                entity.HasIndex(row => new { row.AccountId, row.RealmId, row.Delivered }).HasDatabaseName("IX_crafting_outbox_account");
            });
        }
    }
}
