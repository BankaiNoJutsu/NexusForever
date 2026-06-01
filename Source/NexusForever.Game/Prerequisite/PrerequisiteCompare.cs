using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    internal static class PrerequisiteCompare
    {
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
