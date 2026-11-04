using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public sealed record ResourceConversionSaveReconciliation(string Id, ulong Character) : OrdinaryCharacterSaveReconciliation(Id, Character)
    {
        internal override string Kind => "normal-resource-conversion";
    }

    public sealed record PublicEventQuestSaveReconciliation(string Id, ulong Character) : OrdinaryCharacterSaveReconciliation(Id, Character)
    {
        internal override string Kind => "normal-public-event-quest";
    }

    public abstract record OrdinaryCharacterSaveReconciliation(string Id, ulong Character) : CharacterSaveReconciliation(Id)
    {
        internal override void Validate()
        {
            if (Character == 0 || !Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N"))
                throw new InvalidOperationException("Ordinary Character save requires its original Character and receipt ID.");
        }

        internal override void ValidateParent(CharacterContext context)
        {
            if (context.AtomicCharacterId != Character)
                throw new InvalidOperationException("Ordinary Character save changed its Character.");
        }

        // The existing reconciliation table stores tagged complete-save payloads.
        internal override string ReadPayload(CharacterContext context, string id)
            => context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id == id).Select(row => row.Payload).SingleOrDefault();

        internal override object AddReceipt(CharacterContext context, string payload, DateTime created)
        {
            var receipt = new BankReconciliationReceiptModel { Id = Id, Payload = payload, CreatedUtc = created };
            context.BankReconciliationReceipt.Add(receipt);
            return receipt;
        }

        internal override CharacterReconciliationRolledBackException RolledBack(CharacterContext context, string database, Exception cause, bool completed)
            => new(context, database, Id, cause, completed);
    }
}
