namespace NexusForever.Database.Character.Model
{
    // No Character FK: a paid award survives deletion of its source Character.
    public sealed class CharacterCraftingAccountOutboxModel
    {
        public string Id { get; set; }
        public ulong CharacterId { get; set; }
        public uint AccountId { get; set; }
        public ushort RealmId { get; set; }
        public string PayloadJson { get; set; }
        public bool Delivered { get; set; }
        public bool Cancelled { get; set; }
    }
}
