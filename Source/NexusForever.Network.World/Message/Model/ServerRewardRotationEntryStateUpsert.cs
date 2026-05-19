using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07C7 (inferred: lowest of three entry-state delta opcodes → add/upsert).
    // Wire format matches RewardRotation_EntryStateRow_ReadPayload at client address 1400a1ee0:
    // 3-bit TypeId, 32-bit ContentId, 32-bit RewardTypeId, 8-bit State, 32-bit Value.
    [Message(GameMessageOpcode.ServerRewardRotationEntryStateUpsert)]
    public class ServerRewardRotationEntryStateUpsert : IWritable
    {
        public uint TypeId { get; set; }
        public uint ContentId { get; set; }
        public uint RewardTypeId { get; set; }
        public byte State { get; set; }
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            RewardRotationWireValidation.ValidateEntryStateTypeId(TypeId, nameof(ServerRewardRotationEntryStateUpsert));
            writer.Write(TypeId, 3u);
            writer.Write(ContentId);
            writer.Write(RewardTypeId);
            writer.Write(State);
            writer.Write(Value);
        }
    }
}
