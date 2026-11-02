using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NexusForever.Game.Entity
{
    internal sealed class CharacterColumnSaveIntent(EntityEntry entry, string[] fields, bool partial, string owner)
    {
        internal readonly EntityEntry Entry = entry;
        internal readonly string[] Fields = fields;
        internal object[] Values, Original;
        internal bool[] Modified;
        internal EntityState State;
        internal bool Flushed;
        internal void Capture()
        {
            Values = Fields.Select(name => Entry.Property(name).CurrentValue).ToArray();
            Original = Fields.Select(name => Entry.Property(name).OriginalValue).ToArray();
            Modified = Fields.Select(name => Entry.Property(name).IsModified).ToArray(); State = Entry.State;
        }
        internal void Validate()
        {
            // SaveChanges(false) may be followed by EF's explicit acceptance
            // before the caller commits. Only this exact post-flush transition
            // is equivalent; current values and the SQL target still agree.
            bool accepted = Flushed && (Entry.State == EntityState.Unchanged || partial && Entry.State == EntityState.Modified
                || State == EntityState.Deleted && Entry.State == EntityState.Detached)
                && Values.SequenceEqual(Fields.Select(name => Entry.Property(name).CurrentValue))
                && Values.SequenceEqual(Fields.Select(name => Entry.Property(name).OriginalValue))
                && Fields.All(name => !Entry.Property(name).IsModified);
            if (accepted) return;
            if ((partial ? Entry.State is EntityState.Deleted or EntityState.Detached : Entry.State != State) || !Values.SequenceEqual(Fields.Select(name => Entry.Property(name).CurrentValue))
                || !Original.SequenceEqual(Fields.Select(name => Entry.Property(name).OriginalValue))
                || !Modified.SequenceEqual(Fields.Select(name => Entry.Property(name).IsModified)))
                throw new InvalidOperationException($"{owner} lost its tracked write intent.");
        }
    }}
