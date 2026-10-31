using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;

namespace NexusForever.Game.RealmBank
{
    public sealed partial class RealmBankManager
    {
        private static ItemModel CaptureAdapter(IItem item, ulong character) => new()
        {
            Id = item.Guid, OwnerId = character, ItemId = item.Id, Location = (ushort)item.Location, BagIndex = item.BagIndex,
            StackCount = item.StackCount, Charges = item.Charges, Durability = item.Durability,
            ExpirationTimeLeft = item.ExpirationTimeLeft, Soulbound = item.Soulbound,
            MicrochipIds = ItemMicrochipIdsCodec.Serialize(item.MicrochipIds), RuneSlots = ItemRuneSlotsCodec.Serialize(item.RuneSlots)
        };
        private static RealmBankItemModel BankModel(ItemModel item, uint account, ushort realm) => new()
        {
            Id = item.Id, AccountId = account, RealmId = realm, ItemId = item.ItemId, BagIndex = item.BagIndex,
            StackCount = item.StackCount, Charges = item.Charges, Durability = item.Durability,
            ExpirationTimeLeft = item.ExpirationTimeLeft, Soulbound = item.Soulbound, MicrochipIds = item.MicrochipIds, RuneSlots = item.RuneSlots
        };

        // Both tracked targets and their stored values must reach the same unit.
        // Always request a transaction, including single-row bank updates, so
        // suppression/rewrites can reject before an implicit commit.
        private static void ProtectTransfer(CharacterContext context, RealmBankItemModel bank, bool deposit, ItemModel character)
            => ProtectStorage(context, bank?.Id ?? character.Id, bank, deposit, character, !deposit);

        private static void ProtectStorage(CharacterContext context, ulong id, RealmBankItemModel bank, bool bankPresent,
            ItemModel character, bool characterPresent)
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            object ReadBank() => context.RealmBankItem.AsNoTracking().SingleOrDefault(row => row.Id == id);
            object ReadCharacter() => context.Item.AsNoTracking().SingleOrDefault(row => row.Id == id);
            var bankEntry = bank == null ? null : context.Entry(bank);
            var characterEntry = context.ChangeTracker.Entries<ItemModel>().SingleOrDefault(row => row.Entity.Id == id);
            var targets = new[] {
                new BankRowWitness(context, bankEntry, ReadBank, bankPresent, bankPresent ? bank : null),
                new BankRowWitness(context, characterEntry, ReadCharacter, characterPresent, character)
            };
            bool flushed = false;
            EventHandler<SavedChangesEventArgs> saved = (_, _) => flushed = true;
            context.SavedChanges += saved;
            context.OnSaveCompleted(_ => context.SavedChanges -= saved);
            context.OnSaveValidation(phase =>
            {
                foreach (var target in targets) target.Validate(phase, flushed);
            });
        }
        private sealed class BankRowWitness
        {
            private readonly CharacterContext context;
            private readonly EntityEntry entry;
            private readonly Func<object> read;
            private readonly PropertyValues expected, original, before;
            private readonly bool present;
            internal BankRowWitness(CharacterContext context, EntityEntry entry, Func<object> read, bool present, object value)
            {
                this.context = context; this.entry = entry; this.read = read; this.present = present;
                var stored = read(); before = stored == null ? null : context.Entry(stored).CurrentValues.Clone();
                expected = entry?.CurrentValues.Clone(); original = entry?.OriginalValues.Clone();
                if (present && (entry == null || value == null || !Same(expected, context.Entry(value).CurrentValues))) Reject();
                if (!present && before != null && entry == null) Reject();
            }
            private static bool Same(PropertyValues left, PropertyValues right) => left == null ? right == null
                : right != null && left.Properties.All(property => Equals(left[property.Name], right[property.Name]));
            private void Reject() => throw new InvalidOperationException("Realm bank transfer lost its captured tracked/SQL witness.");
            internal void Validate(CharacterSaveValidationPhase phase, bool flushed)
            {
                if (entry != null)
                {
                    if (!Same(expected, entry.CurrentValues) || !Same(original, entry.OriginalValues) && !Same(expected, entry.OriginalValues)) Reject();
                    if (present)
                    {
                        if (entry.State is EntityState.Detached or EntityState.Deleted) Reject();
                        if (phase == CharacterSaveValidationPhase.BeforeFlush)
                        {
                            var prior = flushed ? expected : before;
                            if (prior == null && entry.State != EntityState.Added) Reject();
                            if (prior != null && entry.State != EntityState.Added && expected.Properties.Any(property =>
                                !Equals(expected[property.Name], prior[property.Name]) && !entry.Property(property.Name).IsModified)) Reject();
                        }
                    }
                    else if (entry.State != EntityState.Deleted && !(flushed && entry.State == EntityState.Detached)) Reject();
                }
                if (phase == CharacterSaveValidationPhase.BeforeFlush) return;
                var stored = read(); var actual = stored == null ? null : context.Entry(stored).CurrentValues;
                var wanted = phase == CharacterSaveValidationPhase.BeforeImplicitCommit || flushed ? (present ? expected : null) : before;
                if (!Same(wanted, actual)) Reject();
            }
        }
    }
}
