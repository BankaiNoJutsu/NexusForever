using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    internal static class RewardRotationContentContextDelivery
    {
        public static IEnumerable<IWritable> CreatePackets(IReadOnlyList<ServerRewardRotationContentContext> contentContexts)
        {
            if (contentContexts == null || contentContexts.Count == 0)
                yield break;

            List<ServerRewardRotationContentContext> nonEmpty = contentContexts
                .Where(context => context != null && context.ContentIds.Count > 0)
                .ToList();
            if (nonEmpty.Count == 0)
                yield break;

            if (nonEmpty.Count == 1)
            {
                yield return nonEmpty[0];
                yield break;
            }

            yield return new ServerRewardRotationContentContextArray
            {
                Entries = nonEmpty
            };
        }
    }
}
