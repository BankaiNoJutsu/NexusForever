using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Retained diagnostic/service data. Native dispatch proves 0x07CD belongs
    // to reward tracks, not rotation content/throttle metadata. No wire opcode.
    public class ServerRewardRotationContentContext : IWritable
    {
        public uint RewardRotationIndex { get; set; }
        public uint UInt0 { get; set; }
        public uint UInt1 { get; set; }
        public uint UInt3 { get; set; }
        public List<uint> ContentIds { get; set; } = new();
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            RewardRotationWireValidation.ValidateContentContext(this, nameof(ServerRewardRotationContentContext));
            writer.Write(RewardRotationIndex, 14u);
            writer.Write(UInt0);
            writer.Write(UInt1);
            writer.Write((uint)ContentIds.Count);
            writer.Write(UInt3);
            foreach (uint contentId in ContentIds)
                writer.Write(contentId);
            writer.Write(Flag);
        }
    }
}
