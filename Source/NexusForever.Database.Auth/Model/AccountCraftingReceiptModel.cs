namespace NexusForever.Database.Auth.Model
{
    public sealed class AccountCraftingReceiptModel
    {
        public string Id { get; set; }
        public uint AccountId { get; set; }
        public ushort RealmId { get; set; }
        public string PayloadJson { get; set; }
        public bool Delivered { get; set; }
        public bool Cancelled { get; set; }
        public bool Completed { get; set; }
    }
}
