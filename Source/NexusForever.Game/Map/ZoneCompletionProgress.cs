namespace NexusForever.Game.Map
{
    internal readonly struct ZoneCompletionProgress
    {
        public uint EpisodeQuestCount { get; init; }
        public uint TaskQuestCount { get; init; }
        public uint ChallengeCount { get; init; }
        public uint DatacubeCount { get; init; }
        public uint TaleCount { get; init; }
        public uint JournalCount { get; init; }
    }
}
