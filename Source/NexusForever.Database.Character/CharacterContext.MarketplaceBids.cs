using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public DbSet<MarketplaceBidReceiptModel> MarketplaceBidReceipt { get; set; }

        private static void ConfigureMarketplaceBids(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketplaceBidReceiptModel>(entity =>
            {
                entity.ToTable("marketplace_bid_receipt");
                entity.HasKey(row => row.Id).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("char(32)").HasMaxLength(32).IsRequired().ValueGeneratedNever();
                entity.Property(row => row.Payload).HasColumnName("payload").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
            });
        }
    }
}
