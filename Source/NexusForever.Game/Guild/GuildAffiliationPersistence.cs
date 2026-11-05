using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Guild
{
    // Affiliation is a nullable Character column, independent of the guild's
    // own graph. Keep its accepted value through failed or uncertain saves.
    internal sealed class GuildAffiliationPersistence(ulong character, ulong? loaded, string database, Func<ulong?> current)
    {
        private sealed record Frozen(object Token, string Database, ulong? Value);
        private sealed class Unit(CharacterContext context, object token)
        {
            internal readonly CharacterContext Context = context;
            internal readonly object Token = token;
            internal readonly HashSet<ulong?> Possible = [];
            internal ulong? Sql, Target;
            internal bool Flushed;
            internal CharacterColumnSaveIntent Row;
            internal IDisposable Scope;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }

        private readonly object gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, ulong), Unit> leases = [];
        private ulong? baseline = loaded;
        private HashSet<ulong?> uncertain;
        private Unit active;
        internal bool HasUnfinishedSave { get { lock (gate) return active != null; } }

        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (gate)
            {
                if (active != null) throw new InvalidOperationException("Guild affiliation has an unfinished save.");
                var frozen = new Frozen(token, route, current());
                return context => Save(context, frozen);
            }
        }

        internal void Save(CharacterContext context) => Save(context, null);
        private void Save(CharacterContext context, Frozen frozen)
        {
            lock (gate)
            {
                try
                {
                    ulong? live = current(); // validates the original manager/Character even for frozen writes
                    if (context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Guild affiliation requires a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    ulong? target = frozen == null ? live : frozen.Value;
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token)))
                        throw new InvalidOperationException("Guild affiliation belongs to an unfinished save unit.");
                    if (active == null && uncertain == null && target == baseline) return;

                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(context, frozen?.Token);
                        unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:guild-affiliation", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                if (leases.ContainsKey((route, character)))
                                    throw new InvalidOperationException("Guild affiliation belongs to another loaded owner.");
                                leases.Add((route, character), unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit;
                        unit.OnFlushed = (_, _) => Flushed(unit);
                        context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, route, committed));
                        context.OnSaveValidation(phase => Validate(unit, phase));
                        ulong? actual = Read(context);
                        if (uncertain != null ? !uncertain.Contains(actual) : actual != baseline)
                            throw new InvalidOperationException("Guild affiliation differs from its loaded or uncertain value.");
                        unit.Sql = actual;
                        unit.Possible.Add(actual);
                    }

                    unit.Row?.Validate();
                    Write(unit, target);
                    unit.Target = target;
                    unit.Possible.Add(target);
                    context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }

        private ulong? Read(CharacterContext context)
            => context.Character.AsNoTracking().Where(row => row.Id == character).Select(row => row.GuildAffiliation).Single();

        private void Write(Unit unit, ulong? target)
        {
            var context = unit.Context;
            context.ChangeTracker.DetectChanges();
            var entry = context.ChangeTracker.Entries<CharacterModel>().SingleOrDefault(row => row.Entity.Id == character);
            if (entry == null)
            {
                entry = context.Entry(new CharacterModel { Id = character, GuildAffiliation = unit.Sql });
                entry.State = EntityState.Unchanged;
            }
            var selection = entry.Property(row => row.GuildAffiliation);
            if (unit.Row == null && (entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                || selection.IsModified || selection.CurrentValue != selection.OriginalValue))
                throw new InvalidOperationException("Guild affiliation has foreign tracked intent.");
            // Player stages a partial Character stub. An untouched column may
            // contain its CLR default rather than SQL; install the proven SQL
            // original only after rejecting local changes to this column.
            // Normalize only this column after a proven flush, including
            // SaveChanges(false). Other Character columns keep their EF intent.
            selection.OriginalValue = unit.Sql;
            selection.CurrentValue = target;
            selection.IsModified = unit.Sql != target;
            unit.Row = new CharacterColumnSaveIntent(entry,
                [nameof(CharacterModel.Id), nameof(CharacterModel.GuildAffiliation)], true, "Guild affiliation");
            unit.Row.Capture();
        }

        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Guild affiliation lost its save owner.");
                if (unit.Context.HasSavepointRollback) return;
                _ = current();
                unit.Context.ChangeTracker.DetectChanges();
                unit.Row?.Validate();
                if (Read(unit.Context) != (phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Guild affiliation SQL differs from its captured value.");
            }
        }

        private void Flushed(Unit unit)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback || Read(unit.Context) != unit.Target) return;
                unit.Sql = unit.Target;
                unit.Flushed = true;
                if (unit.Row != null) { unit.Row.Capture(); unit.Row.Flushed = true; }
            }
        }

        private void Complete(Unit unit, string route, bool committed)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed) { baseline = unit.Sql; uncertain = null; }
                    else
                    {
                        uncertain ??= [baseline];
                        uncertain.UnionWith(unit.Possible);
                    }
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed;
                    active = null;
                    lock (leaseGate)
                        if (leases.TryGetValue((route, character), out var held) && ReferenceEquals(held, unit))
                            leases.Remove((route, character));
                    unit.Scope.Dispose();
                }
            }
        }
    }
}
