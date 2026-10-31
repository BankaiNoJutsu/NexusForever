using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite;

internal static class PrerequisitePercentCompare
{
    // Build 16042 uses single-precision division and the ordinary float helper.
    // In particular, 0/0 is NaN rather than an invented zero-percent capacity.
    internal static bool Compare(PrerequisiteComparison comparison, float current, float capacity, uint expected)
    {
        float percent = current * 100f / capacity;
        return comparison switch
        {
            PrerequisiteComparison.Equal => percent == expected,
            PrerequisiteComparison.NotEqual => percent != expected,
            PrerequisiteComparison.GreaterThan => percent > expected,
            PrerequisiteComparison.GreaterThanOrEqual => percent >= expected,
            PrerequisiteComparison.LessThan => percent < expected,
            PrerequisiteComparison.LessThanOrEqual => percent <= expected,
            _ => false
        };
    }
}
