using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Map
{
    internal static class ZoneCompletionRewardResolver
    {
        public static IReadOnlyList<ushort> GetExplorationOnlyTitleRewards(uint mapZoneId, IGameTableManager gameTableManager)
        {
            ZoneCompletionEntry[] entries = gameTableManager?.ZoneCompletion?.Entries;
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

        public static bool TryGetTitleReward(IPlayer player, uint mapZoneId, bool explorationComplete, out ushort titleId, IGameTableManager gameTableManager)
        {
            titleId = 0;

            if (!explorationComplete)
                return false;

            ZoneCompletionFaction? faction = GetPlayerZoneCompletionFaction(player.Faction1);
            if (faction == null)
                return false;

            ZoneCompletionEntry entry = GetEntry(mapZoneId, faction, gameTableManager);
            if (entry == null || !TryGetTitleId(entry, out titleId))
                return false;

            if (IsExplorationOnly(entry))
                return true;

            ZoneCompletionProgress progress = ZoneCompletionProgressTracker.GetProgress(player, mapZoneId, gameTableManager);
            return MeetsCategoryRequirements(entry, progress);
        }

        public static ZoneCompletionEntry GetEntry(uint mapZoneId, ZoneCompletionFaction? faction, IGameTableManager gameTableManager)
        {
            ZoneCompletionEntry[] entries = gameTableManager?.ZoneCompletion?.Entries;
            if (entries == null)
                return null;

            IEnumerable<ZoneCompletionEntry> matches = entries.Where(e => e.MapZoneId == mapZoneId);
            if (faction != null)
                matches = matches.Where(e => e.ZoneCompletionFactionEnum == (uint)faction.Value);

            return matches.FirstOrDefault();
        }

        public static ZoneCompletionFaction? GetPlayerZoneCompletionFaction(Faction faction)
        {
            return faction switch
            {
                Faction.Dominion => ZoneCompletionFaction.Dominion,
                Faction.Exile    => ZoneCompletionFaction.Exile,
                _                => null,
            };
        }

        public static bool MeetsCategoryRequirements(ZoneCompletionEntry entry, ZoneCompletionProgress progress)
        {
            return progress.EpisodeQuestCount >= entry.EpisodeQuestCount
                && progress.TaskQuestCount >= entry.TaskQuestCount
                && progress.ChallengeCount >= entry.ChallengeCount
                && progress.DatacubeCount >= entry.DatacubeCount
                && progress.TaleCount >= entry.TaleCount
                && progress.JournalCount >= entry.JournalCount;
        }

        public static bool IsExplorationOnly(ZoneCompletionEntry entry)
        {
            return entry.EpisodeQuestCount == 0u
                && entry.TaskQuestCount == 0u
                && entry.ChallengeCount == 0u
                && entry.DatacubeCount == 0u
                && entry.TaleCount == 0u
                && entry.JournalCount == 0u;
        }

        private static bool TryGetTitleId(ZoneCompletionEntry entry, out ushort titleId)
        {
            titleId = 0;
            if (entry.CharacterTitleIdReward is > 0u and <= ushort.MaxValue)
            {
                titleId = (ushort)entry.CharacterTitleIdReward;
                return true;
            }

            return false;
        }
    }
}
