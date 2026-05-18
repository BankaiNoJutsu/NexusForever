using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Achievement
{
    public static class CostumeAchievementUpdater
    {
        public static void Update(IPlayer player, uint itemId)
        {
            if (player == null || itemId == 0u)
                return;

            player.AchievementManager.CheckAchievements(player, AchievementType.CostumeUnlock, itemId);
            player.AchievementManager.CheckAchievements(player, AchievementType.CostumeSetUnlock, itemId);
        }
    }
}
