using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public class AccountItemCooldown
    {
        [Flags]
        private enum AccountItemCooldownSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Modify = 0x0002
        }

        public uint AccountId { get; }
        public uint CooldownGroupId { get; }
        public DateTime? TimeUsed { get; private set; }
        public uint Duration { get; private set; }

        private AccountItemCooldownSaveMask saveMask;

        public AccountItemCooldown(AccountItemCooldownModel model)
        {
            AccountId       = model.Id;
            CooldownGroupId = model.CooldownGroupId;
            TimeUsed        = model.Timestamp;
            Duration        = model.Duration;

            saveMask = AccountItemCooldownSaveMask.None;
        }

        public AccountItemCooldown(uint accountId, uint cooldownGroupId)
        {
            AccountId       = accountId;
            CooldownGroupId = cooldownGroupId;

            saveMask = AccountItemCooldownSaveMask.Create;
        }

        public void Save(AuthContext context)
        {
            if (saveMask == AccountItemCooldownSaveMask.None)
                return;

            AccountItemCooldownModel model = BuildModel();

            if ((saveMask & AccountItemCooldownSaveMask.Create) != 0)
            {
                context.Add(model);
            }
            else if ((saveMask & AccountItemCooldownSaveMask.Modify) != 0)
            {
                EntityEntry<AccountItemCooldownModel> entity = context.Attach(model);
                entity.Property(p => p.Timestamp).IsModified = true;
                entity.Property(p => p.Duration).IsModified  = true;
            }

            saveMask = AccountItemCooldownSaveMask.None;
        }

        public void TriggerWithDuration(uint duration)
        {
            Duration = duration;
            TimeUsed = DateTime.UtcNow;

            if ((saveMask & AccountItemCooldownSaveMask.Create) == 0)
                saveMask |= AccountItemCooldownSaveMask.Modify;
        }

        public uint GetRemainingDuration()
        {
            if (TimeUsed == null || Duration == 0u)
                return 0u;

            double elapsed = DateTime.UtcNow.Subtract(TimeUsed.Value).TotalSeconds;
            if (elapsed >= Duration)
                return 0u;

            return Duration - (uint)elapsed;
        }

        public ServerAccountItemCooldownSet Build()
        {
            return new ServerAccountItemCooldownSet
            {
                AccountItemCooldownGroup = CooldownGroupId,
                CooldownInSeconds        = GetRemainingDuration()
            };
        }

        private AccountItemCooldownModel BuildModel()
        {
            return new AccountItemCooldownModel
            {
                Id              = AccountId,
                CooldownGroupId = CooldownGroupId,
                Timestamp       = TimeUsed,
                Duration        = Duration
            };
        }
    }
}
