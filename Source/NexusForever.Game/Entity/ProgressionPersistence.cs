using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    // One accepted progression action may archive a skill, learn another and
    // discover a recipe. Recovery compares that combined graph, not separate rows.
    internal sealed class ProgressionPersistence
    {
        internal sealed record Skill(ulong Owner, uint Id, uint Xp, uint Active, uint Flags, uint Points,
            uint Tier0, uint Tier1, uint Tier2, uint Tier3, uint Tier4, uint Tier5, uint Tier6, uint Tier7, uint Tier8, uint Tier9);
        internal sealed record Recipe(ulong Owner, uint Id, bool Discovered, float X, float Y);
        internal sealed record Snapshot(Skill[] Skills, Recipe[] Recipes);
        private sealed record Frozen(object Token, string Database, Snapshot State, long Version);
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
            internal long Version, FlushedVersion;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }
        internal readonly object Gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, ulong), Unit> leases = [];
        private readonly ulong character;
        private readonly Func<Snapshot> current;
        private readonly Func<long> publicationVersion;
        private readonly Action<Snapshot, long> acknowledge;
        private readonly Action publicationReady;
        private long acknowledgedVersion;
        private string database;
        private Snapshot baseline;
        private List<Snapshot> uncertain;
        private Unit active;

        internal Snapshot Confirmed { get { lock (Gate) return baseline; } }
        internal ProgressionPersistence(ulong character, Snapshot baseline, Func<Snapshot> current, string database,
            Func<long> publicationVersion = null, Action<Snapshot, long> acknowledge = null, Action publicationReady = null)
        {
            this.character = character; this.baseline = baseline; this.current = current; this.database = database;
            this.publicationVersion = publicationVersion; this.acknowledge = acknowledge; this.publicationReady = publicationReady;
        }
        internal static Skill FromModel(CharacterTradeskillModel row) => new(row.Id, row.TradeskillId, row.TradeskillXp, row.IsActive, row.PropertyProficiencyFlags, row.TalentPoints,
            row.TalentTier00, row.TalentTier01, row.TalentTier02, row.TalentTier03, row.TalentTier04, row.TalentTier05, row.TalentTier06, row.TalentTier07, row.TalentTier08, row.TalentTier09);
        internal static Recipe FromModel(CharacterSchematicModel row) => new(row.Id, row.TradeskillSchematic2Id, row.Discovered, row.DiscoveryCoordinateX, row.DiscoveryCoordinateY);
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null && left.Skills.SequenceEqual(right.Skills) && left.Recipes.SequenceEqual(right.Recipes);
        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (Gate)
            {
                if (active != null) throw new InvalidOperationException("Progression has an unfinished save.");
                var frozen = new Frozen(token, route, current(), publicationVersion?.Invoke() ?? 0); return context => Save(context, frozen);
            }
        }
        internal void Save(CharacterContext context) => Save(context, null);
        private void Save(CharacterContext context, Frozen frozen)
        {
            lock (Gate)
            {
                try
                {
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Progression requires a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    Snapshot target = frozen?.State ?? current();
                    long version = frozen?.Version ?? publicationVersion?.Invoke() ?? 0;
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token)))
                        throw new InvalidOperationException("Progression belongs to an unfinished save unit.");
                    if (active == null && uncertain == null && Equal(target, baseline) && version <= acknowledgedVersion) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(context, route, frozen?.Token);
                        unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:progression", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                if (leases.ContainsKey((route, character))) throw new InvalidOperationException("Progression belongs to another loaded owner.");
                                leases.Add((route, character), unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit; unit.OnFlushed = (_, _) => Flushed(unit); context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed)); context.OnSaveValidation(phase => Validate(unit, phase));
                        Snapshot actual = Read(context);
                        if (uncertain != null ? !uncertain.Any(candidate => Equal(candidate, actual)) : !Equal(baseline, actual))
                            throw new InvalidOperationException("Progression differs from its loaded or uncertain graph.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    foreach (var row in unit.Rows.Values) row.Validate();
                    Stage(unit, unit.Sql, target, unit.Target ?? unit.Sql); unit.Target = target; unit.Possible.Add(target);
                    unit.Version = version;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private Snapshot Read(CharacterContext context) => new(
            context.CharacterTradeskill.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.TradeskillId).AsEnumerable().Select(FromModel).ToArray(),
            context.CharacterSchematic.AsNoTracking().Where(row => row.Id == character).OrderBy(row => row.TradeskillSchematic2Id).AsEnumerable().Select(FromModel).ToArray());
        private static void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            var oldSkills = before.Skills.ToDictionary(row => row.Id); var oldRecipes = before.Recipes.ToDictionary(row => row.Id);
            var previousSkills = previous.Skills.ToDictionary(row => row.Id); var previousRecipes = previous.Recipes.ToDictionary(row => row.Id);
            if (oldSkills.Keys.Except(desired.Skills.Select(row => row.Id)).Any() || oldRecipes.Keys.Except(desired.Recipes.Select(row => row.Id)).Any())
                throw new InvalidOperationException("Progression saves cannot delete loaded rows.");
            foreach (Skill row in desired.Skills)
            {
                Skill old = oldSkills.GetValueOrDefault(row.Id);
                if (row == old && row == previousSkills.GetValueOrDefault(row.Id)) continue;
                Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterTradeskillModel.Id), nameof(CharacterTradeskillModel.TradeskillId)],
                    [nameof(CharacterTradeskillModel.TradeskillXp), nameof(CharacterTradeskillModel.IsActive), nameof(CharacterTradeskillModel.PropertyProficiencyFlags), nameof(CharacterTradeskillModel.TalentPoints),
                     nameof(CharacterTradeskillModel.TalentTier00), nameof(CharacterTradeskillModel.TalentTier01), nameof(CharacterTradeskillModel.TalentTier02), nameof(CharacterTradeskillModel.TalentTier03), nameof(CharacterTradeskillModel.TalentTier04),
                     nameof(CharacterTradeskillModel.TalentTier05), nameof(CharacterTradeskillModel.TalentTier06), nameof(CharacterTradeskillModel.TalentTier07), nameof(CharacterTradeskillModel.TalentTier08), nameof(CharacterTradeskillModel.TalentTier09)]);
            }
            foreach (Recipe row in desired.Recipes)
            {
                Recipe old = oldRecipes.GetValueOrDefault(row.Id);
                if (row == old && row == previousRecipes.GetValueOrDefault(row.Id)) continue;
                Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterSchematicModel.Id), nameof(CharacterSchematicModel.TradeskillSchematic2Id)],
                    [nameof(CharacterSchematicModel.Discovered), nameof(CharacterSchematicModel.DiscoveryCoordinateX), nameof(CharacterSchematicModel.DiscoveryCoordinateY)]);
            }
        }
        private static CharacterTradeskillModel Model(Skill row) => new() { Id = row.Owner, TradeskillId = row.Id, TradeskillXp = row.Xp, IsActive = row.Active, PropertyProficiencyFlags = row.Flags, TalentPoints = row.Points,
            TalentTier00 = row.Tier0, TalentTier01 = row.Tier1, TalentTier02 = row.Tier2, TalentTier03 = row.Tier3, TalentTier04 = row.Tier4, TalentTier05 = row.Tier5, TalentTier06 = row.Tier6, TalentTier07 = row.Tier7, TalentTier08 = row.Tier8, TalentTier09 = row.Tier9 };
        private static CharacterSchematicModel Model(Recipe row) => new() { Id = row.Owner, TradeskillSchematic2Id = row.Id, Discovered = row.Discovered, DiscoveryCoordinateX = row.X, DiscoveryCoordinateY = row.Y };
        private static void Write<T>(Unit unit, T before, T desired, string[] keys, string[] fields) where T : class
        {
            var context = unit.Context; var identity = context.Entry(desired);
            var entry = context.ChangeTracker.Entries<T>().SingleOrDefault(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (entry == null) { entry = context.Entry(before ?? desired); entry.State = before == null ? EntityState.Added : EntityState.Unchanged; }
            else if (!unit.Rows.ContainsKey(entry.Entity) && (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                || fields.Any(name => entry.Property(name).IsModified || !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue))))
                throw new InvalidOperationException("Progression row already has foreign tracked intent.");
            foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            context.ChangeTracker.DetectChanges(); var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), false, "Progression"); guard.Capture(); unit.Rows[entry.Entity] = guard;
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Progression lost its save owner.");
                if (unit.Context.HasSavepointRollback) return;
                unit.Context.ChangeTracker.DetectChanges(); foreach (var row in unit.Rows.Values) row.Validate();
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Progression SQL differs from its captured graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                Snapshot actual = Read(unit.Context); if (!Equal(actual, unit.Target)) return;
                unit.Sql = actual; unit.Possible.Add(actual); unit.Flushed = true;
                unit.FlushedVersion = unit.Version;
                foreach (var row in unit.Rows.Values) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            bool ready = false;
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed)
                    {
                        acknowledge?.Invoke(unit.Sql, unit.FlushedVersion);
                        baseline = unit.Sql; uncertain = null;
                        acknowledgedVersion = Math.Max(acknowledgedVersion, unit.FlushedVersion); ready = true;
                    }
                    else uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).ToList();
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) { if (leases.TryGetValue((unit.Database, character), out var held) && ReferenceEquals(held, unit)) leases.Remove((unit.Database, character)); }
                    unit.Scope.Dispose();
                }
            }
            if (ready) publicationReady?.Invoke();
        }
    }
}
