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
            ConversionId = reader.ReadUInt();
            Selector = reader.ReadUInt();
            ResourceField = reader.ReadULong();
        }
    }
}