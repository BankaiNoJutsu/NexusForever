using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Reward
{
    [Message(GameMessageOpcode.ServerRewardTrackUpdate)]
    public class ServerRewardTrackUpdate : IWritable
    {
        public uint RewardTrackId { get; set; }
        public uint RewardPointsEarned { get; set; }
        public NetworkBitArray ClaimedRewards { get; set; } = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
        public uint Unlocked { get; set; } // native progress slot +0x28; also consulted for linear-parent unlock
        public bool IsActive { get; set; }

        public void Write(GamePacketWriter writer)
        {
            RewardTrackWireValidation.Validate(RewardTrackId, ClaimedRewards);
            writer.Write(RewardTrackId, 14u);
            writer.Write(RewardPointsEarned);
            writer.WriteBytes(ClaimedRewards.GetBuffer());
            writer.Write(Unlocked);
            writer.Write(IsActive);
        }
    }
}
