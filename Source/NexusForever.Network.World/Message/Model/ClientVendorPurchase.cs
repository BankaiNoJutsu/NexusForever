using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientVendorPurchase)]
    public class ClientVendorPurchase : IReadable
    {
        public uint StockUniqueId { get; private set; }
        public uint PurchaseQuantity { get; private set; }

        public void Read(GamePacketReader reader)
        {
            StockUniqueId = reader.ReadUInt();
            PurchaseQuantity = reader.ReadUInt();
        }
    }
}
