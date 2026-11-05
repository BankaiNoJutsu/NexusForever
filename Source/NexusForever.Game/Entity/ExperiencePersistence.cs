using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Game.Achievement;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    // Character experience/level, path selection and mission/reward progress
    // recover together. Per-row mixtures are not committed progression states.
    internal sealed partial class ExperiencePersistence
    {
        internal sealed record Experience(uint Xp, uint Rest, byte Level);
        internal sealed record Selection(uint Path, DateTime Activated);
        internal sealed record PathState(ulong Owner, byte Id, byte Unlocked, uint Xp, byte Rewarded);
        internal sealed record Mission(ulong Owner, ushort Id, ushort Episode, byte State, byte Completed, uint Count, uint Data, uint Xp);
        internal sealed record LearnedSpell(ulong Owner, uint Id, byte Tier);
        internal sealed record Shortcut(ulong Owner, byte Spec, ushort Location, byte Type, uint Object, byte Tier);
        internal sealed record Amp(ulong Owner, byte Spec, ushort Id);
        internal sealed record Spellbook(byte Active, byte Bonus, LearnedSpell[] Learned, Shortcut[] Shortcuts, Amp[] Amps, ushort AmpPower);
        internal sealed record Pet(ulong Owner, byte Type, uint Id, string Name, ulong Flairs);
        internal sealed record Flair(ulong Owner, uint Id);
        internal sealed record Achievement(ulong Owner, ushort Id, uint Progress, uint Credited, DateTime? Completed);
        internal sealed record DatacubeState(ulong Owner, byte Type, ushort Id, uint Progress);
        internal sealed record ArchiveState(ulong Owner, uint Id, uint Unlocked, uint Viewed);
        internal sealed record ExploredHexgroup(ulong Owner, ushort Zone, ushort Group);
        internal sealed record ReputationState(ulong Owner, uint Faction, float Amount);
        internal sealed record Snapshot(Experience Experience, Selection Selection, PathState[] Paths, Mission[] Missions,
            Spellbook Spells = null, Pet[] Pets = null, Achievement[] Achievements = null,
            DatacubeState[] Datacubes = null, ArchiveState[] Articles = null,
            ExploredHexgroup[] MapHexgroups = null, ReputationState[] Reputations = null, long ReputationSequence = 0, long ActionSetSequence = 0, Flair[] Flairs = null);
        private sealed record Frozen(object Token, string Database, Snapshot State);
        private sealed class Unit(CharacterContext context, string database, object token)
        {
            internal readonly CharacterContext Context = context;
            internal readonly string Database = database;
            internal readonly object Token = token;
            internal readonly Dictionary<object, CharacterColumnSaveIntent> Rows = new(ReferenceEqualityComparer.Instance);
            internal readonly List<Snapshot> Possible = [];
            internal Snapshot Sql, Target, Intent, FlushedIntent;
            internal IDisposable Scope;
            internal bool Flushed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
            internal RealmFirstPersistence.Save[] Claims = [];
        }
        private readonly object gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, ulong), Unit> leases = [];
        private readonly ulong character;
        private readonly Func<Snapshot> current;
        private readonly Action<Achievement[], Achievement[], RealmFirstPersistence.Claim[]> acknowledgeAchievements;
        private readonly Action<ArchiveState[]> acknowledgeArchive;
        private readonly Action<DatacubeState[]> acknowledgeDatacubes;
        private readonly Action<ExploredHexgroup[]> acknowledgeExploration;
        private readonly Action<ReputationState[], long> acknowledgeReputation;
        private readonly Action<ushort> acknowledgeAmpPower;
        private readonly Action<PathState[], Mission[]> acknowledgePath;
        private readonly Action<Spellbook, long> acknowledgeActionSets;
        private readonly Action<Flair[]> acknowledgeFlairs;
        private readonly Action publicationReady;
        private readonly RealmFirstPersistence realmFirst;
        private readonly bool ownsExperience, ownsPaths, ownsSpells, ownsPets, ownsAchievements, ownsDatacubes, ownsArchive, ownsMaps, ownsReputations, ownsFlairs;
        private string database;
        private Snapshot baseline;
        private Snapshot baselineIntent;
        private List<Snapshot> uncertain;
        private Unit active;

        internal ExperiencePersistence(ulong character, Snapshot baseline, Func<Snapshot> current, string database,
            Action<Achievement[], Achievement[], RealmFirstPersistence.Claim[]> acknowledgeAchievements = null,
            RealmFirstPersistence realmFirst = null, Action<ArchiveState[]> acknowledgeArchive = null, Action publicationReady = null,
            Action<DatacubeState[]> acknowledgeDatacubes = null, Action<ExploredHexgroup[]> acknowledgeExploration = null,
            Action<ReputationState[], long> acknowledgeReputation = null, Action<ushort> acknowledgeAmpPower = null,
            Action<PathState[], Mission[]> acknowledgePath = null, Action<Spellbook, long> acknowledgeActionSets = null, Action<Flair[]> acknowledgeFlairs = null)
        {
            this.character = character; this.baseline = baseline; this.current = current; this.database = database;
            this.acknowledgeAchievements = acknowledgeAchievements;
            this.realmFirst = realmFirst;
            this.acknowledgeArchive = acknowledgeArchive; this.publicationReady = publicationReady;
            this.acknowledgeDatacubes = acknowledgeDatacubes;
            this.acknowledgeExploration = acknowledgeExploration;
            this.acknowledgeReputation = acknowledgeReputation;
            this.acknowledgeAmpPower = acknowledgeAmpPower;
            this.acknowledgePath = acknowledgePath;
            this.acknowledgeActionSets = acknowledgeActionSets;
            this.acknowledgeFlairs = acknowledgeFlairs;
            baselineIntent = baseline;
            ownsExperience = baseline.Experience != null; ownsPaths = baseline.Selection != null;
            ownsSpells = baseline.Spells != null; ownsPets = baseline.Pets != null; ownsAchievements = baseline.Achievements != null;
            ownsDatacubes = baseline.Datacubes != null; ownsArchive = baseline.Articles != null;
            ownsMaps = baseline.MapHexgroups != null; ownsReputations = baseline.Reputations != null;
            ownsFlairs = baseline.Flairs != null;
        }
        internal static Experience FromExperience(CharacterModel row) => new(row.TotalXp, row.RestBonusXp, row.Level);
        internal static Selection FromSelection(CharacterModel row) => new(row.ActivePath, row.PathActivatedTimestamp);
        internal static PathState FromModel(CharacterPathModel row) => new(row.Id, row.Path, row.Unlocked, row.TotalXp, row.LevelRewarded);
        internal static Mission FromModel(CharacterPathMissionModel row) => new(row.Id, row.PathMissionId, row.PathEpisodeId, row.State, row.Completed, row.ProgressCount, row.ProgressData, row.Xp);
        internal static LearnedSpell FromModel(CharacterSpellModel row) => new(row.Id, row.Spell4BaseId, row.Tier);
        internal static Shortcut FromModel(CharacterActionSetShortcutModel row) => new(row.Id, row.SpecIndex, row.Location, row.ShortcutType, row.ObjectId, row.Tier);
        internal static Amp FromModel(CharacterActionSetAmpModel row) => new(row.Id, row.SpecIndex, row.AmpId);
        internal static Pet FromModel(CharacterPetCustomisationModel row) => new(row.Id, row.Type, row.ObjectId, row.Name, row.FlairIdMask);
        internal static Flair FromModel(CharacterPetFlairModel row) => new(row.Id, row.PetFlairId);
        internal static Achievement FromModel(CharacterAchievementModel row) => new(row.Id, row.AchievementId, row.ProgressState, row.CreditedChecklistMask, row.DateCompleted);
        internal static DatacubeState FromModel(CharacterDatacubeModel row) => new(row.Id, row.Type, row.Datacube, row.Progress);
        internal static ArchiveState FromModel(CharacterGalacticArchiveModel row) => new(row.Id, row.ArchiveArticleId, row.UnlockedFlags, row.ViewedFlags);
        internal static ExploredHexgroup FromModel(CharacterZonemapHexgroupModel row) => new(row.Id, row.ZoneMap, row.HexGroup);
        internal static ReputationState FromModel(CharacterReputation row) => new(row.Id, row.FactionId, row.Amount);
        private static bool SameSpells(Spellbook left, Spellbook right) => left == null ? right == null : right != null
            && SpellbookFields(left) == SpellbookFields(right)
            && left.Learned.SequenceEqual(right.Learned) && left.Shortcuts.SequenceEqual(right.Shortcuts) && left.Amps.SequenceEqual(right.Amps);
        private static (byte Active, byte Bonus, ushort AmpPower)? SpellbookFields(Spellbook book)
            => book == null ? null : (book.Active, book.Bonus, book.AmpPower);
        private static bool SameRows<T>(T[] left, T[] right) => left == null ? right == null : right != null && left.SequenceEqual(right);
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null
            && left.Experience == right.Experience && left.Selection == right.Selection
            && left.Paths.SequenceEqual(right.Paths) && left.Missions.SequenceEqual(right.Missions)
            && SameSpells(left.Spells, right.Spells) && SameRows(left.Pets, right.Pets) && SameRows(left.Achievements, right.Achievements)
            && SameRows(left.Datacubes, right.Datacubes) && SameRows(left.Articles, right.Articles)
            && SameRows(left.MapHexgroups, right.MapHexgroups) && SameRows(left.Reputations, right.Reputations) && SameRows(left.Flairs, right.Flairs);
        internal Action<CharacterContext> CaptureNormal(object token, string route)
        {
            lock (gate)
            {
                if (active != null) throw new InvalidOperationException("Experience has an unfinished save.");
                var frozen = new Frozen(token, route, current()); return context => Save(context, frozen);
            }
        }
        internal void Save(CharacterContext context) => Save(context, null);
        private void Save(CharacterContext context, Frozen frozen)
        {
            lock (gate)
            {
                try
                {
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Experience requires a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (database != null) CharacterDatabaseIdentity.Require(context, database);
                    if (frozen != null) CharacterDatabaseIdentity.Require(context, frozen.Database);
                    database ??= route;
                    Snapshot intent = frozen?.State ?? current();
                    Snapshot target = StoredTarget(context, intent);
                    if (active != null && (!ReferenceEquals(active.Context, context) || !ReferenceEquals(active.Token, frozen?.Token)))
                        throw new InvalidOperationException("Experience belongs to an unfinished save unit.");
                    if (active == null && uncertain == null && Equal(target, baseline)
                        && intent.ReputationSequence == baselineIntent.ReputationSequence
                        && intent.ActionSetSequence == baselineIntent.ActionSetSequence
                        && realmFirst?.HasPendingFor((target.Achievements ?? []).Where(row => row.Completed != null).Select(row => row.Id)) != true) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new(context, route, frozen?.Token);
                        unit.Scope = QuestSaveScope.Acquire(context, route, $"{character}:experience", frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                if (leases.ContainsKey((route, character))) throw new InvalidOperationException("Experience belongs to another loaded owner.");
                                leases.Add((route, character), unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        active = unit; unit.OnFlushed = (_, _) => Flushed(unit); context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed)); context.OnSaveValidation(phase => Validate(unit, phase));
                        Snapshot actual = Read(context);
                        if (uncertain != null ? !uncertain.Any(candidate => Equal(candidate, actual)) : !Equal(baseline, actual))
                            throw new InvalidOperationException("Experience differs from its loaded or uncertain graph.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    foreach (var row in unit.Rows.Values) row.Validate();
                    Stage(unit, unit.Sql, target, unit.Target ?? unit.Sql); unit.Target = target; unit.Intent = intent; unit.Possible.Add(target);
                    unit.Claims = realmFirst?.Stage(context, (target.Achievements ?? []).Where(row => row.Completed != null).Select(row => row.Id)) ?? [];
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private Snapshot Read(CharacterContext context)
        {
            CharacterModel row = context.Character.AsNoTracking().Single(value => value.Id == character);
            return new(ownsExperience ? FromExperience(row) : null, ownsPaths ? FromSelection(row) : null,
                ownsPaths ? context.CharacterPath.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.Path).AsEnumerable().Select(FromModel).ToArray() : [],
                ownsPaths ? context.CharacterPathMission.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.PathMissionId).AsEnumerable().Select(FromModel).ToArray() : [],
                ownsSpells ? new(row.ActiveSpec, row.BonusAbilityTierPoints,
                    context.CharacterSpell.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.Spell4BaseId).AsEnumerable().Select(FromModel).ToArray(),
                    context.CharacterActionSetShortcut.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.SpecIndex).ThenBy(value => value.Location).AsEnumerable().Select(FromModel).ToArray(),
                    context.CharacterActionSetAmp.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.SpecIndex).ThenBy(value => value.AmpId).AsEnumerable().Select(FromModel).ToArray(), row.BonusAmpPower) : null,
                ownsPets ? context.CharacterPetCustomisation.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.Type).ThenBy(value => value.ObjectId).AsEnumerable().Select(FromModel).ToArray() : null,
                ownsAchievements ? context.CharacterAchievement.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.AchievementId).AsEnumerable().Select(FromModel).ToArray() : null,
                ownsDatacubes ? context.CharacterDatacube.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.Type).ThenBy(value => value.Datacube).AsEnumerable().Select(FromModel).ToArray() : null,
                ownsArchive ? context.CharacterGalacticArchive.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.ArchiveArticleId).AsEnumerable().Select(FromModel).ToArray() : null,
                ownsMaps ? context.CharacterZonemapHexgroup.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.ZoneMap).ThenBy(value => value.HexGroup).AsEnumerable().Select(FromModel).ToArray() : null,
                ownsReputations ? context.CharacterReputation.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.FactionId).AsEnumerable().Select(FromModel).ToArray() : null,
                Flairs: ownsFlairs ? context.CharacterPetFlair.AsNoTracking().Where(value => value.Id == character).OrderBy(value => value.PetFlairId).AsEnumerable().Select(FromModel).ToArray() : null);
        }
        private Snapshot StoredTarget(CharacterContext context, Snapshot target)
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) != true) return target;
            // Match the mapped DATETIME conversion already used by the outer
            // CharacterSaveGraph, including this server's sql_mode. Keep the
            // actor and frozen snapshot's original fractional timestamp intact.
            DateTime Stored(DateTime instant) => instant.Ticks % TimeSpan.TicksPerSecond == 0 ? instant
                : context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single();
            var acceptedAchievements = baselineIntent.Achievements?.ToDictionary(row => row.Id);
            var storedAchievements = baseline.Achievements?.ToDictionary(row => row.Id);
            // Once that exact actor intent has committed, retain its stored
            // representation. Unchanged completed achievements need no new
            // conversion/query, and a later session mode cannot rewrite them.
            return target with { Selection = target.Selection == null ? null : target.Selection == baselineIntent.Selection ? baseline.Selection
                    : target.Selection with { Activated = Stored(target.Selection.Activated) },
                Achievements = target.Achievements?.Select(row => row.Completed == null ? row : row with { Completed =
                    acceptedAchievements?.TryGetValue(row.Id, out var accepted) == true && accepted.Completed == row.Completed
                        ? storedAchievements[row.Id].Completed : Stored(row.Completed.Value) }).ToArray() };
        }
        private void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            if (before.Experience != desired.Experience || before.Selection != desired.Selection || previous.Experience != desired.Experience || previous.Selection != desired.Selection
                || SpellbookFields(before.Spells) != SpellbookFields(desired.Spells) || SpellbookFields(previous.Spells) != SpellbookFields(desired.Spells))
            {
                string[] fields = (ownsExperience ? new[] { nameof(CharacterModel.TotalXp), nameof(CharacterModel.RestBonusXp), nameof(CharacterModel.Level) } : [])
                    .Concat(ownsPaths ? new[] { nameof(CharacterModel.ActivePath), nameof(CharacterModel.PathActivatedTimestamp) } : [])
                    .Concat(ownsSpells ? new[] { nameof(CharacterModel.ActiveSpec), nameof(CharacterModel.BonusAbilityTierPoints), nameof(CharacterModel.BonusAmpPower) } : []).ToArray();
                Write(unit, Character(before), Character(desired), [nameof(CharacterModel.Id)], fields, true);
            }
            var paths = before.Paths.ToDictionary(row => row.Id); var missions = before.Missions.ToDictionary(row => row.Id);
            var previousPaths = previous.Paths.ToDictionary(row => row.Id); var previousMissions = previous.Missions.ToDictionary(row => row.Id);
            if (paths.Keys.Except(desired.Paths.Select(row => row.Id)).Any() || missions.Keys.Except(desired.Missions.Select(row => row.Id)).Any())
                throw new InvalidOperationException("Experience saves cannot delete loaded path progress.");
            foreach (PathState row in desired.Paths)
            {
                PathState old = paths.GetValueOrDefault(row.Id);
                if (row == old && row == previousPaths.GetValueOrDefault(row.Id)) continue;
                // Player-owned creates enter the outer atomic receipt. The
                // standalone PathEntry raw upsert compatibility API is retained.
                Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterPathModel.Id), nameof(CharacterPathModel.Path)],
                    [nameof(CharacterPathModel.Unlocked), nameof(CharacterPathModel.TotalXp), nameof(CharacterPathModel.LevelRewarded)]);
            }
            foreach (Mission row in desired.Missions)
            {
                Mission old = missions.GetValueOrDefault(row.Id);
                if (row == old && row == previousMissions.GetValueOrDefault(row.Id)) continue;
                Write(unit, old == null ? null : Model(old), Model(row), [nameof(CharacterPathMissionModel.Id), nameof(CharacterPathMissionModel.PathMissionId)],
                    [nameof(CharacterPathMissionModel.PathEpisodeId), nameof(CharacterPathMissionModel.State), nameof(CharacterPathMissionModel.Completed), nameof(CharacterPathMissionModel.ProgressCount), nameof(CharacterPathMissionModel.ProgressData), nameof(CharacterPathMissionModel.Xp)]);
            }
            StageRewards(unit, before, desired, previous);
            StageCollections(unit, before, desired, previous);
        }
        private CharacterModel Character(Snapshot snapshot) => new() { Id = character, TotalXp = snapshot.Experience?.Xp ?? 0, RestBonusXp = snapshot.Experience?.Rest ?? 0, Level = snapshot.Experience?.Level ?? 0,
            ActivePath = snapshot.Selection?.Path ?? 0, PathActivatedTimestamp = snapshot.Selection?.Activated ?? default,
            ActiveSpec = snapshot.Spells?.Active ?? 0, BonusAbilityTierPoints = snapshot.Spells?.Bonus ?? 0, BonusAmpPower = snapshot.Spells?.AmpPower ?? 0 };
        private static CharacterPathModel Model(PathState row) => new() { Id = row.Owner, Path = row.Id, Unlocked = row.Unlocked, TotalXp = row.Xp, LevelRewarded = row.Rewarded };
        private static CharacterPathMissionModel Model(Mission row) => new() { Id = row.Owner, PathMissionId = row.Id, PathEpisodeId = row.Episode, State = row.State, Completed = row.Completed, ProgressCount = row.Count, ProgressData = row.Data, Xp = row.Xp };
        private static void Write<T>(Unit unit, T before, T desired, string[] keys, string[] fields, bool partial = false) where T : class
        {
            var context = unit.Context; var identity = context.Entry(desired);
            var entry = context.ChangeTracker.Entries<T>().SingleOrDefault(row => keys.All(name => Equals(row.Property(name).CurrentValue, identity.Property(name).CurrentValue)));
            if (entry == null) { entry = context.Entry(before ?? desired); entry.State = before == null ? EntityState.Added : EntityState.Unchanged; }
            else if (!unit.Rows.ContainsKey(entry.Entity) && (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                || fields.Any(name => entry.Property(name).IsModified || !partial && !Equals(entry.Property(name).CurrentValue, context.Entry(before).Property(name).CurrentValue))))
                throw new InvalidOperationException("Experience row already has foreign tracked intent.");
            if (before != null && entry.State == EntityState.Deleted && unit.Rows.ContainsKey(entry.Entity)) entry.State = EntityState.Unchanged;
            foreach (string name in fields)
            {
                if (before != null) entry.Property(name).OriginalValue = context.Entry(before).Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (before != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            context.ChangeTracker.DetectChanges(); var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), partial, "Experience"); guard.Capture(); unit.Rows[entry.Entity] = guard;
        }
        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Experience lost its save owner.");
                if (unit.Context.HasSavepointRollback) return;
                unit.Context.ChangeTracker.DetectChanges(); foreach (var row in unit.Rows.Values) row.Validate();
                if (!Equal(Read(unit.Context), phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                    throw new InvalidOperationException("Experience SQL differs from its captured graph.");
            }
        }
        private void Flushed(Unit unit)
        {
            lock (gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                Snapshot actual = Read(unit.Context); if (!Equal(actual, unit.Target)) return;
                unit.Sql = actual; unit.Possible.Add(actual); unit.Flushed = true;
                unit.FlushedIntent = unit.Intent;
                foreach (var row in unit.Rows.Values) { row.Capture(); row.Flushed = true; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            bool ready = false;
            lock (gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed && !unit.Context.HasSavepointRollback)
                    {
                        var claims = realmFirst?.Acknowledge(unit.Claims) ?? [];
                        acknowledgeAchievements?.Invoke(baseline.Achievements ?? [], unit.Sql.Achievements ?? [], claims);
                        acknowledgeArchive?.Invoke(unit.Sql.Articles ?? []);
                        acknowledgeDatacubes?.Invoke(unit.Sql.Datacubes ?? []);
                        acknowledgeExploration?.Invoke(unit.Sql.MapHexgroups ?? []);
                        acknowledgeReputation?.Invoke(unit.Sql.Reputations ?? [], unit.FlushedIntent.ReputationSequence);
                        if (unit.Sql.Spells != null) acknowledgeAmpPower?.Invoke(unit.Sql.Spells.AmpPower);
                        if (unit.Sql.Spells != null) acknowledgeActionSets?.Invoke(unit.Sql.Spells, unit.FlushedIntent.ActionSetSequence);
                        if (unit.Sql.Flairs != null) acknowledgeFlairs?.Invoke(unit.Sql.Flairs);
                        acknowledgePath?.Invoke(unit.Sql.Paths, unit.Sql.Missions);
                        baseline = unit.Sql; baselineIntent = unit.FlushedIntent; uncertain = null;
                        ready = true;
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
