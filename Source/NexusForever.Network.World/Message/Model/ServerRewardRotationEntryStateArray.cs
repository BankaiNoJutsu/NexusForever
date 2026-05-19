using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07C8.
    // Wire format: 32-bit count followed by count entry-state rows.
    // Each row matches RewardRotation_EntryStateRow_ReadPayload at client address 1400a1ee0.
    [Message(GameMessageOpcode.ServerRewardRotationEntryStateArray)]
    public class ServerRewardRotationEntryStateArray : IWritable
    {
        public class EntryStateRow : IWritable
        {
            public uint TypeId { get; set; }
            public uint ContentId { get; set; }
            public uint RewardTypeId { get; set; }
            public byte State { get; set; }
            public uint Value { get; set; }

            public void Write(GamePacketWriter writer)
            {
                RewardRotationWireValidation.ValidateEntryStateTypeId(TypeId, $"{nameof(ServerRewardRotationEntryStateArray)} entry row");
                writer.Write(TypeId, 3u);
                writer.Write(ContentId);
                writer.Write(RewardTypeId);
                writer.Write(State);
                writer.Write(Value);
            }
        }

        public List<EntryStateRow> Entries { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                EntryStateRow entry = Entries[i] ?? throw new InvalidOperationException($"{nameof(ServerRewardRotationEntryStateArray)} entry {i} is null.");
                entry.Write(writer);
            }
        }
    }
}
