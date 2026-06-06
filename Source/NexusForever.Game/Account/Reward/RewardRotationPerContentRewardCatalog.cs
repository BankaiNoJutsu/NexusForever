using NexusForever.GameTable.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Selects reward rotation catalog rows per content id using game-table eligibility fields.
    /// Client tables expose no direct RewardRotationContent→reward linkage; selection is deterministic
    /// within MinPlayerLevel/WorldDifficultyFlags-filtered catalogs keyed by content id.
    /// </summary>
    internal static class RewardRotationPerContentRewardCatalog
    {
        public static RewardRotationItemEntry SelectItem(
            IReadOnlyList<RewardRotationItemEntry> items,
            uint contentId,
            uint playerLevel = RewardRotationScheduleBuilder.DefaultPlayerLevel,
            uint worldDifficultyFlags = RewardRotationScheduleBuilder.DefaultWorldDifficultyFlags)
        {
            return SelectEntry(Filter(items, playerLevel, worldDifficultyFlags, entry => entry.MinPlayerLevel, entry => entry.WorldDifficultyFlags), contentId, entry => entry.Id);
        }

        public static RewardRotationEssenceEntry SelectEssence(
            IReadOnlyList<RewardRotationEssenceEntry> essences,
            uint contentId,
            uint playerLevel = RewardRotationScheduleBuilder.DefaultPlayerLevel,
            uint worldDifficultyFlags = RewardRotationScheduleBuilder.DefaultWorldDifficultyFlags)
        {
            return SelectEntry(Filter(essences, playerLevel, worldDifficultyFlags, entry => entry.MinPlayerLevel, entry => entry.WorldDifficultyFlags), contentId, entry => entry.Id);
        }

        public static RewardRotationModifierEntry SelectModifier(
            IReadOnlyList<RewardRotationModifierEntry> modifiers,
            uint contentId,
            uint playerLevel = RewardRotationScheduleBuilder.DefaultPlayerLevel,
            uint worldDifficultyFlags = RewardRotationScheduleBuilder.DefaultWorldDifficultyFlags)
        {
            return SelectEntry(Filter(modifiers, playerLevel, worldDifficultyFlags, entry => entry.MinPlayerLevel, entry => entry.WorldDifficultyFlags), contentId, entry => entry.Id);
        }

        private static List<T> Filter<T>(
            IEnumerable<T> entries,
            uint playerLevel,
            uint worldDifficultyFlags,
            Func<T, uint> getMinPlayerLevel,
            Func<T, uint> getWorldDifficultyFlags)
        {
            return (entries ?? Array.Empty<T>())
                .Where(entry => entry != null)
                .Where(entry => getMinPlayerLevel(entry) <= playerLevel)
                .Where(entry =>
                {
                    uint requiredFlags = getWorldDifficultyFlags(entry);
                    return requiredFlags == 0u || (worldDifficultyFlags & requiredFlags) == requiredFlags;
                })
                .OrderBy(getMinPlayerLevel)
                .ThenBy(entry => getWorldDifficultyFlags(entry))
                .ToList();
        }

        private static T SelectEntry<T>(List<T> entries, uint contentId, Func<T, uint> getId)
        {
            if (entries.Count == 0)
                return default;

            return entries[(int)(contentId % (uint)entries.Count)];
        }
    }
}
