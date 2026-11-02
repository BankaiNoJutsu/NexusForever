using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    // One immutable before/after graph owns the wardrobe's mutable columns.
    // Runtime values stay independent of SQL acknowledgements, including A/B/A edits.
    internal sealed class CostumePersistence
    {
        internal sealed record Parent(byte Index, uint Visibility);
        internal sealed record Slot(byte Index, byte Position, uint Item, uint Dye);
        internal sealed record Snapshot(sbyte? Active, Parent[] Parents, Slot[] Slots);
        private sealed record Selection(byte? Index = null, byte? Slot = null);
        private sealed record Frozen(object Token, string Database, Snapshot State);
        private sealed class Unit(CostumePersistence owner, CharacterContext context, string database, object token)
        {
            internal readonly CostumePersistence Owner = owner;
            internal readonly CharacterContext Context = context;
            internal readonly string Database = database;
            internal readonly object Token = token;
            internal readonly Dictionary<object, CharacterColumnSaveIntent> Rows = new(ReferenceEqualityComparer.Instance);
            internal readonly List<Snapshot> Possible = [];
            internal Snapshot Sql, Target;
            internal IDisposable Scope;
            internal bool Flushed, Enrolling;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }

        internal readonly object Gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, ulong), List<Unit>> leases = [];
        private readonly ulong character;
        private readonly byte? index, slot;
        private readonly bool ownsActive;
        private readonly Func<Snapshot> current;
        private string database;
        private Snapshot baseline;
        private List<Snapshot> uncertain;
        private Unit active;

        internal CostumePersistence(ulong character, Snapshot baseline, Func<Snapshot> current, string database = null, byte? index = null, byte? slot = null)
        {
            this.character = character; this.baseline = baseline; this.current = current; this.database = database;
            this.index = index; this.slot = slot; ownsActive = baseline.Active.HasValue;
        }
        internal static Snapshot FromModel(CharacterCostumeModel model)
            => new(null, [new(model.Index, model.VisibilityMask)], model.CostumeItem.OrderBy(row => row.Slot).Select(row => new Slot(row.Index, row.Slot, row.Item2Id, row.DyeData)).ToArray());
        internal static Snapshot FromModel(CharacterModel model)
            => new(model.ActiveCostumeIndex, model.Costume.OrderBy(row => row.Index).Select(row => new Parent(row.Index, row.VisibilityMask)).ToArray(),
                model.Costume.SelectMany(row => row.CostumeItem).OrderBy(row => row.Index).ThenBy(row => row.Slot).Select(row => new Slot(row.Index, row.Slot, row.Item2Id, row.DyeData)).ToArray());
        private static bool Equal(Snapshot a, Snapshot b) => a != null && b != null && a.Active == b.Active && a.Parents.SequenceEqual(b.Parents) && a.Slots.SequenceEqual(b.Slots);
        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (Gate)
            {
                if (active != null) throw new InvalidOperationException("Wardrobe has an unfinished save.");
                var frozen = new Frozen(token, route, current());
                return context => Save(context, new(), frozen);
            }
        }
        internal void Save(CharacterContext context) => Save(context, new(), null);
        internal void SaveCostume(CharacterContext context, byte costume) => Save(context, new(costume), null);
        internal void SaveSlot(CharacterContext context, byte costume, byte position) => Save(context, new(costume, position), null);

        private void Save(CharacterContext context, Selection selection, Frozen frozen)
        {
            lock (Gate)
            {
                try
                {
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Wardrobe requires a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    Snapshot runtime = frozen?.State ?? current();
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token) || active.Enrolling))
                        throw new InvalidOperationException("Wardrobe belongs to an unfinished save unit.");
                    Snapshot target = Select(active?.Target ?? baseline, runtime, selection);
                    if (active == null && uncertain == null && Equal(target, baseline)) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(this, context, route, frozen?.Token) { Sql = baseline };
                        unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:wardrobe:{index}:{slot}", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                var key = (route, character);
                                if (!leases.TryGetValue(key, out var held)) leases[key] = held = [];
                                if (held.Any(other => !index.HasValue || !other.Owner.index.HasValue || index == other.Owner.index
                                    && (!slot.HasValue || !other.Owner.slot.HasValue || slot == other.Owner.slot)))
                                    throw new InvalidOperationException("Wardrobe belongs to another loaded owner.");
                                held.Add(unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit;
                        unit.OnFlushed = (_, _) => Flushed(unit);
                        context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed));
                        context.OnSaveValidation(phase => Validate(unit, phase));
                        Snapshot actual = Read(context);
                        if (uncertain != null ? !uncertain.Any(candidate => Equal(candidate, actual)) : !Equal(baseline, actual))
                            throw new InvalidOperationException("Wardrobe differs from its loaded or uncertain graph.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    unit.Enrolling = true;
                    try
                    {
                        Snapshot previous = unit.Target ?? unit.Sql;
                        target = Select(previous, runtime, selection);
                        Stage(unit, unit.Sql, target, previous); unit.Target = target; unit.Possible.Add(target);
                    }
                    finally { unit.Enrolling = false; }
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private static Snapshot Select(Snapshot stored, Snapshot runtime, Selection scope)
        {
            bool ParentSelected(byte value) => !scope.Index.HasValue || scope.Index == value && !scope.Slot.HasValue;
            bool SlotSelected(Slot value) => !scope.Index.HasValue || scope.Index == value.Index && (!scope.Slot.HasValue || scope.Slot == value.Position);
            return new(scope.Index.HasValue ? stored.Active : runtime.Active,
                stored.Parents.Where(row => !ParentSelected(row.Index)).Concat(runtime.Parents.Where(row => ParentSelected(row.Index))).OrderBy(row => row.Index).ToArray(),
                stored.Slots.Where(row => !SlotSelected(row)).Concat(runtime.Slots.Where(SlotSelected)).OrderBy(row => row.Index).ThenBy(row => row.Position).ToArray());
        }
        private Snapshot Read(CharacterContext context)
        {
            sbyte? selected = ownsActive ? context.Character.AsNoTracking().Where(row => row.Id == character).Select(row => row.ActiveCostumeIndex).Single() : null;
            Parent[] parents = slot.HasValue ? [] : context.CharacterCostume.AsNoTracking().Where(row => row.Id == character && (!index.HasValue || row.Index == index))
                .OrderBy(row => row.Index).Select(row => new Parent(row.Index, row.VisibilityMask)).ToArray();
            Slot[] parts = context.CharacterCostumeItem.AsNoTracking().Where(row => row.Id == character && (!index.HasValue || row.Index == index) && (!slot.HasValue || row.Slot == slot))
                .OrderBy(row => row.Index).ThenBy(row => row.Slot).Select(row => new Slot(row.Index, row.Slot, row.Item2Id, row.DyeData)).ToArray();
            return new(selected, parents, parts);
        }
        private void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (before.Parents.Any(row => !desired.Parents.Any(after => after.Index == row.Index)) || before.Slots.Any(row => !desired.Slots.Any(after => after.Index == row.Index && after.Position == row.Position)))
                throw new InvalidOperationException("Wardrobe save cannot delete loaded rows.");
            if (before.Active != desired.Active || previous.Active != desired.Active)
                StageRow(unit, new CharacterModel { Id = character, ActiveCostumeIndex = before.Active!.Value },
                    new CharacterModel { Id = character, ActiveCostumeIndex = desired.Active!.Value }, [nameof(CharacterModel.Id)], [nameof(CharacterModel.ActiveCostumeIndex)], true);
            foreach (Parent row in desired.Parents)
            {
                Parent old = before.Parents.SingleOrDefault(value => value.Index == row.Index);
                if (row == old && row == previous.Parents.SingleOrDefault(value => value.Index == row.Index)) continue;
                StageRow(unit, old == null ? null : new CharacterCostumeModel { Id = character, Index = old.Index, VisibilityMask = old.Visibility },
                    new CharacterCostumeModel { Id = character, Index = row.Index, VisibilityMask = row.Visibility }, [nameof(CharacterCostumeModel.Id), nameof(CharacterCostumeModel.Index)], [nameof(CharacterCostumeModel.VisibilityMask)]);
            }
            foreach (Slot row in desired.Slots)
            {
                Slot old = before.Slots.SingleOrDefault(value => value.Index == row.Index && value.Position == row.Position);
                if (row == old && row == previous.Slots.SingleOrDefault(value => value.Index == row.Index && value.Position == row.Position)) continue;
                StageRow(unit, old == null ? null : new CharacterCostumeItemModel { Id = character, Index = old.Index, Slot = old.Position, Item2Id = old.Item, DyeData = old.Dye },
                    new CharacterCostumeItemModel { Id = character, Index = row.Index, Slot = row.Position, Item2Id = row.Item, DyeData = row.Dye },
                    [nameof(CharacterCostumeItemModel.Id), nameof(CharacterCostumeItemModel.Index), nameof(CharacterCostumeItemModel.Slot)], [nameof(CharacterCostumeItemModel.Item2Id), nameof(CharacterCostumeItemModel.DyeData)]);
            }
        }
        private static void StageRow<T>(Unit unit, T before, T desired, string[] keys, string[] fields, bool characterField = false) where T : class
        {
            var context = unit.Context; var identity = context.Entry(desired);
            var entry = context.ChangeTracker.Entries<T>().SingleOrDefault(candidate => keys.All(name => Equals(candidate.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (entry == null)
            {
                entry = context.Entry(before ?? desired);
                if (before != null)
                {
                    var stored = entry.GetDatabaseValues() ?? throw new InvalidOperationException("Wardrobe row disappeared.");
                    entry.CurrentValues.SetValues(stored); entry.OriginalValues.SetValues(stored);
                }
                entry.State = before == null ? EntityState.Added : EntityState.Unchanged;
            }
            else if (!unit.Rows.ContainsKey(entry.Entity))
            {
                if (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached || fields.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("Wardrobe row already has foreign tracked intent.");
                if (!characterField && fields.Any(name => !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue)))
                    throw new InvalidOperationException("Wardrobe row has a stale tracked baseline.");
            }
            foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).OriginalValue, entry.Property(name).CurrentValue);
            }
            context.ChangeTracker.DetectChanges(); var row = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), characterField, "Wardrobe"); row.Capture(); unit.Rows[entry.Entity] = row;
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Enrolling) throw new InvalidOperationException("Wardrobe lost its save owner.");
                if (unit.Context.HasSavepointRollback) return;
                unit.Context.ChangeTracker.DetectChanges(); foreach (CharacterColumnSaveIntent row in unit.Rows.Values) row.Validate();
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Wardrobe SQL differs from its captured graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                Snapshot actual = Read(unit.Context);
                if (!Equal(actual, unit.Target)) return;
                unit.Sql = actual; unit.Possible.Add(actual); unit.Flushed = true;
                foreach (CharacterColumnSaveIntent row in unit.Rows.Values) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                if (committed && unit.Flushed) { baseline = unit.Sql; uncertain = null; }
                else uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).ToList();
                unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                lock (leaseGate) { var key = (unit.Database, character); leases[key].Remove(unit); if (leases[key].Count == 0) leases.Remove(key); }
                unit.Scope.Dispose();
            }
        }
    }
}
