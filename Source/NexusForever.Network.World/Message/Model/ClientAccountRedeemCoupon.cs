using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Coupon redemption request (opcode 0x0790). Wire shape is a single wide string coupon code.
    /// </summary>
    [Message(GameMessageOpcode.ClientAccountRedeemCoupon)]
    public class ClientAccountRedeemCoupon : IReadable
    {
        public string CouponCode { get; private set; } = string.Empty;

        public void Read(GamePacketReader reader)
        {
            CouponCode = reader.ReadWideString();
        }
    }
}
