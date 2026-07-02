using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class ItemThresholds : IReadable, IWritable
    {
        public byte Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public ushort Unknown3 { get; set; }
        public ushort Unknown4 { get; set; }

        public void Read(GamePacketReader reader)
        {
            ulong value = reader.ReadULong();

            Unknown1 = (byte)(value & 0xF);
            value >>= 4;
            Unknown2 = (ushort)(value & 0x3FF);
            value >>= 10;
            Unknown3 = (ushort)(value & 0x3FF);
            value >>= 10;
            Unknown4 = (ushort)(value & 0x3FF);
        }

        public void Write(GamePacketWriter writer)
        {
            ulong value = Unknown1
                | (ulong)Unknown2 << 4
                | (ulong)Unknown3 << 14
                | (ulong)Unknown4 << 24;

            writer.Write(value);
        }
    }
}
