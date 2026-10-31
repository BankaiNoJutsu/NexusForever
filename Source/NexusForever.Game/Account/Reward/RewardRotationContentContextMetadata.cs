using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Retained diagnostic metadata for the compatibility content-context API.
    /// Rotation request throttle fields are separate from reward-track state;
    /// these values must not be emitted on reward-track opcodes 0x07CD/0x07D3.
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
            // Diagnostic compatibility value only; not a reward-track active flag.
            context.Flag = false;
        }
    }
}
