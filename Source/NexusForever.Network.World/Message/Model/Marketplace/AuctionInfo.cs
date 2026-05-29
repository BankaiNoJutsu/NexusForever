using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Marketplace
{
    public class AuctionInfo : IReadable, IWritable
    {
        public ulong AuctionId { get; set; }
        public ulong OwnerCharacterId { get; set; }
        public ulong MinimumBid { get; set; }
        public ulong BuyoutPrice { get; set; }
        public ulong CurrentBid { get; set; }
        public ulong TopBidderCharacterId { get; set; }
        public ulong ExpirationTime { get; set; } // in seconds from now
        public uint Item2Id { get; set; }
        public uint Quantity { get; set; }
        public uint WorldRequirement_Item2Id { get; set; }
        /// <summary>
        /// Shared-item microchip id list (3-bit count on wire). Matches
        /// <see cref="Shared.Item.Microchips"/> beside circuit/glyph payload.
        /// </summary>
        public List<uint> MicrochipIds { get; set; } = [];
        public ulong CircuitData { get; set; }
        public uint GlyphData { get; set; }
        public ulong ThresholdData { get; set; }
        public uint Unknown2 { get; set; }

        public void Read(GamePacketReader reader)
        {
            AuctionId = reader.ReadULong();
            OwnerCharacterId = reader.ReadULong();
            MinimumBid = reader.ReadULong();
            BuyoutPrice = reader.ReadULong();
            CurrentBid = reader.ReadULong();
            TopBidderCharacterId = reader.ReadULong();
            ExpirationTime = reader.ReadULong();
            Item2Id = reader.ReadUInt(18u);
            Quantity = reader.ReadUInt();
            WorldRequirement_Item2Id = reader.ReadUInt(18u);

            uint count = reader.ReadUInt(3u);
            for(uint i = 0; i < count; i++)
            {
                MicrochipIds.Add(reader.ReadUInt());
            }

            CircuitData = reader.ReadULong();
            GlyphData = reader.ReadUInt();
            ThresholdData = reader.ReadULong();
            Unknown2 = reader.ReadUInt();
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(AuctionId);
            writer.Write(OwnerCharacterId);
            writer.Write(MinimumBid);
            writer.Write(BuyoutPrice);
            writer.Write(CurrentBid);
            writer.Write(TopBidderCharacterId);
            writer.Write(ExpirationTime);
            writer.Write(Item2Id, 18u);
            writer.Write(Quantity);
            writer.Write(WorldRequirement_Item2Id, 18u);

            writer.Write(MicrochipIds.Count, 3u);
            MicrochipIds.ForEach(microchipId => writer.Write(microchipId));

            writer.Write(CircuitData);
            writer.Write(GlyphData);
            writer.Write(ThresholdData);
            writer.Write(Unknown2);
        }
    }
}
