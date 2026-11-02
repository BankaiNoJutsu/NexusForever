using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public sealed class BankReconciliationConflictException(Exception cause)
        : InvalidOperationException("Bank reconciliation lost its captured parent state.", cause);

    public sealed record BankSaveReconciliation(string Id, string OperationId, string OutcomeSHA256, bool Committed)
        : CharacterSaveReconciliation(Id)
    {
        private Action<CharacterContext> validateOutcome;
        private Action<CharacterContext> validateEnrolled;
        private Func<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry, bool> ownsMailItem;
        public BankSaveReconciliation(string id, string operation, string outcome, bool committed, Action<CharacterContext> validate, Action<CharacterContext> enrolled, Func<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry, bool> mailOwner)
            : this(id, operation, outcome, committed)
        {
            validateOutcome = validate ?? throw new ArgumentNullException(nameof(validate));
            validateEnrolled = enrolled ?? throw new ArgumentNullException(nameof(enrolled));
            ownsMailItem = mailOwner ?? throw new ArgumentNullException(nameof(mailOwner));
        }
        internal override string Kind => "normal-bank-reconciliation";
        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || !Guid.TryParseExact(OperationId, "N", out var operation) || operation == Guid.Empty || OperationId != operation.ToString("N")
                || Id == OperationId || OutcomeSHA256?.Length != 64 || OutcomeSHA256.Any(c => !"0123456789ABCDEF".Contains(c)) || validateOutcome == null || ownsMailItem == null)
                throw new InvalidOperationException("Bank ordinary save requires its retained complete outcome witness.");
        }
        internal override void ValidateParent(CharacterContext context)
        {
            try { validateOutcome(context); context.BankRowOwner = ownsMailItem; }
            catch (InvalidOperationException error) { throw new BankReconciliationConflictException(error); }
        }
        internal override void ValidateEnrolled(CharacterContext context) => validateEnrolled(context);
        internal override string ReadPayload(CharacterContext context, string id)
            => context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id == id).Select(row => row.Payload).SingleOrDefault();
        internal override object AddReceipt(CharacterContext context, string payload, DateTime created)
        {
            var row = new BankReconciliationReceiptModel { Id = Id, Payload = payload, CreatedUtc = created };
            context.BankReconciliationReceipt.Add(row); return row;
        }
        internal override CharacterReconciliationRolledBackException RolledBack(CharacterContext context, string database, Exception cause, bool completed)
            => new(context, database, Id, cause, completed);
    }
}
