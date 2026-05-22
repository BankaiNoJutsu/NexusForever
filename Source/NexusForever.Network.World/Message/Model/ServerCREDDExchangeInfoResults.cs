using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// CREDD exchange info snapshot (opcode 0x026A). Layout mapped from
    /// <c>CREDDExchangeInfo_BuildLuaResults</c> (<c>WildStar64.exe</c> <c>14042b9a0</c>):
    /// buy/sell counts, three buy price buckets, three sell price buckets, owned-order count.
    /// Owned-order row pointer data lives outside the fixed 0x50-byte packet when count is non-zero.
    /// </summary>
    [Message(GameMessageOpcode.ServerCREDDExchangeInfoResults)]
    public class ServerCREDDExchangeInfoResults : IWritable
    {
        public const uint PayloadLength = 0x50u;
        public const int PriceBucketCount = 3;

        public uint BuyOrderCount { get; set; }
        public uint SellOrderCount { get; set; }
        public ulong[] BuyOrderPrices { get; } = new ulong[PriceBucketCount];
        public ulong[] SellOrderPrices { get; } = new ulong[PriceBucketCount];
        public uint OwnedOrderCount { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(BuyOrderCount);
            writer.Write(SellOrderCount);

            for (int i = 0; i < PriceBucketCount; i++)
                writer.Write(BuyOrderPrices[i]);

            for (int i = 0; i < PriceBucketCount; i++)
                writer.Write(SellOrderPrices[i]);

            writer.Write(OwnedOrderCount);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0ul);
        }
    }
}
