using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07C9 (inferred: middle of three entry-state delta opcodes → update existing row).
    // Wire format matches RewardRotation_EntryStateRow_ReadPayload at client address 1400a1ee0:
    // 3-bit TypeId, 32-bit ContentId, 32-bit RewardTypeId, 8-bit State, 32-bit Value.
    [Message(GameMessageOpcode.ServerRewardRotationEntryStateUpdate)]
    public class ServerRewardRotationEntryStateUpdate : IWritable
    {
        public uint TypeId { get; set; }
        public uint ContentId { get; set; }
        public uint RewardTypeId { get; set; }
        public byte State { get; set; }
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            RewardRotationWireValidation.ValidateEntryStateTypeId(TypeId, nameof(ServerRewardRotationEntryStateUpdate));
            RewardRotationWireValidation.ValidateEntryStateRewardTypeLane(State, nameof(ServerRewardRotationEntryStateUpdate));
            writer.Write(TypeId, 3u);
            writer.Write(ContentId);
            writer.Write(RewardTypeId);
            writer.Write(State);
            writer.Write(Value);
        }
    }
}
