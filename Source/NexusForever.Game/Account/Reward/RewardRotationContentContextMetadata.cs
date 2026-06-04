using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Correlated 0x07CD metadata from reward-manager init (<c>RewardRotation_ManagerInit</c> @ 140635840)
    /// and request throttle fields read by <c>Reward_SendRewardUpdateRequest</c> @ 140636ba0.
    /// Static evidence maps the throttle slot defaults, but not the 0x07CD apply assignment, so
    /// UInt0/UInt1/UInt3 remain neutral and the trailing Flag stays false until capture or dynamic proof.
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
            // Flag consumer blocked: keep false until a named apply helper, capture, or dynamic dispatch proof is recovered.
            context.Flag = false;
        }
    }
}
