using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class WorldRequirements : IWritable
    {
        public byte ItemLevel { get; set; }
        public byte Unused1 { get; set; }
        public byte Unknown { get; set; }
        public byte Unused2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            ulong value = ItemLevel
                | (ulong)Unused1 << 8
                | (ulong)Unknown << 16
                | (ulong)Unused2 << 24;

            writer.Write(value);
        }
    }
}
