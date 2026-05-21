using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Map
{
    internal static class ZoneCompletionRewardResolver
    {
        public static IReadOnlyList<ushort> GetExplorationOnlyTitleRewards(uint mapZoneId)
        {
            ZoneCompletionEntry[] entries = GameTableManager.Instance.ZoneCompletion?.Entries;
            if (entries == null)
                return [];

            ushort[] titleIds = entries
                .Where(e => e.MapZoneId == mapZoneId)
                .Where(IsExplorationOnly)
                .Where(e => e.CharacterTitleIdReward is > 0u and <= ushort.MaxValue)
                .Select(e => (ushort)e.CharacterTitleIdReward)
                .Distinct()
                .ToArray();

            return titleIds.Length == 1 ? titleIds : [];
        }

        private static bool IsExplorationOnly(ZoneCompletionEntry entry)
        {
            return entry.EpisodeQuestCount == 0u
                && entry.TaskQuestCount == 0u
                && entry.ChallengeCount == 0u
                && entry.DatacubeCount == 0u
                && entry.TaleCount == 0u
                && entry.JournalCount == 0u;
        }
    }
}
