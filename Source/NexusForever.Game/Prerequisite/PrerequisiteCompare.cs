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

        /// <summary>
        /// Managed IEEE comparisons retained for callers whose native instruction-level
        /// correspondence is separate from the type-40 read. A NaN operand only satisfies !=.
        /// </summary>
        public static bool CompareFloat(PrerequisiteComparison comparison, float actual, float expected)
            => comparison switch
            {
                PrerequisiteComparison.Equal              => actual == expected,
                PrerequisiteComparison.NotEqual           => actual != expected,
                PrerequisiteComparison.GreaterThan        => expected < actual,
                PrerequisiteComparison.GreaterThanOrEqual => expected <= actual,
                PrerequisiteComparison.LessThan           => actual < expected,
                PrerequisiteComparison.LessThanOrEqual    => actual <= expected,
                _                                         => false
            };

        /// <summary>
        /// Verified native helper <c>1404a2010</c>: UCOMISS/JNE and UCOMISS/JE
        /// use ZF without rejecting unordered operands. With masked FP exceptions,
        /// NaN therefore satisfies Equal only. Type 40's direct call is mapped in
        /// Decomp/Analysis/PREREQUISITE_40_PRIMARY_SUBJECT_20261114.md.
        /// </summary>
        public static bool CompareNativeFloat(PrerequisiteComparison comparison, float actual, float expected)
        {
            if (float.IsNaN(actual) || float.IsNaN(expected))
                return comparison == PrerequisiteComparison.Equal;

            return CompareFloat(comparison, actual, expected);
        }

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
