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
        public const uint DefaultPlayerLevel = 1u;
        public const uint DefaultWorldDifficultyFlags = 0u;
        public const uint WorldDifficultyFlagNormal = 1u;
        public const uint WorldDifficultyFlagVeteran = 2u;
        public const uint KnownWorldDifficultyFlags = WorldDifficultyFlagNormal | WorldDifficultyFlagVeteran;

        public static ServerRewardRotationScheduleArray Build(
            RewardRotationContentContextSources sources,
            uint rewardRotationIndex,
            IReadOnlyList<uint> contentIds,
            IGameTableManager gameTables = null,
            uint playerLevel = DefaultPlayerLevel,
            uint worldDifficultyFlags = DefaultWorldDifficultyFlags)
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

                foreach (uint scheduleWorldDifficultyFlags in GetScheduleWorldDifficultyFlags(worldDifficultyFlags))
                {
                    TryAddItemRow(schedule, contentId, durationDays, items, playerLevel, scheduleWorldDifficultyFlags);
                    TryAddEssenceRow(schedule, contentId, durationDays, essences, playerLevel, scheduleWorldDifficultyFlags);
                    TryAddModifierRow(schedule, contentId, durationDays, modifiers, playerLevel, scheduleWorldDifficultyFlags);
                }
            }

            return schedule;
        }

        public static ServerRewardRotationScheduleArray Build(
            IGameTableManager gameTables,
            uint rewardRotationIndex,
            uint playerLevel = DefaultPlayerLevel,
            uint worldDifficultyFlags = DefaultWorldDifficultyFlags)
        {
            if (gameTables == null)
                throw new ArgumentNullException(nameof(gameTables));

            RewardRotationContentContextSources sources = RewardRotationContentContextSources.From(gameTables);
            List<uint> contentIds = RewardRotationContentContextBuilder.CollectContentIds(sources, rewardRotationIndex);
            return Build(sources, rewardRotationIndex, contentIds, gameTables, playerLevel, worldDifficultyFlags);
        }

        private static void TryAddItemRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationItemEntry> items,
            uint playerLevel,
            uint worldDifficultyFlags)
        {
            RewardRotationItemEntry entry = RewardRotationPerContentRewardCatalog.SelectItem(items, contentId, playerLevel, worldDifficultyFlags);
            if (entry == null)
                return;
            if (HasScheduleRow(schedule, contentId, RewardTypeItem, entry.Id))
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeItem,
                Value = entry.Id
            });
        }

        private static void TryAddEssenceRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationEssenceEntry> essences,
            uint playerLevel,
            uint worldDifficultyFlags)
        {
            RewardRotationEssenceEntry entry = RewardRotationPerContentRewardCatalog.SelectEssence(essences, contentId, playerLevel, worldDifficultyFlags);
            if (entry == null)
                return;
            if (HasScheduleRow(schedule, contentId, RewardTypeEssence, entry.Id))
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeEssence,
                Value = entry.Id
            });
        }

        private static void TryAddModifierRow(
            ServerRewardRotationScheduleArray schedule,
            uint contentId,
            float durationDays,
            List<RewardRotationModifierEntry> modifiers,
            uint playerLevel,
            uint worldDifficultyFlags)
        {
            RewardRotationModifierEntry entry = RewardRotationPerContentRewardCatalog.SelectModifier(modifiers, contentId, playerLevel, worldDifficultyFlags);
            if (entry == null)
                return;
            if (HasScheduleRow(schedule, contentId, RewardTypeModifier, entry.Id))
                return;

            schedule.Entries.Add(new ServerRewardRotationScheduleArray.ScheduleRow
            {
                ContentId = contentId,
                RewardKeyId = entry.Id,
                Duration = durationDays,
                RewardType = RewardTypeModifier,
                Value = entry.Id
            });
        }

        private static IEnumerable<uint> GetScheduleWorldDifficultyFlags(uint worldDifficultyFlags)
        {
            if ((worldDifficultyFlags & KnownWorldDifficultyFlags) != KnownWorldDifficultyFlags)
            {
                yield return worldDifficultyFlags;
                yield break;
            }

            yield return WorldDifficultyFlagNormal;
            yield return WorldDifficultyFlagVeteran;
        }

        private static bool HasScheduleRow(ServerRewardRotationScheduleArray schedule, uint contentId, byte rewardType, uint rewardKeyId)
        {
            return schedule.Entries.Any(row =>
                row.ContentId == contentId &&
                row.RewardType == rewardType &&
                row.RewardKeyId == rewardKeyId);
        }

    }
}
