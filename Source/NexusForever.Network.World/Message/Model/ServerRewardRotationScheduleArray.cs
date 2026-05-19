using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07CA.
    // Wire format: 32-bit count followed by count schedule rows.
    // RewardRotation_ApplyServerScheduleUpdate consumes the decoded row as:
    // 32-bit RewardKeyId, 14-bit ContentId, float Duration, 8-bit RewardType, 32-bit Value.
    [Message(GameMessageOpcode.ServerRewardRotationScheduleArray)]
    public class ServerRewardRotationScheduleArray : IWritable
    {
        public class ScheduleRow : IWritable
        {
            public uint RewardKeyId { get; set; }
            public uint ContentId { get; set; }
            public float Duration { get; set; }
            public byte RewardType { get; set; }
            public uint Value { get; set; }

            public void Write(GamePacketWriter writer)
            {
                RewardRotationWireValidation.ValidateScheduleRow(this, $"{nameof(ServerRewardRotationScheduleArray)} row");
                writer.Write(RewardKeyId);
                writer.Write(ContentId, 14u);
                writer.Write(Duration);
                writer.Write(RewardType);
                writer.Write(Value);
            }
        }

        public List<ScheduleRow> Entries { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                ScheduleRow entry = Entries[i] ?? throw new InvalidOperationException($"{nameof(ServerRewardRotationScheduleArray)} entry {i} is null.");
                entry.Write(writer);
            }
        }
    }
}
