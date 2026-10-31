namespace NexusForever.Game.Abstract.Account.Reward
{
    public sealed record ContractRewardModifierSource(uint RuntimeEffectId, uint SpellId, uint SourceEffectId, float Value);

    /// <summary>Copied actor-owned Contract modifier input, not completion authority.</summary>
    public sealed class ContractRewardModifierSnapshot
    {
        public uint TrackId { get; }
        public uint TrackType { get; }
        public float Multiplier { get; }
        public long Revision { get; }
        public IReadOnlyList<ContractRewardModifierSource> Sources { get; }

        public ContractRewardModifierSnapshot(uint trackId, uint trackType, float multiplier, long revision,
            IEnumerable<ContractRewardModifierSource> sources)
        {
            ArgumentNullException.ThrowIfNull(sources);
            TrackId = trackId; TrackType = trackType; Multiplier = multiplier; Revision = revision;
            Sources = Array.AsReadOnly(sources.ToArray());
        }

        // Actual Contract sources and mapped +50% effects yield exact integers.
        // Accept only where float32 and exact arithmetic agree: this does not
        // invent a retail fractional rounding, overflow or saturation policy.
        public bool TryGetPoints(uint basePoints, out uint points)
        {
            points = 0;
            double product = basePoints * (double)Multiplier;
            if (TrackId is 0 or > 0x3FFF || TrackType > 1 || !float.IsFinite(Multiplier) || Multiplier < 1
                || (double)Multiplier * 2 != Math.Truncate((double)Multiplier * 2)
                || !double.IsFinite(product) || product < 0 || product > uint.MaxValue
                || product != Math.Truncate(product) || (double)(float)product != product) return false;
            points = (uint)product; return true;
        }
    }
}
