using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public DbSet<CharacterChallengeAttemptModel> CharacterChallengeAttempt { get; set; }
        public DbSet<CharacterChallengeRewardTrackModel> CharacterChallengeRewardTrack { get; set; }
        public DbSet<CharacterChallengeRewardSlotModel> CharacterChallengeRewardSlot { get; set; }
        public DbSet<CharacterChallengeMedalReceiptModel> CharacterChallengeMedalReceipt { get; set; }
        public DbSet<CharacterChallengeRewardClaimModel> CharacterChallengeRewardClaim { get; set; }

        private static void ConfigureRewardTracks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterChallengeModel>().Property(row => row.ActiveAttemptId)
                .HasColumnName("activeAttemptId").HasColumnType("char(32)").HasMaxLength(32).IsRequired(false);
            modelBuilder.Entity<CharacterChallengeModel>().HasIndex(row => new { row.Id, row.ActiveAttemptId })
                .HasDatabaseName("IX_character_challenge_activeAttempt");

            modelBuilder.Entity<CharacterChallengeAttemptModel>(entity =>
            {
                entity.ToTable("character_challenge_attempt");
                entity.HasKey(row => new { row.Id, row.AttemptId }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.AttemptId).HasColumnName("attemptId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.ChallengeId).HasColumnName("challengeId").HasColumnType("smallint(5) unsigned");
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RewardMode).HasColumnName("rewardMode").HasColumnType("tinyint(3) unsigned");
                entity.Property(row => row.EarnedMedalMask).HasColumnName("earnedMedalMask").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Terminal).HasColumnName("terminal").HasColumnType("tinyint(1)");
                entity.HasOne(row => row.Character).WithMany().HasForeignKey(row => row.Id)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_challenge_attempt_character");
            });

            modelBuilder.Entity<CharacterChallengeRewardTrackModel>(entity =>
            {
                entity.ToTable("character_challenge_reward_track");
                entity.HasKey(row => new { row.Id, row.TrackId }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Points).HasColumnName("points").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Revision).HasColumnName("revision").HasColumnType("bigint(20) unsigned").IsConcurrencyToken();
                entity.Property(row => row.Active).HasColumnName("active").HasColumnType("tinyint(1)");
                entity.HasOne(row => row.Character).WithMany().HasForeignKey(row => row.Id)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_challenge_reward_track_character");
            });

            modelBuilder.Entity<CharacterChallengeRewardSlotModel>(entity =>
            {
                entity.ToTable("character_challenge_reward_slot");
                entity.HasKey(row => new { row.Id, row.TrackId, row.CycleId, row.Ordinal }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.Ordinal).HasColumnName("ordinal").HasColumnType("tinyint(3) unsigned").ValueGeneratedNever();
                entity.Property(row => row.RewardRowId).HasColumnName("rewardRowId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.RequiredPoints).HasColumnName("requiredPoints").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Claimed).HasColumnName("claimed").HasColumnType("tinyint(1)");
                entity.HasOne(row => row.Track).WithMany().HasForeignKey(row => new { row.Id, row.TrackId })
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_challenge_reward_slot_track");
            });

            modelBuilder.Entity<CharacterChallengeMedalReceiptModel>(entity =>
            {
                entity.ToTable("character_challenge_medal_receipt");
                entity.HasKey(row => new { row.Id, row.AttemptId, row.Medal }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.AttemptId).HasColumnName("attemptId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.Medal).HasColumnName("medal").HasColumnType("tinyint(3) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Points).HasColumnName("points").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Multiplier).HasColumnName("multiplier").HasColumnType("float");
                entity.Property(row => row.ModifierRevision).HasColumnName("modifierRevision").HasColumnType("bigint(20)");
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
                entity.HasOne(row => row.Attempt).WithMany().HasForeignKey(row => new { row.Id, row.AttemptId })
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_challenge_medal_receipt_attempt");
            });

            modelBuilder.Entity<CharacterChallengeRewardClaimModel>(entity =>
            {
                entity.ToTable("character_challenge_reward_claim");
                entity.HasKey(row => new { row.Id, row.TrackId, row.CycleId, row.Ordinal }).HasName("PRIMARY");
                entity.Property(row => row.Id).HasColumnName("id").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.TrackId).HasColumnName("trackId").HasColumnType("int(10) unsigned").ValueGeneratedNever();
                entity.Property(row => row.CycleId).HasColumnName("cycleId").HasColumnType("bigint(20) unsigned").ValueGeneratedNever();
                entity.Property(row => row.Ordinal).HasColumnName("ordinal").HasColumnType("tinyint(3) unsigned").ValueGeneratedNever();
                entity.Property(row => row.RewardRowId).HasColumnName("rewardRowId").HasColumnType("int(10) unsigned");
                entity.Property(row => row.Choice).HasColumnName("choice").HasColumnType("int(10) unsigned");
                entity.Property(row => row.ClaimId).HasColumnName("claimId").HasColumnType("char(32)").HasMaxLength(32).IsRequired();
                entity.Property(row => row.DeliveryVersion).HasColumnName("deliveryVersion").HasColumnType("int(10) unsigned");
                entity.Property(row => row.DeliveryJson).HasColumnName("deliveryJson").HasColumnType("longtext").IsRequired();
                entity.Property(row => row.CreatedUtc).HasColumnName("createdUtc").HasColumnType("datetime(6)");
                entity.HasIndex(row => row.ClaimId).IsUnique().HasDatabaseName("UX_character_challenge_reward_claim_claimId");
                entity.HasOne(row => row.Slot).WithMany().HasForeignKey(row => new { row.Id, row.TrackId, row.CycleId, row.Ordinal })
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_challenge_reward_claim_slot");
            });
        }
    }
}
