using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class ActivationAchievementUpdater
    {
        public static void Update(IPlayer player, IWorldEntity entity)
        {
            if (player == null || entity == null)
                return;

            player.AchievementManager.CheckAchievements(player, AchievementType.DiscoverObject, entity.CreatureId);
            player.AchievementManager.CheckAchievements(player, AchievementType.FindSecretStash, entity.CreatureId);
        }
    }
}
