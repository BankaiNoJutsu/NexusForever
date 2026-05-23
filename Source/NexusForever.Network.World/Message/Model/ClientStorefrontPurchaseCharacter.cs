using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x082A. Wire layout from <c>Storefront_PurchaseCharacter_WritePayload</c> @ <c>1400acf80</c>
    /// (filled by <c>Storefront_SendClientPurchaseCharacterOffer</c> @ <c>140450720</c> /
    /// <c>StorefrontLib_PurchaseOffer</c> @ <c>1404f1150</c>):
    /// <c>OfferId</c>, 5-bit <c>PaymentCurrencySlot</c> (Game.Money+0x14),
    /// <c>PurchaseMoneyAmountBits</c> (Game.Money amount),
    /// 14-bit currency type id, <c>PurchaseOptionId</c> (Lua arg 3), <c>Target</c>,
    /// <c>PurchaseExtensionId</c> (Lua arg 4).
    /// NF purchase handlers only validate <see cref="OfferId"/> and <see cref="CurrencyId"/>.
    /// </summary>
    [Message(GameMessageOpcode.ClientStorefrontPurchaseCharacter)]
    public class ClientStorefrontPurchaseCharacter : IReadable
    {
        public uint OfferId { get; private set; }
        /// <summary>5-bit slot from <c>Game.Money</c> (+0x14); not the same as <see cref="PurchaseOptionId"/>.</summary>
        public byte PaymentCurrencySlot { get; private set; }
        /// <summary>Game.Money amount encoded as float bits (client <c>StorefrontLib_PurchaseOffer</c>).</summary>
        public uint PurchaseMoneyAmountBits { get; private set; }
        public ushort CurrencyId { get; private set; }
        /// <summary>Lua <c>PurchaseOffer</c> argument 3 (client option index).</summary>
        public uint PurchaseOptionId { get; private set; }
        public Identity Target { get; } = new();
        /// <summary>Lua <c>PurchaseOffer</c> argument 4; retail/tests usually send zero.</summary>
        public uint PurchaseExtensionId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            OfferId                   = reader.ReadUInt();
            PaymentCurrencySlot       = reader.ReadByte(5);
            PurchaseMoneyAmountBits   = reader.ReadUInt();
            CurrencyId                = reader.ReadUShort(14);
            PurchaseOptionId          = reader.ReadUInt();
            Target.Read(reader);
            PurchaseExtensionId       = reader.ReadUInt();
        }
    }
}
