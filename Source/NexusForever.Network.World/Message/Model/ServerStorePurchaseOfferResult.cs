using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Character-route store purchase-offer result (opcode 0x098C). The client dispatches
    /// <c>StorePurchaseOfferResult</c> via <c>Storefront_HandleStorePurchaseOfferResult</c>
    /// (<c>14044c780</c>). Paired with account-route <see cref="ServerStorePurchaseOfferResultVariant"/>
    /// (client sender <c>0x0828</c> vs <c>0x082A</c>).
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
