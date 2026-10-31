namespace NexusForever.Game.Abstract.Account.Reward
{
    /// <summary>A frozen whole Challenge multiplier and its source revision.</summary>
    public readonly struct ChallengeRewardModifierSnapshot(float multiplier, long revision)
    {
        public float Multiplier { get; } = multiplier;
        public long Revision { get; } = revision;

        /// <summary>
        /// Evaluate one medal increment using native float32 multiplication and
        /// truncation. Invalid or overflowing awards fail without producing points.
        /// </summary>
        public bool TryGetPoints(uint baseIncrement, out uint points)
        {
            points = 0;
            float product = (float)baseIncrement * Multiplier;
            if (!float.IsFinite(Multiplier) || Multiplier < 0f
                || !float.IsFinite(product) || product < 0f || product >= 4294967296f)
                return false;
            points = (uint)product;
            return true;
        }
    }
}
