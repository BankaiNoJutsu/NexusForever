namespace NexusForever.Database.Character.Model
{
    public class MarketplaceCommodityOrderModel
    {
        public ulong Id { get; set; }
        public ulong OwnerCharacterId { get; set; }
        public uint Item2Id { get; set; }
        public uint Quantity { get; set; }
        public ulong PricePerUnit { get; set; }
        public ulong Price { get; set; }
        public bool IsBuyOrder { get; set; }
        public bool ForceImmediate { get; set; }
        public ulong ListTime { get; set; }
        public ulong ExpirationTime { get; set; }
    }
}
