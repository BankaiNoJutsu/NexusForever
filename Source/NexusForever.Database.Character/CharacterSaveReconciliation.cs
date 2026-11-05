using System;
using System.Threading.Tasks;

namespace NexusForever.Database.Character
{
    // The two concrete economic parents share complete ordinary-save recovery.
    // Their receipts and parent validation remain in their own domains.
    public abstract record CharacterSaveReconciliation(string Id)
    {
        internal CharacterBidSaveRetry Retry { get; } = new();
        internal abstract string Kind { get; }
        internal abstract void Validate();
        internal abstract void ValidateParent(CharacterContext context);
        internal virtual void ValidateEnrolled(CharacterContext context) { }
        // Enrollment remains synchronous. A specifically typed external payer
        // may authorize only the already sealed graph while its SQL lock is held.
        internal virtual Task AuthorizeAsync(string payload) => Task.CompletedTask;
        internal virtual Task ProvenRollbackAsync() => Task.CompletedTask;
        internal abstract string ReadPayload(CharacterContext context, string id);
        internal abstract object AddReceipt(CharacterContext context, string payload, DateTime created);
        internal abstract CharacterReconciliationRolledBackException RolledBack(CharacterContext context, string database, Exception cause, bool completed);
    }

    public class CharacterReconciliationRolledBackException : Exception
    {
        public CharacterContext Context { get; }
        public string ReceiptId { get; }
        public bool EnrollmentCompleted { get; }
        public string ContextIdentity { get; }
        public string DatabaseFingerprint { get; }
        public ulong CharacterId { get; }
        internal CharacterReconciliationRolledBackException(CharacterContext context, string database, string receiptId, Exception cause, bool completed)
            : base("The Character save is proven rolled back; its original frozen actor graph may retry.", cause)
        {
            Context = context; ReceiptId = receiptId; EnrollmentCompleted = completed;
            ContextIdentity = context?.ContextId.ToString(); DatabaseFingerprint = database == null ? null : MarketplaceBidReconciliation.Hash(database);
            CharacterId = context?.AtomicCharacterId ?? 0;
        }
    }
}
