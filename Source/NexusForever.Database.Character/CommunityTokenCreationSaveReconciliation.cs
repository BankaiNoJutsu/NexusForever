using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NexusForever.Database.Character
{
    public sealed record CommunityTokenCreationSaveReconciliation : CommunityCreationSaveReconciliation
    {
        public uint Account { get; }
        public ulong Epoch { get; }
        public ulong TokenFee { get; }
        public bool Primary { get; }
        public string AuthRoute { get; }
        private readonly Func<string, string, Task> authorize;
        private readonly Func<string, Task> rollback;

        public CommunityTokenCreationSaveReconciliation(string id, ulong character, ulong guild, ulong residence, string name,
            uint account, ulong epoch, ulong fee, bool primary, string authRoute, Action<CharacterContext> parent, Action<CharacterContext> enrolled,
            Func<EntityEntry, bool> row, Func<string, string, Task> authorize, Func<string, Task> rollback)
            : base(id, character, guild, residence, name, parent, enrolled, row)
        {
            Account = account; Epoch = epoch; TokenFee = fee; Primary = primary;
            AuthRoute = authRoute;
            this.authorize = authorize ?? throw new ArgumentNullException(nameof(authorize));
            this.rollback = rollback ?? throw new ArgumentNullException(nameof(rollback));
        }
        internal override string Kind => "community-creation-tokens";
        internal override void Validate()
        {
            base.Validate();
            if (Account == 0 || Epoch == 0 || TokenFee == 0 || AuthRoute?.Length != 64)
                throw new InvalidOperationException("Community token creation requires its admitted payer and accepted fee.");
        }
        internal override Task AuthorizeAsync(string payload) => Primary ? authorize(Id, payload) : Task.CompletedTask;
        internal override Task ProvenRollbackAsync() => Primary ? rollback(Id) : Task.CompletedTask;
        internal override void ValidateParent(CharacterContext context)
        {
            base.ValidateParent(context);
            // CharacterAtomicSave already holds this row's serializable owner
            // lock; the admitted Auth payer must still own that SQL Character.
            if (context.Character.AsNoTracking().SingleOrDefault(row => row.Id == Character)?.AccountId != Account)
                throw new InvalidOperationException("Community token payer no longer owns its locked Character.");
        }
    }
}
