using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07CA.
    // Wire format: 32-bit count followed by count schedule rows.
    // Each row matches RewardRotation_ScheduleRow_ReadPayload at client address 1400a1d90:
    // 32-bit ContentId, 14-bit RewardKeyId, float Duration, 8-bit RewardType, 32-bit Value.
    [Message(GameMessageOpcode.ServerRewardRotationScheduleArray)]
    public class ServerRewardRotationScheduleArray : IWritable
    {
        public class ScheduleRow : IWritable
        {
            public uint ContentId { get; set; }
            public uint RewardKeyId { get; set; }
            public float Duration { get; set; }
            public byte RewardType { get; set; }
            public uint Value { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(ContentId);
                writer.Write(RewardKeyId, 14u);
                writer.Write(Duration);
                writer.Write(RewardType);
                writer.Write(Value);
            }
        }

        public List<ScheduleRow> Entries { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Entries.Count);
            Entries.ForEach(e => e.Write(writer));
        }
    }
}
