using System.Numerics;

namespace NexusForever.Game.Entity.Movement
{
    internal static class MovementMath
    {
        public static bool IsFinite(float value)
        {
            return float.IsFinite(value);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.X)
                && IsFinite(value.Y)
                && IsFinite(value.Z);
        }

        public static bool HasDirection(Vector3 value)
        {
            return IsFinite(value) && value.LengthSquared() > float.Epsilon;
        }

        public static Vector3 NormaliseOrZero(Vector3 value)
        {
            return HasDirection(value)
                ? Vector3.Normalize(value)
                : Vector3.Zero;
        }

        public static bool IsValidSpeed(float speed)
        {
            return IsFinite(speed) && speed > float.Epsilon;
        }
    }
}
