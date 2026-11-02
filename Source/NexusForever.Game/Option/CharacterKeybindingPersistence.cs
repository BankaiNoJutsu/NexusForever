using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Option
{
    // The complete character input set is one accepted replacement. Recovery
    // must not combine independently matching old/new bindings after uncertainty.
    internal sealed class CharacterKeybindingPersistence
    {
        internal sealed record Binding(ulong Owner, ushort Action, uint Device0, uint Device1, uint Device2,
            uint Code0, uint Code1, uint Code2, uint Meta0, uint Meta1, uint Meta2, uint Event0, uint Event1, uint Event2);
        internal sealed record Snapshot(sbyte? Selection, Binding[] Bindings);
        private sealed record Frozen(object Token, string Database, Snapshot State);
        private sealed class Unit(CharacterContext context, string database, object token)
        {
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
        private static readonly Dictionary<(string, ulong), Unit> leases = [];
        private readonly ulong character;
        private readonly Func<Snapshot> current;
        private readonly Action<Snapshot> accepted;
        private readonly Action validateOwner;
        private readonly bool ownsSelection;
        private string database;
        private Snapshot baseline;
        private List<Snapshot> uncertain;
        private Unit active;
        private static readonly string[] keys = [nameof(CharacterKeybindingModel.Id), nameof(CharacterKeybindingModel.InputActionId)];
        private static readonly string[] fields = [nameof(CharacterKeybindingModel.DeviceEnum00), nameof(CharacterKeybindingModel.DeviceEnum01), nameof(CharacterKeybindingModel.DeviceEnum02),
            nameof(CharacterKeybindingModel.Code00), nameof(CharacterKeybindingModel.Code01), nameof(CharacterKeybindingModel.Code02),
            nameof(CharacterKeybindingModel.MetaKeys00), nameof(CharacterKeybindingModel.MetaKeys01), nameof(CharacterKeybindingModel.MetaKeys02),
            nameof(CharacterKeybindingModel.EventTypeEnum00), nameof(CharacterKeybindingModel.EventTypeEnum01), nameof(CharacterKeybindingModel.EventTypeEnum02)];

        internal CharacterKeybindingPersistence(ulong character, Snapshot baseline, Func<Snapshot> current, Action<Snapshot> accepted, string database, Action validateOwner)
        { this.character = character; this.baseline = baseline; this.current = current; this.accepted = accepted; this.database = database; this.validateOwner = validateOwner; ownsSelection = baseline.Selection.HasValue; }
        internal static Binding FromModel(CharacterKeybindingModel row) => new(row.Id, row.InputActionId, row.DeviceEnum00, row.DeviceEnum01, row.DeviceEnum02,
            row.Code00, row.Code01, row.Code02, row.MetaKeys00, row.MetaKeys01, row.MetaKeys02, row.EventTypeEnum00, row.EventTypeEnum01, row.EventTypeEnum02);
        private static CharacterKeybindingModel Model(Binding row) => new() { Id = row.Owner, InputActionId = row.Action, DeviceEnum00 = row.Device0, DeviceEnum01 = row.Device1, DeviceEnum02 = row.Device2,
            Code00 = row.Code0, Code01 = row.Code1, Code02 = row.Code2, MetaKeys00 = row.Meta0, MetaKeys01 = row.Meta1, MetaKeys02 = row.Meta2, EventTypeEnum00 = row.Event0, EventTypeEnum01 = row.Event1, EventTypeEnum02 = row.Event2 };
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null && left.Selection == right.Selection && left.Bindings.SequenceEqual(right.Bindings);
        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (Gate)
            {
                if (active != null) throw new InvalidOperationException("Keybindings have an unfinished save.");
                var frozen = new Frozen(token, route, current()); return context => Save(context, frozen);
            }
        }
        internal void Save(CharacterContext context) => Save(context, null);
        private void Save(CharacterContext context, Frozen frozen)
        {
            lock (Gate)
            {
                try
                {
                    validateOwner?.Invoke();
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Keybindings require a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    Snapshot target = frozen?.State ?? current();
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token)))
                        throw new InvalidOperationException("Keybindings belong to an unfinished save unit.");
                    if (active == null && uncertain == null && Equal(target, baseline)) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(context, route, frozen?.Token); unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:keybindings", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                if (leases.ContainsKey((route, character))) throw new InvalidOperationException("Keybindings belong to another loaded owner.");
                                leases.Add((route, character), unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit; unit.OnFlushed = (_, _) => Flushed(unit); context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed)); context.OnSaveValidation(phase => Validate(unit, phase));
                        Snapshot actual = Read(context);
                        if (uncertain != null ? !uncertain.Any(candidate => Equal(candidate, actual)) : !Equal(baseline, actual))
                            throw new InvalidOperationException("Keybindings differ from their loaded or uncertain set.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    foreach (var row in unit.Rows.Values) row.Validate();
                    NormalizeFlushed(unit);
                    Stage(unit, unit.Sql, target, unit.Target ?? unit.Sql); unit.Target = target; unit.Possible.Add(target);
                    context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private Snapshot Read(CharacterContext context) => new(
            ownsSelection ? context.Character.AsNoTracking().Where(row => row.Id == character).Select(row => (sbyte?)row.InputKeySet).Single() : null,
            context.CharacterKeybinding.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.InputActionId).AsEnumerable().Select(FromModel).ToArray());
        private static void NormalizeFlushed(Unit unit)
        {
            var stored = unit.Sql.Bindings.ToDictionary(row => row.Action);
            foreach (var guard in unit.Rows.Values.Where(row => row.Flushed && row.Entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                if (guard.Entry.Entity is CharacterModel root)
                {
                    if (unit.Sql.Selection != root.InputKeySet) throw new InvalidOperationException("Keybindings lost their flushed input selection.");
                    var selection = guard.Entry.Property(nameof(CharacterModel.InputKeySet));
                    selection.OriginalValue = unit.Sql.Selection.Value; selection.IsModified = false;
                    guard.Capture(); continue;
                }
                Binding row = FromModel((CharacterKeybindingModel)guard.Entry.Entity);
                if (stored.TryGetValue(row.Action, out Binding sql))
                {
                    if (row != sql) throw new InvalidOperationException("Keybindings lost their flushed tracked values.");
                    guard.Entry.State = EntityState.Unchanged;
                }
                else
                {
                    if (guard.Entry.State != EntityState.Deleted) throw new InvalidOperationException("Flushed keybinding disappeared from its SQL set.");
                    guard.Entry.State = EntityState.Detached;
                }
                // Accept only this owner's proven flush. Other tracked intent
                // remains pending, including unrelated Character columns.
                guard.Capture();
            }
        }
        private void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (desired.Selection.HasValue && (before.Selection != desired.Selection || previous.Selection != desired.Selection))
                WriteSelection(unit, before.Selection.Value, desired.Selection.Value);
            var stored = before.Bindings.ToDictionary(row => row.Action); var prior = previous.Bindings.ToDictionary(row => row.Action); var target = desired.Bindings.ToDictionary(row => row.Action);
            foreach (Binding row in desired.Bindings)
            {
                Binding old = stored.GetValueOrDefault(row.Action);
                if (row == old && row == prior.GetValueOrDefault(row.Action)) continue;
                Write(unit, old, row);
            }
            foreach (Binding row in before.Bindings.Where(row => !target.ContainsKey(row.Action)))
            {
                var entry = Write(unit, row, row); entry.State = EntityState.Deleted; unit.Rows[entry.Entity].Capture();
            }
            foreach (Binding row in previous.Bindings.Where(row => !stored.ContainsKey(row.Action) && !target.ContainsKey(row.Action)))
            {
                var entry = unit.Context.ChangeTracker.Entries<CharacterKeybindingModel>().Single(value => value.Entity.Id == row.Owner && value.Entity.InputActionId == row.Action);
                if (!unit.Rows.TryGetValue(entry.Entity, out var guard) || entry.State != EntityState.Added) throw new InvalidOperationException("Keybindings lost their pending create.");
                guard.Validate(); entry.State = EntityState.Detached; guard.Capture();
            }
        }
        private void WriteSelection(Unit unit, sbyte before, sbyte desired)
        {
            var entry = unit.Context.ChangeTracker.Entries<CharacterModel>().SingleOrDefault(row => row.Entity.Id == character);
            if (entry == null) { entry = unit.Context.Entry(new CharacterModel { Id = character, InputKeySet = before }); entry.State = EntityState.Unchanged; }
            var selection = entry.Property(row => row.InputKeySet);
            if (!unit.Rows.ContainsKey(entry.Entity) && (entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached || selection.IsModified))
                throw new InvalidOperationException("Input selection has foreign tracked intent.");
            selection.OriginalValue = before; selection.CurrentValue = desired; selection.IsModified = before != desired;
            var guard = new CharacterColumnSaveIntent(entry, [nameof(CharacterModel.Id), nameof(CharacterModel.InputKeySet)], true, "Keybinding selection");
            guard.Capture(); unit.Rows[entry.Entity] = guard;
        }
        private static EntityEntry<CharacterKeybindingModel> Write(Unit unit, Binding before, Binding desired)
        {
            var context = unit.Context; var model = Model(desired); var identity = context.Entry(model);
            var entry = context.ChangeTracker.Entries<CharacterKeybindingModel>().SingleOrDefault(row => row.Entity.Id == desired.Owner && row.Entity.InputActionId == desired.Action);
            var old = before == null ? null : context.Entry(Model(before));
            if (entry == null) { entry = old ?? identity; entry.State = before == null ? EntityState.Added : EntityState.Unchanged; }
            else if (!unit.Rows.ContainsKey(entry.Entity) && (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                || fields.Any(name => entry.Property(name).IsModified || !Equals(entry.Property(name).CurrentValue, old.Property(name).CurrentValue))))
                throw new InvalidOperationException("Keybinding row has foreign tracked intent.");
            if (before != null && entry.State == EntityState.Deleted && unit.Rows.ContainsKey(entry.Entity)) entry.State = EntityState.Unchanged;
            foreach (string name in fields)
            {
                if (old != null) entry.Property(name).OriginalValue = old.Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (old != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            context.ChangeTracker.DetectChanges(); var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), false, "Keybindings"); guard.Capture(); unit.Rows[entry.Entity] = guard;
            return entry;
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Keybindings lost their save owner.");
                if (unit.Context.HasSavepointRollback) return;
                validateOwner?.Invoke();
                unit.Context.ChangeTracker.DetectChanges(); foreach (var row in unit.Rows.Values) row.Validate();
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Keybinding SQL differs from its captured set.");
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
                    if (committed && unit.Flushed) { baseline = unit.Sql; uncertain = null; accepted(baseline); }
                    else uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).ToList();
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) { if (leases.TryGetValue((unit.Database, character), out var held) && ReferenceEquals(held, unit)) leases.Remove((unit.Database, character)); }
                    unit.Scope.Dispose();
                }
            }
        }
    }
}
