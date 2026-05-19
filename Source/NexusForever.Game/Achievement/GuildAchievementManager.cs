using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Achievement
{
    public sealed class GuildAchievementManager : BaseAchievementManager<GuildAchievementModel>, IGuildAchievementManager
    {
        private readonly IGuild guild;
        protected override ulong OwnerId => guild.Id;

        /// <summary>
        /// Create a new <see cref="IGuildAchievementManager"/> from existing <see cref="GuildModel"/> database model.
        /// </summary>
        public GuildAchievementManager(IGuild guild, GuildModel model)
        {
            this.guild = guild;
            Initialise(model.Achievement, false);
        }

        /// <summary>
        /// Create a new <see cref="IGuildAchievementManager"/> for <see cref="IGuild"/>.
        /// </summary>
        public GuildAchievementManager(IGuild guild)
        {
            this.guild = guild;
        }

        protected override void SendAchievementUpdate(IEnumerable<IAchievement> updates)
        {
            guild.Broadcast(BuildAchievementUpdate(updates));
        }

        protected override void CompleteAchievement(IAchievement achievement)
        {
            base.CompleteAchievement(achievement);

            if (GlobalAchievementManager.Instance.TryClaimRealmFirstAchievement(achievement.Info, true))
                BroadcastRealmFirstAchievement(achievement, true, guild.Name);
        }

        protected override void CompleteAchievement(IPlayer target, IAchievement achievement)
        {
            base.CompleteAchievement(target, achievement);

            if (target != null)
                target.AchievementManager.CheckAchievements(target, AchievementType.AchievementComplete, achievement.Id);
        }

        /// <summary>
        /// Update or complete player achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0, uint count = 1)
        {
            CheckAchievements(target, GlobalAchievementManager.Instance.GetGuildAchievements(type), objectId, objectIdAlt, count);
        }

        /// <summary>
        /// Set current progress for guild achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        public override void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
        {
            SetAchievementProgress(target, GlobalAchievementManager.Instance.GetGuildAchievements(type), objectId, objectIdAlt, value);
        }
    }
}
