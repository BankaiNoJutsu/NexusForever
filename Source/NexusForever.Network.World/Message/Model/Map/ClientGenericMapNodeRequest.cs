using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Map
{
    [Message(GameMessageOpcode.ClientGenericMapNodeRequest)]
    public class ClientGenericMapNodeRequest : IReadable
    {
        public ushort GenericMapNodeId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            GenericMapNodeId = reader.ReadUShort(14u);
        }
    }
}
