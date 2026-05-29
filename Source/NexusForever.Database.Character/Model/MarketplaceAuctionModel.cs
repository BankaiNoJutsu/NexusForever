namespace NexusForever.Database.Character.Model
{
    public class MarketplaceAuctionModel
    {
        public ulong Id { get; set; }
        public ulong OwnerCharacterId { get; set; }
        public ulong ItemId { get; set; }
        public ulong MinimumBid { get; set; }
        public ulong BuyoutPrice { get; set; }
        public ulong CurrentBid { get; set; }
        public ulong TopBidderCharacterId { get; set; }
        public ulong ExpirationTime { get; set; }
        public uint Item2Id { get; set; }
        public uint Quantity { get; set; }
        public uint WorldRequirementItem2Id { get; set; }
        public ulong CircuitData { get; set; }
        public uint GlyphData { get; set; }
        public ulong ThresholdData { get; set; }
        public uint Unknown2 { get; set; }
        public string MicrochipIds { get; set; }

        public ItemModel Item { get; set; }
    }
}
