using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAttributePoints)]
    public class ServerAttributePoints : IWritable
    {
        public uint AttributePoints { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(AttributePoints);
        }
    }
}
