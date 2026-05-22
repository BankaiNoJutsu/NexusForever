using NexusForever.Game.Retail;
using NexusForever.Game.Static.Support;

namespace NexusForever.Game.Support
{
    public static class RetailStuckCooldownTracker
    {
        private static readonly object syncRoot = new();
        private static readonly Dictionary<(ulong CharacterId, UnstickType Type), DateTimeOffset> lastUseUtc = new();

        public static uint GetRemainingCooldownMs(ulong characterId, UnstickType unstickType)
        {
            int cooldownMs = GetCooldownMs(unstickType);
            lock (syncRoot)
            {
                if (!lastUseUtc.TryGetValue((characterId, unstickType), out DateTimeOffset last))
                    return 0;

                double remaining = cooldownMs - (DateTimeOffset.UtcNow - last).TotalMilliseconds;
                return remaining > 0 ? (uint)Math.Ceiling(remaining) : 0u;
            }
        }

        public static void RecordUse(ulong characterId, UnstickType unstickType)
        {
            lock (syncRoot)
            {
                lastUseUtc[(characterId, unstickType)] = DateTimeOffset.UtcNow;
            }
        }

        private static int GetCooldownMs(UnstickType unstickType)
        {
            return unstickType switch
            {
                UnstickType.RecallTransmat => RetailCertainRules.StuckRecallBindCooldownMs,
                UnstickType.RecallHouse    => RetailCertainRules.StuckRecallHouseCooldownMs,
                UnstickType.FreeSuicide    => RetailCertainRules.StuckRecallDeathCooldownMs,
                _                          => 0
            };
        }

        internal static void ClearForTests()
        {
            lock (syncRoot)
                lastUseUtc.Clear();
        }
    }
}
