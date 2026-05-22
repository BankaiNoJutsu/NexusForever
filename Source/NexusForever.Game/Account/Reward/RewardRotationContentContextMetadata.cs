using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Correlated 0x07CD metadata from reward-manager init (<c>FUN_140635840</c> @ 140635840)
    /// and request throttle fields at <c>manager + 0x150 + index * 0x14</c>.
    /// UInt0/UInt1/UInt3 mirror client throttle defaults; the trailing Flag bit and dedicated
    /// 0x07CD apply helper remain unmapped (registrar passes a null handler; runtime dispatch only).
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
            // Flag consumer blocked: keep false until a named apply helper is recovered.
            context.Flag = false;
        }
    }
}
