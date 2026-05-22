using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountPendingItemModel
    {
        public uint Id { get; set; }
        public ulong PendingItemId { get; set; }
        public string GroupName { get; set; }
        public uint AccountItemId { get; set; }
        public uint SenderAccountId { get; set; }
        public ushort SenderRealmId { get; set; }
        public ulong SenderCharacterId { get; set; }
        public ushort TargetRealmId { get; set; }
        public ulong TargetCharacterId { get; set; }
        public byte ClaimState { get; set; }
        public bool Unknown1 { get; set; }
        public DateTime CreateTime { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
