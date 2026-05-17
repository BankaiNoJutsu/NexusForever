using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpendAttributePoints)]
    public class ClientSpendAttributePoints : IReadable
    {
        public List<uint> AttributePoints { get; } = [];

        public void Read(GamePacketReader reader)
        {
            for (int i = 0; i < 6; i++)
                AttributePoints.Add(reader.ReadUInt());
        }
    }
}
