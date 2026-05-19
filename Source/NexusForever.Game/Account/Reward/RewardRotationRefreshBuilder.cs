using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    public static class RewardRotationRefreshBuilder
    {
        // Reward_SendRewardUpdateRequest throttles indices below seven and
        // RewardRotation_ApplyServerScheduleUpdate dispatches RewardRotationsUpdated for the same range.
        public const uint ContentTypeCount = 7u;
        public static uint MaxSupportedRewardRotationIndex => ContentTypeCount - 1u;

        public static bool IsSupported(uint rewardRotationIndex)
        {
            return rewardRotationIndex < ContentTypeCount;
        }

        public static RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return Build(rewardRotationIndex, null);
        }

        public static RewardRotationRefresh Build(uint rewardRotationIndex, IRewardRotationRefreshProvider provider)
        {
            if (!IsSupported(rewardRotationIndex))
                return null;

            provider ??= EmptyRewardRotationRefreshProvider.Instance;

            RewardRotationRefresh refresh = provider.Build(rewardRotationIndex);
            if (refresh == null)
                throw new InvalidOperationException($"{provider.GetType().Name} returned a null {nameof(RewardRotationRefresh)} for reward rotation index {rewardRotationIndex}.");

            if (refresh.RewardRotationIndex != rewardRotationIndex)
                throw new InvalidOperationException($"{provider.GetType().Name} returned reward rotation index {refresh.RewardRotationIndex} for request {rewardRotationIndex}.");

            return refresh;
        }
    }

    public interface IRewardRotationRefreshProvider
    {
        RewardRotationRefresh Build(uint rewardRotationIndex);
    }

    public sealed class EmptyRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        public static EmptyRewardRotationRefreshProvider Instance { get; } = new();

        private EmptyRewardRotationRefreshProvider()
        {
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return new RewardRotationRefresh(
                rewardRotationIndex,
                new ServerRewardRotationScheduleArray(),
                new ServerRewardRotationEntryStateArray(),
                "empty-placeholder-provider");
        }
    }

    public sealed class RewardRotationRefresh
    {
        public uint RewardRotationIndex { get; }
        public ServerRewardRotationScheduleArray ScheduleArray { get; }
        public ServerRewardRotationEntryStateArray EntryStateArray { get; }
        public string ResponseSource { get; }
        public int ScheduleEntryCount => ScheduleArray.Entries.Count;
        public int EntryStateCount => EntryStateArray.Entries.Count;
        public bool IsPlaceholder => ScheduleEntryCount == 0 && EntryStateCount == 0;

        public RewardRotationRefresh(
            uint rewardRotationIndex,
            ServerRewardRotationScheduleArray scheduleArray,
            ServerRewardRotationEntryStateArray entryStateArray,
            string responseSource = null)
        {
            RewardRotationIndex = rewardRotationIndex;
            ScheduleArray = scheduleArray ?? throw new ArgumentNullException(nameof(scheduleArray));
            EntryStateArray = entryStateArray ?? throw new ArgumentNullException(nameof(entryStateArray));
            ResponseSource = string.IsNullOrWhiteSpace(responseSource) ? "unspecified" : responseSource;
        }
    }
}
