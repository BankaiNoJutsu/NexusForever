using NexusForever.Game.Static.Loot;

namespace NexusForever.Database.World.Model
{
    public class ItemSalvageModel
    {
        public ItemSalvagePurpose Purpose { get; set; }
        public uint SourceItemId { get; set; }
        public uint SourceItem2TypeId { get; set; }
        public uint SourceLevel { get; set; }
        public uint Type { get; set; }
        public uint StaticId { get; set; }
        public float Probability { get; set; }
        public uint MinCount { get; set; }
        public uint MaxCount { get; set; }
        public string Comment { get; set; }
    }
}
