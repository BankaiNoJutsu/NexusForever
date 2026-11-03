using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public DbSet<RealmFirstAchievementModel> RealmFirstAchievement { get; set; }

        private static void ConfigureRealmFirst(ModelBuilder builder)
        {
            builder.Entity<RealmFirstAchievementModel>(entity =>
            {
                entity.ToTable("realm_first_achievement");
                entity.HasKey(row => new { row.IsGuild, row.AchievementId }).HasName("PRIMARY");
                entity.Property(row => row.IsGuild).HasColumnName("isGuild").HasColumnType("tinyint(1)").ValueGeneratedNever();
                entity.Property(row => row.AchievementId).HasColumnName("achievementId").HasColumnType("smallint(5) unsigned").ValueGeneratedNever();
                entity.Property(row => row.OwnerId).HasColumnName("ownerId").HasColumnType("bigint(20) unsigned");
                entity.Property(row => row.Token).HasColumnName("token").HasColumnType("char(32)").HasMaxLength(32).IsRequired(false);
                entity.Property(row => row.Name).HasColumnName("name").HasColumnType("longtext").IsRequired(false);
            });
        }
    }
}
