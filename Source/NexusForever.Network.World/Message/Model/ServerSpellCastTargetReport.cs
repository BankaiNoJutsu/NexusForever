using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Likely miss, immunity, or invalid-target follow-up packet for a spell cast.
    /// The current model is intentionally structural until the exact target-result semantics are proven.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellCastTargetReport)]
    public class ServerSpellCastTargetReport : IWritable
    {
        public class TargetReportRow : IWritable
        {
            public uint CasterId { get; set; }
            public byte Unknown4 { get; set; } = 0;
            public uint Unknown5 { get; set; } = 0;

            public void Write(GamePacketWriter writer)
            {
                writer.Write(CasterId);
                writer.Write(Unknown4, 4u);
                writer.Write(Unknown5);
            }
        }
        public uint CastingId { get; set; }

        public List<TargetReportRow> TargetReportRows { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CastingId);
            writer.Write(TargetReportRows.Count, 32u);
            TargetReportRows.ForEach(u => u.Write(writer));
        }
    }
}
