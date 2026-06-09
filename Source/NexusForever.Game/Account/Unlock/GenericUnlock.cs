using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Account.Costume;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Account.Unlock
{
    public class GenericUnlock : IGenericUnlock
    {
        public GenericUnlockEntryEntry Entry { get; }
        public GenericUnlockType Type => (GenericUnlockType)Entry.GenericUnlockTypeEnum;

        private readonly IAccount account;
        private bool isDirty;

        /// <summary>
        /// Create a new <see cref="IGenericUnlock"/> from supplied <see cref="GenericUnlockEntryEntry"/>.
        /// </summary>
        public GenericUnlock(IAccount account, GenericUnlockEntryEntry entry)
            : this(account, entry, true)
        {
        }

        internal GenericUnlock(IAccount account, GenericUnlockEntryEntry entry, bool isDirty)
        {
            this.account = account;
            Entry        = entry;
            this.isDirty = isDirty;
        }

        public void Save(AuthContext context)
        {
            if (!isDirty)
                return;

            var model = new AccountGenericUnlockModel
            {
                Id    = account.Id,
                Entry = Entry.Id
            };

            context.Add(model);
            isDirty = false;
        }
    }
}
