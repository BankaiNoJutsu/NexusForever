namespace NexusForever.Aspire.Database.Migrations.Configuration.Model
{
    public class AccountCreationOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public uint? RoleId { get; set; }
        public List<AccountCreationAccountOptions> Accounts { get; set; } = [];
    }

    public class AccountCreationAccountOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public uint? RoleId { get; set; }
    }
}
