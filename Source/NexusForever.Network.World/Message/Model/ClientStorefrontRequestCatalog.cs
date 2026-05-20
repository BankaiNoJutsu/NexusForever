using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientStorefrontRequestCatalog)]
    public class ClientStorefrontRequestCatalog : IReadable
    {
        public ushort CatalogContext { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CatalogContext = reader.ReadUShort(14u);
        }
    }
}
