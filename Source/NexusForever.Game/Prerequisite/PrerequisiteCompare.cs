using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    internal static class PrerequisiteCompare
    {
        /// <summary>
        /// Native CVTTSS2SI to int64 followed by a uint32 comparison. Invalid
        /// conversion produces int64.MinValue, whose low32 bits are zero.
        /// </summary>
        public static uint TruncateNativeScalar(float value)
            => value >= -9223372036854775808f && value < 9223372036854775808f
                ? unchecked((uint)(long)value)
                : 0u;

        public static bool Compare(PrerequisiteComparison comparison, uint current, uint expected)
        {
            return comparison switch
            {
                PrerequisiteComparison.Equal              => current == expected,
                PrerequisiteComparison.NotEqual           => current != expected,
                PrerequisiteComparison.GreaterThanOrEqual => current >= expected,
                PrerequisiteComparison.GreaterThan        => current > expected,
                PrerequisiteComparison.LessThanOrEqual    => current <= expected,
                PrerequisiteComparison.LessThan           => current < expected,
                _                                         => false
            };
        }
    }
}
