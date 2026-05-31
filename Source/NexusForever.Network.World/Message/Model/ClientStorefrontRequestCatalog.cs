using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Client request for storefront catalog refresh. Native write evidence keeps this packet at one
    /// 14-bit catalog selector payload.
    /// </summary>
    [Message(GameMessageOpcode.ClientStorefrontRequestCatalog)]
    public class ClientStorefrontRequestCatalog : IReadable
    {
        /// <summary>
        /// Fourteen-bit catalog selector from <c>Storefront_RequestCatalog_WritePayload</c> (<c>14007a610</c>).
        /// </summary>
        public ushort CatalogContext { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CatalogContext = reader.ReadUShort(14u);
        }
    }
}
