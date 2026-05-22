using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07CD.
    // Wire format matches ServerRewardRotationContentContext_ReadPayload @ 14008fcb0:
    // 14-bit reward-rotation index, four 32-bit fields, a counted 32-bit id array, and a trailing 1-bit flag.
    // UInt0/UInt1/UInt3 are correlated with reward-manager throttle defaults (manager + 0x150 + index * 0x14);
    // the dedicated 0x07CD apply helper is still unmapped in the client registrar.
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
