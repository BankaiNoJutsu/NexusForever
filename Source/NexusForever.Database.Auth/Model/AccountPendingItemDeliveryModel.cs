namespace NexusForever.Database.Auth.Model
{
    /// <summary>
    /// A durable append receipt, retained after the pending row is consumed.
    /// PendingItemId zero is reserved for an Auth save-unit commit witness.
    /// </summary>
    public sealed class AccountPendingItemDeliveryModel
    {
        public uint AccountId { get; set; }
        public string DeliveryKey { get; set; }
        public ulong PendingItemId { get; set; }
        public AccountModel Account { get; set; }
    }
}
