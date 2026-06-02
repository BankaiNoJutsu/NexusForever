using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Prerequisite
{
    internal static class PositionalPrerequisiteHelper
    {
        public static bool MeetsDirectionalRequirement(
            IPlayer player,
            PrerequisiteComparison comparison,
            IUnitEntity facingEntity,
            PositionalRequirementEntry entry)
        {
            return MeetsDirectionalRequirement(comparison, facingEntity, player, entry);
        }

        public static bool MeetsDirectionalRequirement(
            PrerequisiteComparison comparison,
            IUnitEntity facingEntity,
            IUnitEntity checkedEntity,
            PositionalRequirementEntry entry)
        {
            if (checkedEntity == null || facingEntity == null || entry == null)
                return false;

            float x = MathF.Cos(-facingEntity.Rotation.X);
            float z = MathF.Sin(-facingEntity.Rotation.X);
            Vector3 forward = new(x, 0f, z);

            Vector3 direction = Vector3.Normalize(checkedEntity.Position - facingEntity.Position);
            float dot = Vector3.Dot(direction, forward);
            float cross = Vector3.Cross(direction, forward).Y;

            float angle = MathF.Atan2(cross, dot);
            angle += MathF.PI / 2f;
            if (angle < 0f)
                angle += MathF.PI * 2f;

            angle = angle.ToDegrees();

            float minBounds = entry.AngleCenter - entry.AngleRange / 2f;
            float maxBounds = entry.AngleCenter + entry.AngleRange / 2f;
            bool isAllowed = angle >= minBounds && angle <= maxBounds;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => isAllowed,
                PrerequisiteComparison.NotEqual => !isAllowed,
                _                               => false
            };
        }
    }
}
