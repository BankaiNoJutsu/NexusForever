namespace NexusForever.Network.World.Message.Model.Reward
{
    internal static class RewardTrackWireValidation
    {
        public static void Validate(uint trackId, NetworkBitArray claimed)
        {
            if (trackId > 0x3FFFu)
                throw new ArgumentOutOfRangeException(nameof(trackId), trackId, "Reward track ID exceeds its 14-bit wire field.");
            if (claimed?.GetBuffer().Length != sizeof(uint))
                throw new InvalidOperationException("Reward track claimed state must contain exactly 32 bits.");
        }

        public static void Validate(RewardTrack track)
        {
            if (track == null)
                throw new InvalidOperationException("Reward track row is missing.");
            Validate(track.RewardTrackId, track.ClaimedRewards);
            if (track.RewardTrackRewardsIds == null || track.RewardTrackRewardsIds.Count > 32)
                throw new InvalidOperationException("Reward track reward rows exceed the 32-bit claim mask.");
        }
    }
}
