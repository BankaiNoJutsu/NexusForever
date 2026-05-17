using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Achievement
{
    public sealed class GlobalAchievementManager : Singleton<GlobalAchievementManager>, IGlobalAchievementManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<ushort, IAchievementInfo> achievements = new();
        private readonly Dictionary<AchievementType, List<IAchievementInfo>> characterAchievements = new();
        private readonly Dictionary<AchievementType, List<IAchievementInfo>> guildAchievements = new();
        private readonly HashSet<ushort> completedCharacterRealmFirstAchievements = [];
        private readonly HashSet<ushort> completedGuildRealmFirstAchievements = [];

        private readonly object realmFirstLock = new();

        public void Initialise()
        {
            DateTime start = DateTime.UtcNow;

            foreach (AchievementEntry entry in GameTableManager.Instance.Achievement.Entries)
            {
                var info = new AchievementInfo(entry);
                achievements.Add((ushort)entry.Id, info);

                AchievementType type = (AchievementType)entry.AchievementTypeId;
                Dictionary<AchievementType, List<IAchievementInfo>> collection = info.IsPlayerAchievement ? characterAchievements : guildAchievements;
                if (!collection.ContainsKey(type))
                    collection.Add(type, new List<IAchievementInfo>());

                collection[type].Add(info);
            }

            LoadCompletedRealmFirstAchievements();

            TimeSpan span = DateTime.UtcNow - start;
            log.Info($"Initialised {achievements.Count} achievements in {span.TotalMilliseconds}ms.");
        }

        private void LoadCompletedRealmFirstAchievements()
        {
            CharacterDatabase database = DatabaseManager.Instance.GetDatabase<CharacterDatabase>();

            completedCharacterRealmFirstAchievements.UnionWith(database.GetCompletedCharacterAchievementIds()
                .Where(IsRealmFirstAchievement));
            completedGuildRealmFirstAchievements.UnionWith(database.GetCompletedGuildAchievementIds()
                .Where(IsRealmFirstAchievement));
        }

        private bool IsRealmFirstAchievement(ushort achievementId)
        {
            return achievements.TryGetValue(achievementId, out IAchievementInfo info) && info.IsRealmFirst;
        }

        /// <summary>
        /// Return <see cref="IAchievementInfo"/> for supplied achievement id.
        /// </summary>
        public IAchievementInfo GetAchievement(ushort id)
        {
            return achievements.TryGetValue(id, out IAchievementInfo info) ? info : null;
        }

        /// <summary>
        /// Return all <see cref="IAchievementInfo"/>'s of <see cref="AchievementType"/> that can be completed by a player.
        /// </summary>
        public IEnumerable<IAchievementInfo> GetCharacterAchievements(AchievementType type)
        {
            if (!characterAchievements.TryGetValue(type, out List<IAchievementInfo> achievements))
                return Enumerable.Empty<IAchievementInfo>();

            return achievements;
        }

        /// <summary>
        /// Return all <see cref="AchievementInfo"/>'s of <see cref="AchievementType"/> that can be completed by a guild.
        /// </summary>
        public IEnumerable<IAchievementInfo> GetGuildAchievements(AchievementType type)
        {
            if (!guildAchievements.TryGetValue(type, out List<IAchievementInfo> achievements))
                return Enumerable.Empty<IAchievementInfo>();

            return achievements;
        }

        public bool TryClaimRealmFirstAchievement(IAchievementInfo info, bool isGuildAchievement)
        {
            if (!info.IsRealmFirst)
                return false;

            lock (realmFirstLock)
            {
                HashSet<ushort> completedAchievements = isGuildAchievement
                    ? completedGuildRealmFirstAchievements
                    : completedCharacterRealmFirstAchievements;

                return completedAchievements.Add(info.Id);
            }
        }
    }
}
