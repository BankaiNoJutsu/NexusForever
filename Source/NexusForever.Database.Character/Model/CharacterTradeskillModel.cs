namespace NexusForever.Database.Character.Model
{
    public class CharacterTradeskillModel
    {
        public ulong Id { get; set; }
        public uint TradeskillId { get; set; }
        public uint TradeskillXp { get; set; }
        public uint IsActive { get; set; }
        public uint PropertyProficiencyFlags { get; set; }
        public uint TalentPoints { get; set; }
        public uint TalentTier00 { get; set; }
        public uint TalentTier01 { get; set; }
        public uint TalentTier02 { get; set; }
        public uint TalentTier03 { get; set; }
        public uint TalentTier04 { get; set; }
        public uint TalentTier05 { get; set; }
        public uint TalentTier06 { get; set; }
        public uint TalentTier07 { get; set; }
        public uint TalentTier08 { get; set; }
        public uint TalentTier09 { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
