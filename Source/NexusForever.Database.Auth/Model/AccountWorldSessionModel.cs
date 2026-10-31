namespace NexusForever.Database.Auth.Model
{
    public sealed class AccountWorldSessionModel
    {
        public uint AccountId { get; set; }
        public ulong Epoch { get; set; }
        public AccountModel Account { get; set; }
    }
}
