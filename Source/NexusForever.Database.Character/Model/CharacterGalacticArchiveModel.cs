namespace NexusForever.Database.Character.Model
{
    public class CharacterGalacticArchiveModel
    {
        public ulong Id { get; set; }
        public uint ArchiveArticleId { get; set; }
        public uint UnlockedFlags { get; set; }
        public uint ViewedFlags { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
