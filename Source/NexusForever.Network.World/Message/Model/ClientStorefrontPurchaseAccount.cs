using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x0828. Shared purchase payload (see <see cref="ClientStorefrontPurchaseCharacter"/>)
    /// from <c>Storefront_PurchaseAccount_WritePayload</c> @ <c>140080a20</c>, then account-only
    /// trailing uint32, <see cref="AccountTarget"/>, and <see cref="RecipientName"/>.
    /// </summary>
    [Message(GameMessageOpcode.ClientStorefrontPurchaseAccount)]
    public class ClientStorefrontPurchaseAccount : IReadable
    {
        public uint OfferId { get; private set; }
        public byte PaymentCurrencySlot { get; private set; }
        public uint PurchaseMoneyAmountBits { get; private set; }
        public ushort CurrencyId { get; private set; }
        public uint PurchaseOptionId { get; private set; }
        public Identity Target { get; } = new();
        public uint PurchaseExtensionId { get; private set; }
        /// <summary>
        /// Account-route u32 after shared payload (<c>Storefront_PurchaseAccount_WritePayload</c> @ <c>140080a20</c>
        /// reads struct <c>+0x30</c>). Filled from the same Lua <c>PurchaseOffer</c> arg 4 as
        /// <see cref="PurchaseExtensionId"/> in <c>Storefront_SendClientPurchaseAccountOffer</c> @ <c>1404507e0</c>.
        /// NF purchase handlers do not read this field.
        /// </summary>
        public uint AccountPurchaseExtensionId { get; private set; }
        public Identity AccountTarget { get; } = new();
        public string RecipientName { get; private set; }

        public void Read(GamePacketReader reader)
        {
            OfferId                 = reader.ReadUInt();
            PaymentCurrencySlot     = reader.ReadByte(5u);
            PurchaseMoneyAmountBits = reader.ReadUInt();
            CurrencyId              = reader.ReadUShort(14u);
            PurchaseOptionId        = reader.ReadUInt();
            Target.Read(reader);
            PurchaseExtensionId     = reader.ReadUInt();
            AccountPurchaseExtensionId = reader.ReadUInt();
            AccountTarget.Read(reader);
            RecipientName           = reader.ReadWideString();
        }
    }
}
