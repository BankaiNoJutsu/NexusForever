namespace NexusForever.Game.Static.Support
{
    /// <summary>
    /// Spell4 ids paired with <see cref="UnstickType"/> from retail support-stuck fixtures.
    /// </summary>
    public static class SupportStuckSpell4Ids
    {
        public const uint RecallTransmat = 49881u;
        public const uint RecallHouse    = 49882u;
        public const uint FreeSuicide    = 52577u;

        public static uint GetSpell4Id(UnstickType unstickType)
        {
            return unstickType switch
            {
                UnstickType.RecallTransmat => RecallTransmat,
                UnstickType.RecallHouse    => RecallHouse,
                UnstickType.FreeSuicide    => FreeSuicide,
                _                          => 0u
            };
        }
    }
}
