using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    internal static class RewardRotationContentContextDelivery
    {
        public static IEnumerable<IWritable> CreatePackets(IReadOnlyList<ServerRewardRotationContentContext> contentContexts)
        {
            // Diagnostic rows formerly used 0x07CD/0x07D3, whose actual consumer
            // is the reward-track manager. Rotation schedule/state APIs remain.
            return Array.Empty<IWritable>();
        }
    }
}
