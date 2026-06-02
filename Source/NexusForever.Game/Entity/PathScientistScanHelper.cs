using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    internal static class PathScientistScanHelper
    {
        /// <summary>
        /// Bitmask with one bit per <see cref="PathScientistCreatureInfoEntry.ChecklistCount"/> step.
        /// </summary>
        public static uint GetCompletionMask(PathScientistCreatureInfoEntry entry)
        {
            uint count = entry?.ChecklistCount ?? 0u;
            if (count == 0u)
                return 1u;

            if (count >= 32u)
                return uint.MaxValue;

            return (1u << (int)count) - 1u;
        }

        public static bool IsChecklistBitSet(uint progress, uint checklistIndex)
        {
            if (checklistIndex >= 32u)
                return false;

            return (progress & (1u << (int)checklistIndex)) != 0u;
        }

        public static bool IsFullyScanned(uint progress, PathScientistCreatureInfoEntry entry)
        {
            uint mask = GetCompletionMask(entry);
            return (progress & mask) == mask;
        }
    }
}
