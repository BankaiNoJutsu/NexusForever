namespace NexusForever.Database.Auth.Model
{
    public class AccountCREDDOrderModel
    {
        public ulong OrderId { get; set; }
        public uint AccountId { get; set; }
        public ulong CharacterId { get; set; }
        public ushort RealmId { get; set; }
        public ulong CreditAmount { get; set; }
        public bool IsBuyOrder { get; set; }
    }
}
