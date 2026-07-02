using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class CraftStats : IReadable, IWritable
    {
        public Property[] StatType { get; set; } = new Property[5];
        public byte Unknown { get; set; }
        public byte ApSpSplit { get; set; }
        public uint CircuitComplete { get; set; }

        public void Read(GamePacketReader reader)
        {
            ulong value = reader.ReadULong();

            for (int i = 0; i < StatType.Length; i++)
            {
                StatType[i] = (Property)(value & 0xFF);
                value >>= 8;
            }

            value >>= 8;
            Unknown = (byte)(value & 0xFF);

            value >>= 8;
            ApSpSplit = (byte)(value & 0xFF);

            value >>= 8;
            CircuitComplete = (uint)(value & 0xFFFFFFFF);
        }

        public void Write(GamePacketWriter writer)
        {
            ulong value = 0;

            for (int i = 0; i < StatType.Length; i++)
            {
                value |= (ulong)StatType[i];
                value <<= 8;
            }

            value |= Unknown;
            value <<= 8;
            value |= ApSpSplit;
            value <<= 8;
            value |= CircuitComplete;
            value <<= 8;

            writer.Write(value);
        }
    }
}
