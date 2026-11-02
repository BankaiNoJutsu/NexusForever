using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    // Grant/revocation, encoded remaining time and active title share one SQL
    // baseline. Acknowledgement never replaces the actor's later runtime values.
    internal sealed class TitlePersistence
    {
        internal sealed record State(ushort Id, uint Time, byte Revoked);
        internal sealed record Snapshot(ushort? Active, State[] Titles);
        private sealed record Frozen(object Token, string Database, Snapshot State);
        private sealed class Unit(TitlePersistence owner, CharacterContext context, string database, object token)
        {
            internal readonly TitlePersistence Owner = owner;
            internal readonly CharacterContext Context = context;
            internal readonly string Database = database;
            internal readonly object Token = token;
            internal readonly Dictionary<object, CharacterColumnSaveIntent> Rows = new(ReferenceEqualityComparer.Instance);
            internal readonly List<Snapshot> Possible = [];
            internal Snapshot Sql, Target;
            internal IDisposable Scope;
            internal bool Flushed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }
        internal readonly object Gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, ulong), List<Unit>> leases = [];
        private readonly ulong character;
        private readonly ushort? title;
        private readonly bool ownsActive;
        private readonly Func<Snapshot> current;
        private string database;
        private Snapshot baseline;
        private List<Snapshot> uncertain;
        private Unit active;

        internal TitlePersistence(ulong character, Snapshot baseline, Func<Snapshot> current, string database = null, ushort? title = null)
        {
            this.character = character; this.baseline = baseline; this.current = current; this.database = database;
            this.title = title; ownsActive = baseline.Active.HasValue;
        }
        internal static State FromModel(CharacterTitleModel row) => new(row.Title, row.TimeRemaining, row.Revoked);
        internal static Snapshot FromModel(CharacterModel model) => new(model.Title, model.CharacterTitle.OrderBy(row => row.Title).Select(FromModel).ToArray());
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null && left.Active == right.Active && left.Titles.SequenceEqual(right.Titles);
        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (Gate)
            {
                if (active != null) throw new InvalidOperationException("Titles have an unfinished save.");
                var frozen = new Frozen(token, route, current()); return context => Save(context, null, frozen);
            }
        }
        internal void Save(CharacterContext context) => Save(context, null, null);
        internal void SaveTitle(CharacterContext context, ushort id) => Save(context, id, null);
        private static Snapshot Select(Snapshot stored, Snapshot runtime, ushort? selected)
            => !selected.HasValue ? runtime : new(stored.Active, stored.Titles.Where(row => row.Id != selected).Concat(runtime.Titles.Where(row => row.Id == selected)).OrderBy(row => row.Id).ToArray());

        private void Save(CharacterContext context, ushort? selected, Frozen frozen)
        {
            lock (Gate)
            {
                try
                {
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Titles require a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    Snapshot runtime = frozen?.State ?? current();
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token)))
                        throw new InvalidOperationException("Titles belong to an unfinished save unit.");
                    Snapshot target = Select(active?.Target ?? baseline, runtime, selected);
                    if (active == null && uncertain == null && Equal(target, baseline)) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(this, context, route, frozen?.Token);
                        unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:titles:{title}", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                var key = (route, character);
                                if (!leases.TryGetValue(key, out var held)) leases[key] = held = [];
                                if (held.Any(other => !title.HasValue || !other.Owner.title.HasValue || title == other.Owner.title))
                                    throw new InvalidOperationException("Titles belong to another loaded owner.");
                                held.Add(unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit; unit.OnFlushed = (_, _) => Flushed(unit); context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed)); context.OnSaveValidation(phase => Validate(unit, phase));
                        Snapshot actual = Read(context);
                        if (uncertain != null ? !uncertain.Any(candidate => Equal(candidate, actual)) : !Equal(baseline, actual))
                            throw new InvalidOperationException("Titles differ from their loaded or uncertain graph.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    foreach (var row in unit.Rows.Values) row.Validate();
                    Snapshot previous = unit.Target ?? unit.Sql; target = Select(previous, runtime, selected);
                    Stage(unit, unit.Sql, target, previous); unit.Target = target; unit.Possible.Add(target);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private Snapshot Read(CharacterContext context)
            => new(ownsActive ? context.Character.AsNoTracking().Where(row => row.Id == character).Select(row => row.Title).Single() : null,
                context.CharacterTitle.AsNoTracking().Where(row => row.Id == character && (!title.HasValue || row.Title == title))
                    .OrderBy(row => row.Title).Select(row => new State(row.Title, row.TimeRemaining, row.Revoked)).ToArray());
        private void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (before.Titles.Any(row => !desired.Titles.Any(after => after.Id == row.Id))) throw new InvalidOperationException("Title saves cannot delete loaded rows.");
            if (before.Active != desired.Active || previous.Active != desired.Active)
                Write(unit, new CharacterModel { Id = character, Title = before.Active!.Value }, new CharacterModel { Id = character, Title = desired.Active!.Value },
                    [nameof(CharacterModel.Id)], [nameof(CharacterModel.Title)], true);
            foreach (State row in desired.Titles)
            {
                State old = before.Titles.SingleOrDefault(value => value.Id == row.Id);
                if (row == old && row == previous.Titles.SingleOrDefault(value => value.Id == row.Id)) continue;
                Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterTitleModel.Id), nameof(CharacterTitleModel.Title)],
                    [nameof(CharacterTitleModel.TimeRemaining), nameof(CharacterTitleModel.Revoked)]);
            }
            CharacterTitleModel Model(State row) => new() { Id = character, Title = row.Id, TimeRemaining = row.Time, Revoked = row.Revoked };
        }
        private static void Write<T>(Unit unit, T before, T desired, string[] keys, string[] fields, bool partial = false) where T : class
        {
            var context = unit.Context; var identity = context.Entry(desired);
            var entry = context.ChangeTracker.Entries<T>().SingleOrDefault(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (entry == null)
            {
                entry = context.Entry(before ?? desired);
                if (before != null)
                {
                    var stored = entry.GetDatabaseValues() ?? throw new InvalidOperationException("Title row disappeared.");
                    entry.CurrentValues.SetValues(stored); entry.OriginalValues.SetValues(stored);
                }
                entry.State = before == null ? EntityState.Added : EntityState.Unchanged;
            }
            else if (!unit.Rows.ContainsKey(entry.Entity))
            {
                if (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached || fields.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("Title row already has foreign tracked intent.");
                if (!partial && fields.Any(name => !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue)))
                    throw new InvalidOperationException("Title row has a stale tracked baseline.");
            }
            foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            context.ChangeTracker.DetectChanges(); var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), partial, "Title"); guard.Capture(); unit.Rows[entry.Entity] = guard;
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Titles lost their save owner.");
                if (unit.Context.HasSavepointRollback) return;
                unit.Context.ChangeTracker.DetectChanges(); foreach (var row in unit.Rows.Values) row.Validate();
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Title SQL differs from its captured graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                Snapshot actual = Read(unit.Context); if (!Equal(actual, unit.Target)) return;
                unit.Sql = actual; unit.Possible.Add(actual); unit.Flushed = true;
                foreach (var row in unit.Rows.Values) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed) { baseline = unit.Sql; uncertain = null; }
                    else uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).ToList();
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) { var key = (unit.Database, character); leases[key].Remove(unit); if (leases[key].Count == 0) leases.Remove(key); }
                    unit.Scope.Dispose();
                }
            }
        }
    }
}
