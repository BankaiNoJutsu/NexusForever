using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Builds <see cref="ServerRewardRotationScheduleArray"/> rows per content id using
    /// game-table catalog eligibility (MinPlayerLevel, WorldDifficultyFlags) and deterministic
    /// content-id selection. Client tables still expose no direct content→reward linkage.
    /// </summary>
    public static class RewardRotationScheduleBuilder
    {
        public const byte RewardTypeItem = 1;
        public const byte RewardTypeEssence = 2;
        public const byte RewardTypeModifier = 3;

        public static ServerRewardRotationScheduleArray Build(
            RewardRotationContentContextSources sources,
            uint rewardRotationIndex,
            IReadOnlyList<uint> contentIds,
            IGameTableManager gameTables = null)
        {
            if (contentIds == null || contentIds.Count == 0)
                return new ServerRewardRotationScheduleArray();

            float durationDays = RewardRotationScheduleDefaults.GetRotationDurationDays(gameTables);

            List<RewardRotationItemEntry> items = (sources.RewardRotationItem.Entries ?? Array.Empty<RewardRotationItemEntry>())
                .Where(entry => entry != null && entry.Id != 0u)
                .OrderBy(entry => entry.Id)
                .ToList();
            List<RewardRotationEssenceEntry> essences = (sources.RewardRotationEssence.Entries ?? Array.Empty<RewardRotationEssenceEntry>())
                .Where(entry => entry != null && entry.Id != 0u)
                .OrderBy(entry => entry.Id)
                .ToList();
            List<RewardRotationModifierEntry> modifiers = (sources.RewardRotationModifier.Entries ?? Array.Empty<RewardRotationModifierEntry>())
                .Where(entry => entry != null && entry.Id != 0u)
                .OrderBy(entry => entry.Id)
                .ToList();

            ServerRewardRotationScheduleArray schedule = new();
            foreach (uint contentId in contentIds)
            {
                if (contentId == 0u)
                    continue;

                TryAddItemRow(schedule, contentId, durationDays, items);
                TryAddEssenceRow(schedule, contentId, durationDays, essences);
                TryAddModifierRow(schedule, contentId, durationDays, modifiers);
            }

            return schedule;
        }

        public static ServerRewardRotationScheduleArray Build(IGameTableManager gameTables, uint rewardRotationIndex)
        {
            if (gameTables == null)
                throw new ArgumentNullException(nameof(gameTables));

            RewardRotationContentContextSources sources = RewardRotationContentContextSources.From(gameTables);
            List<uint> contentIds = RewardRotationContentContextBuilder.CollectContentIds(sources, rewardRotationIndex);
            return Build(sources, rewardRotationIndex, contentIds, gameTables);
        }

        private static void TryAddItemRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationItemEntry> items)
        {
            RewardRotationItemEntry entry = RewardRotationPerContentRewardCatalog.SelectItem(items, contentId);
            if (entry == null)
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeItem,
                Value = entry.Count
            });
        }

        private static void TryAddEssenceRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationEssenceEntry> essences)
        {
            RewardRotationEssenceEntry entry = RewardRotationPerContentRewardCatalog.SelectEssence(essences, contentId);
            if (entry == null)
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeEssence,
                Value = 1u
            });
        }

        private static void TryAddModifierRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationModifierEntry> modifiers)
        {
            RewardRotationModifierEntry entry = RewardRotationPerContentRewardCatalog.SelectModifier(modifiers, contentId);
            if (entry == null)
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeModifier,
                Value = unchecked((uint)entry.ModifierValue)
            });
        }

    }
}
