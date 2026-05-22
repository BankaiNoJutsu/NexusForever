using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Virtual-currency package purchase request (opcode 0x082E). Mapped from
    /// <c>Storefront_SendClientPurchaseVirtualCurrencyPackage</c> (<c>WildStar64.exe</c> <c>1404f1d50</c>),
    /// which sends one package-id byte.
    /// </summary>
    [Message(GameMessageOpcode.ClientStorefrontPurchaseVirtualCurrencyPackage)]
    public class ClientStorefrontPurchaseVirtualCurrencyPackage : IReadable
    {
        public byte PackageId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            PackageId = reader.ReadByte();
        }
    }
}
