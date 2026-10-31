namespace NexusForever.Database.Auth.Model
{
    // Append-only wallet receipts survive account removal. A retained operation
    // must never become grantable again merely because its owner was deleted.
    public class AccountCurrencyOperationModel
    {
        public string OperationKey { get; set; }
        public uint AccountId { get; set; }
        public byte CurrencyId { get; set; }
        public ulong Amount { get; set; }
        public bool Subtract { get; set; }
        public ulong BeforeAmount { get; set; }
        public ulong AfterAmount { get; set; }
        public string SourceJson { get; set; }
    }
}
