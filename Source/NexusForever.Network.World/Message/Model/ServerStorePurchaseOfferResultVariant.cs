using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Alternate store purchase-offer result opcode (0x098D). Wire shape and client
    /// consumer match <see cref="ServerStorePurchaseOfferResult"/>; variant semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerStorePurchaseOfferResultVariant)]
    public class ServerStorePurchaseOfferResultVariant : IWritable
    {
        public bool IsSuccess { get; set; }
        public PurchaseResultDisplayType DisplayType { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(IsSuccess);
            writer.Write(DisplayType, 5u);
        }
    }
}
