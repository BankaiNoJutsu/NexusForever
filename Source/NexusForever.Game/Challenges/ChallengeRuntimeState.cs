namespace NexusForever.Game.Challenges
{
    internal sealed class ChallengeRuntimeState
    {
        public uint ChallengeId { get; init; }
        public bool Activated { get; set; }
        public bool OnCooldown { get; set; }
        public bool LeftArea { get; set; }
        public uint CurrentCount { get; set; }
        public uint CurrentTier { get; set; }
        public uint LastRewardTier { get; set; }
        public uint CompletionCount { get; set; }
        public double ActiveTimer { get; set; }
        public double CooldownTimer { get; set; }
        public double AreaFailTimer { get; set; }
        public double ShareTimer { get; set; }
    }

    internal sealed class PendingChallengeShare
    {
        public uint SharerUnitId { get; init; }
        public double Timer { get; set; }
    }
}
