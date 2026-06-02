using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class WorldLocationPrerequisiteHelper
    {
        public static bool IsPlayerInside(IPlayer player, uint worldLocation2Id, IGameTableManager gameTableManager, float horizontalPadding = 0f)
        {
            if (player == null || worldLocation2Id == 0u)
                return false;

            WorldLocation2Entry entry = gameTableManager.WorldLocation2.GetEntry(worldLocation2Id);
            return entry != null && IsInside(player.Position, entry, horizontalPadding);
        }

        public static bool IsInside(Vector3 position, WorldLocation2Entry worldLocation, float horizontalPadding = 0f)
        {
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            float horizontalRange = worldLocation.Radius + horizontalPadding;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return worldLocation.MaxVerticalDistance <= 0f
                || MathF.Abs(position.Y - worldLocation.Position1) <= worldLocation.MaxVerticalDistance;
        }
    }
}
