using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07CD.
    // Wire format matches ServerRewardRotationContentContext_ReadPayload @ 14008fcb0:
    // 14-bit reward-rotation index, four 32-bit fields, a counted 32-bit id array, and a trailing 1-bit flag.
    // UInt0/UInt1/UInt3 correlate with reward-manager request-throttle slots initialized in
    // RewardRotation_ManagerInit @ 140635840 and read by Reward_SendRewardUpdateRequest @ 140636ba0.
    // The 0x07CD registrar still exposes no static apply helper, so slot assignment and Flag semantics remain blocked.
    [Message(GameMessageOpcode.ServerRewardRotationContentContext)]
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
