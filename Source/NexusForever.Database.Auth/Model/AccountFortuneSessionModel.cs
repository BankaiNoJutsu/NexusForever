namespace NexusForever.Database.Auth.Model
{
    public class AccountFortuneSessionModel
    {
        public uint Id { get; set; }
        public uint Card0AccountItemId { get; set; }
        public uint Card1AccountItemId { get; set; }
        public uint Card2AccountItemId { get; set; }
        public byte Card0Rarity { get; set; }
        public byte Card1Rarity { get; set; }
        public byte Card2Rarity { get; set; }
        public bool Card0Flipped { get; set; }
        public bool Card1Flipped { get; set; }
        public bool Card2Flipped { get; set; }
        public bool Card0Granted { get; set; }
        public bool Card1Granted { get; set; }
        public bool Card2Granted { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
