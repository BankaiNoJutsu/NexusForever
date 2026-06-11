using System.Linq;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Builds <see cref="ServerRewardRotationContentContext"/> payloads from the same game-table
    /// sources the client walks in <c>RewardRotation_BuildContentContextIndex</c> (140635b60).
    /// UInt0/UInt1/UInt3 are populated with correlated throttle metadata; Flag stays false until mapped.
    /// </summary>
    public static class RewardRotationContentContextBuilder
    {
        public static ServerRewardRotationContentContext Build(IGameTableManager gameTables, uint rewardRotationIndex)
        {
            return Build(RewardRotationContentContextSources.From(gameTables), rewardRotationIndex);
        }

        public static ServerRewardRotationContentContext Build(RewardRotationContentContextSources sources, uint rewardRotationIndex)
        {
            return BuildContexts(sources, rewardRotationIndex).FirstOrDefault()
                ?? new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = rewardRotationIndex
                };
        }

        public static List<ServerRewardRotationContentContext> BuildContexts(RewardRotationContentContextSources sources, uint rewardRotationIndex)
        {
            if (!RewardRotationRefreshBuilder.IsSupported(rewardRotationIndex))
                throw new ArgumentOutOfRangeException(nameof(rewardRotationIndex), rewardRotationIndex, "Reward rotation index is outside the evidence-backed 0-6 range.");

            List<uint> contentIds = CollectContentIds(sources, rewardRotationIndex);
            if (contentIds.Count == 0)
                return new List<ServerRewardRotationContentContext>();

            List<ServerRewardRotationContentContext> contexts = new();
            for (int offset = 0; offset < contentIds.Count; offset += RewardRotationWireBudget.MaxContentContextContentIds)
            {
                var context = new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = rewardRotationIndex,
                    ContentIds = contentIds.Skip(offset).Take(RewardRotationWireBudget.MaxContentContextContentIds).ToList()
                };
                RewardRotationContentContextMetadata.Apply(context);
                contexts.Add(context);
            }

            return contexts;
        }

        internal static List<uint> CollectContentIds(RewardRotationContentContextSources sources, uint rewardRotationIndex)
        {
            HashSet<uint> contentIds = new();

            foreach (RewardRotationContentEntry contentEntry in sources.RewardRotationContent.Entries ?? Array.Empty<RewardRotationContentEntry>())
            {
                if (contentEntry == null || contentEntry.Id == 0u)
                    continue;

                if (contentEntry.ContentTypeEnum == rewardRotationIndex)
                    contentIds.Add(contentEntry.Id);
            }

            foreach (WorldZoneEntry worldZoneEntry in sources.WorldZone.Entries ?? Array.Empty<WorldZoneEntry>())
                TryAddContentId(sources, contentIds, rewardRotationIndex, worldZoneEntry?.RewardRotationContentId ?? 0u);

            foreach (WorldEntry worldEntry in sources.World.Entries ?? Array.Empty<WorldEntry>())
                TryAddContentId(sources, contentIds, rewardRotationIndex, worldEntry?.RewardRotationContentId ?? 0u);

            foreach (PublicEventEntry publicEventEntry in sources.PublicEvent.Entries ?? Array.Empty<PublicEventEntry>())
                TryAddContentId(sources, contentIds, rewardRotationIndex, publicEventEntry?.RewardRotationContentId ?? 0u);

            foreach (MatchTypeRewardRotationContentEntry matchTypeEntry in sources.MatchTypeRewardRotationContent.Entries ?? Array.Empty<MatchTypeRewardRotationContentEntry>())
            {
                TryAddContentId(sources, contentIds, rewardRotationIndex, matchTypeEntry?.RewardRotationContentIdRandomNormal ?? 0u);
                TryAddContentId(sources, contentIds, rewardRotationIndex, matchTypeEntry?.RewardRotationContentIdRandomVeteran ?? 0u);
            }

            List<uint> sorted = contentIds.ToList();
            sorted.Sort();
            return sorted;
        }

        private static void TryAddContentId(RewardRotationContentContextSources sources, HashSet<uint> contentIds, uint rewardRotationIndex, uint rewardRotationContentId)
        {
            if (rewardRotationContentId == 0u)
                return;

            RewardRotationContentEntry contentEntry = sources.RewardRotationContent.Entries?
                .FirstOrDefault(entry => entry != null && entry.Id == rewardRotationContentId);
            if (contentEntry == null || contentEntry.ContentTypeEnum != rewardRotationIndex)
                return;

            contentIds.Add(rewardRotationContentId);
        }
    }
}
