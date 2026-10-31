using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Reward
{
    public class RewardTrack : IWritable
    {
        public uint RewardTrackId { get; set; }
        public uint RewardPointsEarned { get; set; }
        public NetworkBitArray ClaimedRewards { get; set; } = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
        public uint Unlocked { get; set; } // native progress slot +0x28; also consulted for linear-parent unlock
        public List<uint> RewardTrackRewardsIds { get; set; } = [];
        public bool IsActive { get; set; }

        public void Write(GamePacketWriter writer)
        {
            RewardTrackWireValidation.Validate(this);
            writer.Write(RewardTrackId, 14u);
            writer.Write(RewardPointsEarned);
            writer.WriteBytes(ClaimedRewards.GetBuffer());
            writer.Write(RewardTrackRewardsIds.Count);
            writer.Write(Unlocked, 32u);
            RewardTrackRewardsIds.ForEach(rewardId => writer.Write(rewardId));
            writer.Write(IsActive);
        }
    }
}
