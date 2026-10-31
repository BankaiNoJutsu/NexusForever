namespace NexusForever.Database.Auth.Model
{
    /// <summary>Stable creation identity retained after account-item consumption.</summary>
    public sealed class AccountInventoryReceiptModel
    {
        public uint AccountId { get; set; }
        public string DeliveryKey { get; set; }
        public ulong InventoryId { get; set; }
        public AccountModel Account { get; set; }
    }
}
