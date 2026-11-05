using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NexusForever.Database.Character
{
    public sealed record AppearanceChangeSaveReconciliation : OrdinaryCharacterSaveReconciliation
    {
        public uint Account { get; }
        public ulong Epoch { get; }
        public byte Race { get; }
        public byte Sex { get; }
        public string TargetHash { get; }
        public ulong TokenFee { get; }
        public string AuthRoute { get; }
        public bool Primary { get; }
        private readonly Action<CharacterContext> parent, enrolled;
        private readonly Func<string, string, Task> authorize;
        private readonly Func<string, Task> rollback;

        public AppearanceChangeSaveReconciliation(string id, ulong character, uint account, ulong epoch,
            byte race, byte sex, string targetHash, ulong tokens, string authRoute, bool primary,
            Action<CharacterContext> parent, Action<CharacterContext> enrolled,
            Func<string, string, Task> authorize = null, Func<string, Task> rollback = null) : base(id, character)
        {
            Account = account; Epoch = epoch; Race = race; Sex = sex; TargetHash = targetHash;
            TokenFee = tokens; AuthRoute = authRoute; Primary = primary;
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.enrolled = enrolled ?? throw new ArgumentNullException(nameof(enrolled));
            this.authorize = authorize; this.rollback = rollback;
        }
        internal override string Kind => "normal-appearance-change";
        internal override void Validate()
        {
            base.Validate();
            if (Account == 0 || Epoch == 0 || TargetHash?.Length != 64
                || TokenFee != 0 && (AuthRoute?.Length != 64 || authorize == null || rollback == null))
                throw new InvalidOperationException("Appearance change requires its original admitted payer and exact target.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            base.ValidateParent(context);
            if (context.Character.AsNoTracking().Single(row => row.Id == Character).AccountId != Account)
                throw new InvalidOperationException("Appearance payer no longer owns its locked Character.");
            parent(context);
        }
        internal override void ValidateEnrolled(CharacterContext context) => enrolled(context);
        internal override Task AuthorizeAsync(string payload) => Primary && TokenFee != 0 ? authorize(Id, payload) : Task.CompletedTask;
        internal override Task ProvenRollbackAsync() => Primary && TokenFee != 0 ? rollback(Id) : Task.CompletedTask;
    }
}
