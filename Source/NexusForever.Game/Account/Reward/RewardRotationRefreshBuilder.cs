using NexusForever.Network.Message;
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
                contentContext: null,
                new ServerRewardRotationScheduleArray(),
                new ServerRewardRotationEntryStateArray(),
                "empty-placeholder-provider");
        }
    }

    public sealed class RewardRotationRefresh
    {
        public uint RewardRotationIndex { get; }
        public IReadOnlyList<ServerRewardRotationContentContext> ContentContexts { get; }
        public ServerRewardRotationContentContext ContentContext => ContentContexts.FirstOrDefault();
        public ServerRewardRotationScheduleArray ScheduleArray { get; }
        public ServerRewardRotationEntryStateArray EntryStateArray { get; }
        public string ResponseSource { get; }
        public int ContentContextIdCount => ContentContexts.Sum(context => context?.ContentIds.Count ?? 0);
        public int ContentContextPacketCount => ContentContexts.Count(context => context != null && context.ContentIds.Count > 0);
        public int ScheduleEntryCount => ScheduleArray.Entries.Count;
        public int EntryStateCount => EntryStateArray.Entries.Count;
        public bool HasContentContext => ContentContextIdCount > 0;
        public bool IsPlaceholder => !HasContentContext && ScheduleEntryCount == 0 && EntryStateCount == 0;

        public RewardRotationRefresh(
            uint rewardRotationIndex,
            ServerRewardRotationScheduleArray scheduleArray,
            ServerRewardRotationEntryStateArray entryStateArray,
            string responseSource = null)
            : this(rewardRotationIndex, Array.Empty<ServerRewardRotationContentContext>(), scheduleArray, entryStateArray, responseSource)
        {
        }

        public RewardRotationRefresh(
            uint rewardRotationIndex,
            ServerRewardRotationContentContext contentContext,
            ServerRewardRotationScheduleArray scheduleArray,
            ServerRewardRotationEntryStateArray entryStateArray,
            string responseSource = null)
            : this(
                rewardRotationIndex,
                contentContext == null ? null : new[] { contentContext },
                scheduleArray,
                entryStateArray,
                responseSource)
        {
        }

        public RewardRotationRefresh(
            uint rewardRotationIndex,
            IReadOnlyList<ServerRewardRotationContentContext> contentContexts,
            ServerRewardRotationScheduleArray scheduleArray,
            ServerRewardRotationEntryStateArray entryStateArray,
            string responseSource = null)
        {
            RewardRotationIndex = rewardRotationIndex;
            ContentContexts = contentContexts == null
                ? Array.Empty<ServerRewardRotationContentContext>()
                : contentContexts.Where(context => context != null).ToList();
            ScheduleArray = scheduleArray ?? throw new ArgumentNullException(nameof(scheduleArray));
            EntryStateArray = entryStateArray ?? throw new ArgumentNullException(nameof(entryStateArray));
            ResponseSource = string.IsNullOrWhiteSpace(responseSource) ? "unspecified" : responseSource;
        }

        public IEnumerable<IWritable> GetContentContextPackets()
        {
            return RewardRotationContentContextDelivery.CreatePackets(ContentContexts);
        }
    }
}
