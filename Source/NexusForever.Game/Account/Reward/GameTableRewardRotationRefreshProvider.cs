using System.Linq;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Emits game-table-derived reward rotation refresh packets: content context (0x07CD/0x07D3),
    /// per-content schedule rows (0x07CA), and optional persisted entry-state (0x07C8).
    /// </summary>
    public sealed class GameTableRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        private readonly IGameTableManager gameTables;
        private readonly uint playerLevel;
        private readonly uint worldDifficultyFlags;

        public GameTableRewardRotationRefreshProvider(
            IGameTableManager gameTables,
            uint playerLevel = RewardRotationScheduleBuilder.DefaultPlayerLevel,
            uint worldDifficultyFlags = RewardRotationScheduleBuilder.DefaultWorldDifficultyFlags)
        {
            this.gameTables = gameTables ?? throw new ArgumentNullException(nameof(gameTables));
            this.playerLevel = playerLevel;
            this.worldDifficultyFlags = worldDifficultyFlags;
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return Build(rewardRotationIndex, null);
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex, ServerRewardRotationEntryStateArray entryState)
        {
            RewardRotationContentContextSources sources = RewardRotationContentContextSources.From(gameTables);
            List<ServerRewardRotationContentContext> contentContexts = RewardRotationContentContextBuilder.BuildContexts(sources, rewardRotationIndex);
            List<uint> contentIds = contentContexts.SelectMany(context => context.ContentIds).Distinct().OrderBy(id => id).ToList();
            ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
                sources,
                rewardRotationIndex,
                contentIds,
                gameTables,
                playerLevel,
                worldDifficultyFlags);
            int contentIdCount = contentIds.Count;
            entryState ??= new ServerRewardRotationEntryStateArray();
            int entryStateCount = entryState.Entries.Count;

            string responseSource = contentIdCount == 0
                ? entryStateCount > 0
                    ? "game-table-entry-state-only"
                    : "game-table-refresh-empty"
                : schedule.Entries.Count == 0
                    ? entryStateCount > 0
                        ? "game-table-entry-state-only"
                        : "game-table-content-context-only"
                    : contentContexts.Count > 1
                        ? entryStateCount > 0
                            ? "game-table-content-context-array-with-schedule-and-entry-state"
                            : "game-table-content-context-array-with-schedule"
                        : entryStateCount > 0
                            ? "game-table-content-context-with-schedule-and-entry-state"
                            : "game-table-content-context-with-schedule";

            return new RewardRotationRefresh(
                rewardRotationIndex,
                contentContexts,
                schedule,
                entryState,
                responseSource);
        }
    }
}
