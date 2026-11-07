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

        /// <summary>
        /// The evaluator's shared boolean tail (<c>LAB_1404a22ec</c> and the same logic inlined in case 0x1c) used by types 28, 39, 86, 87 and others: comparison 1 returns the flag, comparison 2 its negation
        /// and every other comparison is false. The slot's Value is not read.
        /// </summary>
        public static bool CompareFlag(PrerequisiteComparison comparison, bool flag)
            => comparison switch
            {
                PrerequisiteComparison.Equal    => flag,
                PrerequisiteComparison.NotEqual => !flag,
                _                               => false
            };

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
