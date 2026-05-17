namespace NexusForever.Database.World.Model
{
    public class CreatureLootModel
    {
        public uint CreatureId { get; set; }
        public uint ItemId { get; set; }
        public decimal Chance { get; set; }
        public uint DropTimes { get; set; }
        public uint AggregateDropSum { get; set; }
        public uint AggregateDropCount { get; set; }
        public uint GameVersion { get; set; }
        public uint SourceDropId { get; set; }
        public uint VersionedItemDropAggregateId { get; set; }
        public uint VersionedCreatureDropAggregateId { get; set; }
        public uint LastSeenIn { get; set; }
        public string MatchStatus { get; set; }
        public string SourceName { get; set; }
        public string ItemName { get; set; }
    }
}
