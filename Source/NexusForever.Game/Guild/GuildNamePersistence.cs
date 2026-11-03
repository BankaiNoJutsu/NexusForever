using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Guild
{
    internal sealed class GuildNamePersistence(IGuildBase owner, string loadedName, string loadedDatabase, Action<string> saved)
    {
        private sealed class Unit(CharacterContext context, string target, string sql, CharacterColumnSaveIntent guard)
        {
            internal readonly CharacterContext Context = context;
            internal readonly string Target = target;
            internal string Sql = sql;
            internal readonly CharacterColumnSaveIntent Guard = guard;
            internal bool Flushed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }

        private readonly object gate = new();
        private readonly ulong guildId = owner.Id;
        private readonly byte guildType = (byte)owner.Type;
        private readonly HashSet<string> possibleNames = new(StringComparer.Ordinal) { loadedName };
        private string database = loadedDatabase;
        private string confirmedName = loadedName;
        private bool uncertain;
        private Unit active;

        internal bool Owns(CharacterContext context) { lock (gate) return active != null && ReferenceEquals(active.Context, context); }
        internal bool HasActiveUnit { get { lock (gate) return active != null; } }
        internal void RequireAvailable(CharacterContext context)
        {
            lock (gate)
                if (active != null && !ReferenceEquals(active.Context, context))
                {
                    var error = new InvalidOperationException("Guild name belongs to an unfinished save unit.");
                    context.RejectSave(error); throw error;
                }
        }

        internal bool NeedsSave
        {
            get { lock (gate) return active != null || uncertain || !StringComparer.Ordinal.Equals(owner.Name, confirmedName); }
        }

        internal void RequireDatabase(CharacterContext context)
        {
            lock (gate)
            {
                try
                {
                    if (owner.Id != guildId || (byte)owner.Type != guildType || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Guild name save lost its original owner or fresh context.");
                    database ??= CharacterDatabaseIdentity.Get(context);
                    CharacterDatabaseIdentity.Require(context, database);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }

        internal void Save(CharacterContext context, string captured = null)
        {
            lock (gate)
            {
                try
                {
                    if (active == null && captured == null && !NeedsSave) return;
                    RequireDatabase(context);
                    if (active != null)
                    {
                        if (!ReferenceEquals(active.Context, context))
                            throw new InvalidOperationException("Guild name belongs to an unfinished save unit.");
                        return;
                    }
                    GuildModel before = Read(context);
                    if (!possibleNames.Contains(before.Name))
                        throw new InvalidOperationException("Guild name SQL changed outside its retained outcomes.");
                    string beforeName = before.Name;
                    EntityEntry<GuildModel> entry = context.ChangeTracker.Entries<GuildModel>().SingleOrDefault(row => row.Entity.Id == guildId);
                    if (entry == null) entry = context.Attach(before);
                    else if (entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                        || entry.Property(row => row.Name).IsModified || entry.Property(row => row.Type).IsModified
                        || entry.Property(row => row.DeleteTime).IsModified || entry.Entity.Name != before.Name)
                        throw new InvalidOperationException("Guild name already has foreign tracked intent.");
                    string target = captured ?? owner.Name;
                    entry.Property(row => row.Name).OriginalValue = beforeName;
                    entry.Property(row => row.Name).CurrentValue = target;
                    entry.Property(row => row.Name).IsModified = true;
                    var guard = new CharacterColumnSaveIntent(entry,
                        [nameof(GuildModel.Id), nameof(GuildModel.Name), nameof(GuildModel.Type), nameof(GuildModel.DeleteTime)], true, "Guild name");
                    guard.Capture();
                    var unit = new Unit(context, target, beforeName, guard);
                    active = unit;
                    unit.OnFlushed = (_, _) =>
                    {
                        lock (gate)
                        {
                            if (context.HasSavepointRollback) return;
                            if (Read(context).Name != unit.Target)
                                throw new InvalidOperationException("Guild name did not flush its captured value.");
                            unit.Sql = unit.Target; unit.Flushed = true; guard.Capture(); guard.Flushed = true;
                        }
                    };
                    context.SavedChanges += unit.OnFlushed;
                    context.OnSaveCompleted(committed => Complete(unit, committed));
                    context.OnSaveValidation(phase =>
                    {
                        lock (gate)
                        {
                            if (context.HasSavepointRollback) return;
                            CharacterDatabaseIdentity.Require(context, database);
                            if (!ReferenceEquals(active, unit) || owner.Id != guildId || (byte)owner.Type != guildType
                                || !context.ChangeTracker.Entries<GuildModel>().Any(row => ReferenceEquals(row.Entity, entry.Entity)))
                                throw new InvalidOperationException("Guild name lost its tracked save owner.");
                            context.ChangeTracker.DetectChanges(); guard.Validate();
                            string expected = phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target;
                            if (Read(context).Name != expected)
                                throw new InvalidOperationException("Guild name SQL differs from its captured save unit.");
                        }
                    });
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }

        private GuildModel Read(CharacterContext context)
        {
            GuildModel row = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == guildId);
            if (row == null || row.Type != guildType || row.DeleteTime != null)
                throw new InvalidOperationException("Guild name save lost its active database row.");
            return row;
        }

        private void Complete(Unit unit, bool committed)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                unit.Context.SavedChanges -= unit.OnFlushed;
                active = null;
                if (committed && unit.Flushed && owner.Id == guildId && (byte)owner.Type == guildType)
                {
                    confirmedName = unit.Target; uncertain = false;
                    possibleNames.Clear(); possibleNames.Add(unit.Target);
                    saved(unit.Target);
                }
                else { uncertain = true; possibleNames.Add(unit.Target); }
            }
        }
    }
}
