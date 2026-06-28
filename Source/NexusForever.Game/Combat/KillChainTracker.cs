namespace NexusForever.Game.Combat
{
    internal sealed class KillChainTracker
    {
        public const uint FirstAnnouncedKillCount = 2u;
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(5d);

        private uint killCount;
        private DateTimeOffset lastKillTime;

        public uint RecordKill(DateTimeOffset killTime, TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
                window = DefaultWindow;

            if (killCount == 0u || killTime - lastKillTime > window)
                killCount = 1u;
            else
                killCount = killCount == uint.MaxValue ? uint.MaxValue : killCount + 1u;

            lastKillTime = killTime;
            return killCount;
        }
    }
}
