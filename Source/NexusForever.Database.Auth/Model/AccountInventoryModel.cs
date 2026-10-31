using System;

namespace NexusForever.Database.Auth.Model
{
    public class AccountInventoryModel
    {
        public uint Id { get; set; }
        public ulong InventoryId { get; set; }
        public uint AccountItemId { get; set; }
        public byte ClaimState { get; set; }
        public bool HasTargetPlayerIdentity { get; set; }
        public bool ForceSoulbind { get; set; }
        public string CreationKey { get; set; }
        public ushort TargetRealmId { get; set; }
        public ulong TargetCharacterId { get; set; }
        public DateTime CreateTime { get; set; }

        public virtual AccountModel Account { get; set; }
    }
}
