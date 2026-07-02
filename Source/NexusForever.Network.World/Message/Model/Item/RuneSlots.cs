using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class RuneSlots : IReadable, IWritable
    {
        public byte Unknown1 { get; set; }
        public byte Unknown2 { get; set; }
        public bool Unknown3 { get; set; }
        public byte[] CraftingGroup { get; set; } = new byte[8];

        public void Read(GamePacketReader reader)
        {
            uint value = reader.ReadUInt();

            Unknown1 = (byte)(value & 0x7);
            value >>= 3;
            Unknown2 = (byte)(value & 0x7);
            value >>= 3;
            Unknown3 = (value & 0x1) != 0;

            value >>= 1;
            for (int i = 0; i < CraftingGroup.Length; i++)
            {
                CraftingGroup[i] = (byte)(value & 0x7);
                value >>= 3;
            }
        }

        public void Write(GamePacketWriter writer)
        {
            uint value = Unknown1
                | (uint)Unknown2 << 3
                | (uint)(Unknown3 ? 1 : 0) << 6;

            for (int i = 0; i < CraftingGroup.Length; i++)
                value |= (uint)CraftingGroup[i] << (7 + i * 3);

            writer.Write(value);
        }
    }
}
