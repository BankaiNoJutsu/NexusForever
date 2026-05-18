namespace NexusForever.Database.Character.Model
{
    public class CharacterSchematicModel
    {
        public ulong Id { get; set; }
        public uint TradeskillSchematic2Id { get; set; }
        public bool Discovered { get; set; }
        public float DiscoveryCoordinateX { get; set; }
        public float DiscoveryCoordinateY { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
