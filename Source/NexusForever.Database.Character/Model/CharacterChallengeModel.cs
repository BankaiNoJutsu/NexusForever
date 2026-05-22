namespace NexusForever.Database.Character.Model
{
    public class CharacterChallengeModel
    {
        public ulong Id { get; set; }
        public ushort ChallengeId { get; set; }
        public bool Activated { get; set; }
        public bool OnCooldown { get; set; }
        public bool LeftArea { get; set; }
        public uint CurrentCount { get; set; }
        public uint CurrentTier { get; set; }
        public uint LastRewardTier { get; set; }
        public uint CompletionCount { get; set; }
        public double ActiveTimerSeconds { get; set; }
        public double CooldownTimerSeconds { get; set; }
        public double AreaFailTimerSeconds { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
