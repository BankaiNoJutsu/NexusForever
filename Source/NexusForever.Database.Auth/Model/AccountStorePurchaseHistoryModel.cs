using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountStorePurchaseHistoryModel
    {
        public ulong Id { get; set; }
        public uint AccountId { get; set; }
        public uint OfferId { get; set; }
        public ushort CurrencyId { get; set; }
        public ulong Price { get; set; }
        public DateTime PurchasedUtc { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
