namespace NexusForever.Database.Character.Model
{
    public class RealmBankItemModel
    {
        public ulong Id { get; set; }
        public uint AccountId { get; set; }
        public ushort RealmId { get; set; }
        public uint ItemId { get; set; }
        public uint BagIndex { get; set; }
        public uint StackCount { get; set; }
        public uint Charges { get; set; }
        public float Durability { get; set; }
        public uint ExpirationTimeLeft { get; set; }
        public bool Soulbound { get; set; }
    }
}
