using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientConvertResource)]
    public class ClientConvertResource : IReadable
    {
        public uint ConversionId { get; private set; }
        public uint Selector { get; private set; }
        public ulong ResourceField { get; private set; }

        public void Read(GamePacketReader reader)
        {
            // Native writer 1400890c0 (registration 14006c290, opcode 0x00D7)
            // packs the table id into fourteen bits before the existing fields.
            ConversionId = reader.ReadUInt(14u);
            Selector = reader.ReadUInt();
            ResourceField = reader.ReadULong();
        }
    }
}
