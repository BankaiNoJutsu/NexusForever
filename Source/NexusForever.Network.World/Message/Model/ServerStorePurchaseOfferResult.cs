using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Store purchase-offer result for opcode 0x098C. The client dispatches
    /// <c>StorePurchaseOfferResult</c>; opcode 0x098D shares the reader and event path.
    /// </summary>
    [Message(GameMessageOpcode.ServerStorePurchaseOfferResult)]
    public class ServerStorePurchaseOfferResult : IWritable
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
