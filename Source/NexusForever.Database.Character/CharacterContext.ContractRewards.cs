using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public DbSet<CharacterContractRewardTrackModel> CharacterContractRewardTrack { get; set; }
        public DbSet<CharacterContractCompletionModel> CharacterContractCompletion { get; set; }
        public DbSet<CharacterContractRewardSlotModel> CharacterContractRewardSlot { get; set; }
        public DbSet<CharacterContractRewardClaimModel> CharacterContractRewardClaim { get; set; }
        public DbSet<CharacterContractAccountOutboxModel> CharacterContractAccountOutbox { get; set; }

        private static void ConfigureContractRewards(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterContractRewardSlotModel>(entity =>
            {
                entity.ToTable("character_contract_reward_slot");
                entity.HasKey(row => new { row.Id, row.TrackId, row.CycleId, row.Ordinal }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.Ordinal).HasColumnName("ordinal").HasColumnType("tinyint(3) unsigned").ValueGeneratedNever();
                entity.Property(row => row.RewardRowId).HasColumnName("rewardRowId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RequiredPoints).HasColumnName("requiredPoints").HasColumnType("int(10) unsigned");
                entity.Property(row => row.SelectionJson).HasColumnName("selectionJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.Claimed).HasColumnName("claimed").HasColumnType("tinyint(1)").HasDefaultValue(false);
                entity.HasOne(row => row.Track).WithMany().HasForeignKey(row => new { row.Id, row.TrackId })
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_contract_reward_slot_track");
            });
            modelBuilder.Entity<CharacterContractRewardClaimModel>(entity =>
            {
                entity.ToTable("character_contract_reward_claim");
                entity.HasKey(row => new { row.Id, row.TrackId, row.CycleId, row.Ordinal }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.Ordinal).HasColumnName("ordinal").HasColumnType("tinyint(3) unsigned").ValueGeneratedNever();
                entity.Property(row => row.RewardRowId).HasColumnName("rewardRowId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Choice).HasColumnName("choice").HasColumnType("int(10) unsigned");
                entity.Property(row => row.ClaimId).HasColumnName("claimId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.HasIndex(row => row.ClaimId).IsUnique().HasDatabaseName("IX_contract_reward_claim_id");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.QuoteVersion).HasColumnName("quoteVersion").HasColumnType("int(10) unsigned");
                entity.Property(row => row.QuoteJson).HasColumnName("quoteJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.DeliveryVersion).HasColumnName("deliveryVersion").HasColumnType("int(10) unsigned");
                entity.Property(row => row.DeliveryJson).HasColumnName("deliveryJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.CharacterCurrencyJson).HasColumnName("characterCurrencyJson").HasColumnType("longtext");
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
                // History is Character-owned and survives track/cycle removal.
                entity.HasOne(row => row.Character).WithMany().HasForeignKey(row => row.Id)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_contract_reward_claim_character");
            });
            modelBuilder.Entity<CharacterContractAccountOutboxModel>(entity =>
            {
                entity.ToTable("character_contract_account_outbox");
                entity.HasKey(row => row.ClaimId).HasName("PRIMARY");
                entity.Property(row => row.ClaimId).HasColumnName("claimId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.CharacterId).HasColumnName("characterId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.AccountId).HasColumnName("accountId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RealmId).HasColumnName("realmId").HasColumnType("smallint(5) unsigned");
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Ordinal).HasColumnName("ordinal").HasColumnType("tinyint(3) unsigned");
                entity.Property(row => row.PayloadJson).HasColumnName("payloadJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.Delivered).HasColumnName("delivered").HasColumnType("tinyint(1)");
                entity.HasIndex(row => new { row.CharacterId, row.ClaimId }).HasDatabaseName("IX_contract_outbox_character");
            });
            modelBuilder.Entity<CharacterContractRewardTrackModel>(entity =>
            {
                entity.ToTable("character_contract_reward_track");
                entity.HasKey(row => new { row.Id, row.TrackId }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Points).HasColumnName("points").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Revision).HasColumnName("revision").HasColumnType("bigint(20) unsigned").IsConcurrencyToken();
                entity.Property(row => row.Active).HasColumnName("active").HasColumnType("tinyint(1)");
                entity.Property(row => row.SelectionInitialized).HasColumnName("selectionInitialized").HasColumnType("tinyint(1)").HasDefaultValue(false);
                entity.HasOne(row => row.Character).WithMany().HasForeignKey(row => row.Id)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_contract_reward_track_character");
            });
            modelBuilder.Entity<CharacterContractCompletionModel>(entity =>
            {
                entity.ToTable("character_contract_completion");
                entity.HasKey(row => new { row.Id, row.AcceptanceId }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.AcceptanceId).HasColumnName("acceptanceId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.QuestId).HasColumnName("questId").HasColumnType("smallint(5) unsigned");
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.BasePoints).HasColumnName("basePoints").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Points).HasColumnName("points").HasColumnType("int(10) unsigned");
                entity.Property(row => row.PayloadVersion).HasColumnName("payloadVersion").HasColumnType("int(10) unsigned");
                entity.Property(row => row.PayloadJson).HasColumnName("payloadJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
                // Receipt history survives Quest removal/reacceptance and track reset.
                entity.HasOne(row => row.Character).WithMany().HasForeignKey(row => row.Id)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_contract_completion_character");
            });
        }
    }
}
