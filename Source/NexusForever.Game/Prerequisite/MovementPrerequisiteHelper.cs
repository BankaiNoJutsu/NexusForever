using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Prerequisite
{
    internal static class MovementPrerequisiteHelper
    {
        public static IWorldEntity GetPlatformEntity(IPlayer player)
        {
            if (player.PlatformGuid == null)
                return null;

            return player.GetVisible<IWorldEntity>(player.PlatformGuid.Value);
        }

        public static uint GetPlatformEntityType(IPlayer player)
        {
            IWorldEntity platform = GetPlatformEntity(player);
            return platform == null ? 0u : (uint)platform.Type;
        }

        public static uint GetMountedScalar(IPlayer player) =>
            GetPlatformEntity(player) != null ? 1u : 0u;
    }
}
