using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountCREDDHistoryModel
    {
        public ulong Id { get; set; }
        public uint AccountId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public uint Operation { get; set; }
        public bool IsInitiator { get; set; }
        public ushort IdentityRealmId { get; set; }
        public ulong IdentityCharacterId { get; set; }
        public ushort CounterpartyRealmId { get; set; }
        public ulong CounterpartyCharacterId { get; set; }
        public ulong CreditAmount { get; set; }
    }
}
