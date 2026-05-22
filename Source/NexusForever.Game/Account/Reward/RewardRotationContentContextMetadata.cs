using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Correlated 0x07CD metadata from reward-manager init (<c>FUN_140635840</c> @ 140635840)
    /// and request throttle fields at <c>manager + 0x150 + index * 0x14</c>.
    /// Consumer semantics for these uints remain unmapped; values mirror client defaults.
    /// </summary>
    internal static class RewardRotationContentContextMetadata
    {
        public const uint ThrottleIntervalMilliseconds = 1000u;
        public const uint SecondaryThrottleMilliseconds = 1000u;

        public static void Apply(ServerRewardRotationContentContext context)
        {
            if (context == null)
                return;

            context.UInt0 = (uint)Environment.TickCount;
            context.UInt1 = ThrottleIntervalMilliseconds;
            context.UInt3 = SecondaryThrottleMilliseconds;
        }
    }
}
