using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Storefront purchase-history request (opcode 0x082E). Mapped from
    /// the <c>StorefrontLib</c> registration table entry <c>RequestHistory</c>
    /// (<c>WildStar64.exe</c> <c>140b69e10</c> -> <c>1404f1d50</c>).
    /// </summary>
    [Message(GameMessageOpcode.ClientStorefrontRequestPurchaseHistory)]
    public class ClientStorefrontRequestPurchaseHistory : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }
}
