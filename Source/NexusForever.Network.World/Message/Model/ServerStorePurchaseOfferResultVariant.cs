using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Account-route store purchase-offer result (opcode 0x098D). Wire shape and client
    /// consumer match <see cref="ServerStorePurchaseOfferResult"/> via
    /// <c>Storefront_HandleStorePurchaseOfferResult</c> (<c>14044c780</c>).
    /// Opcode variant is verified by paired client senders: character <c>0x082A</c>
    /// (<c>Storefront_SendClientPurchaseCharacterOffer</c> @ <c>140450720</c>) vs account
    /// <c>0x0828</c> (<c>Storefront_SendClientPurchaseAccountOffer</c> @ <c>1404507e0</c>).
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
