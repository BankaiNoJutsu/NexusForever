using System.Numerics;
using NexusForever.Game.Entity.Movement;

namespace NexusForever.Game.Entity.Movement.Key
{
    public class PositionKeys : Keys<Vector3>
    {
        /// <summary>
        /// Calculate the interpolcated <see cref="Vector3"/> position value at the current time.
        /// </summary>
        public override Vector3 CalculateInterpolated(Vector3 v1, Vector3 v2, float t)
        {
            return Vector3.Lerp(v1, v2, t);
        }

        /// <summary>
        /// Calculate the interpolated <see cref="Vector3"/> rotation value at the current time.
        /// </summary>
        public Vector3 GetRotation()
        {
            int index = Math.Min(GetIndex(), Values.Count - 2);
            if (index < 0)
                return Vector3.Zero;

            Vector3 vector = MovementMath.NormaliseOrZero(Values[index + 1] - Values[index]);
            if (vector == Vector3.Zero)
                return Vector3.Zero;

            float pitch = MathF.Asin(-vector.Y);
            float yaw = MathF.Atan2(-vector.X, -vector.Z);
            return new Vector3(yaw, pitch, 0f);
        }
    }
}
