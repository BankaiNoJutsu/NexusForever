using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Challenges.Reward
{
    /// <summary>
    /// Stages one character-owned operation in the caller's fresh serializable
    /// transaction. Returned snapshots are provisional until terminal commit.
    /// The actor owner must retain its command/reservation across uncertainty,
    /// retry the whole unit with fresh contexts, and publish only after settlement.
    /// </summary>
    public sealed partial class CharacterChallengeRewardRepository
    {
        private readonly CharacterContext context;
        private readonly ulong characterId;
        private readonly CharacterChallengeRewardCatalog catalog;
        private readonly IGameTableManager tables;
        private readonly Func<uint, bool> meets;
        private readonly Func<int, int> selector;
        private readonly IDbContextTransaction transaction;
        private readonly List<OwnedRow> owned = [];
        private readonly Dictionary<ushort, (uint Track, uint First, uint Second, uint Third)> sources = [];
        private HashSet<ulong> deliveryItemIds = [];
        private bool started, enrolled, flushed, terminal;

        private sealed record OwnedRow(object Entity, EntityState State, PropertyValues Values, PropertyValues Originals, Func<object> Read);

        public CharacterChallengeRewardRepository(CharacterContext context, ulong characterId,
            CharacterChallengeRewardCatalog catalog, IGameTableManager tables, Func<uint, bool> meets,
            Func<int, int> selector = null)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.characterId = characterId;
            this.catalog = catalog;
            this.tables = tables;
            this.meets = meets;
            this.selector = selector;
            try
            {
                ArgumentNullException.ThrowIfNull(catalog);
                ArgumentNullException.ThrowIfNull(tables);
                ArgumentNullException.ThrowIfNull(meets);
                Require(characterId != 0 && !context.IsSaveRejected && !context.HasSavepointRollback
                    && !context.ChangeTracker.Entries().Any(), "A fresh untracked Character context is required.");
                Require(context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel == IsolationLevel.Serializable,
                    "A caller-owned serializable transaction is required.");
                transaction = context.Database.CurrentTransaction;
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
                Require(mysql || context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite", "Unsupported reward repository provider.");
                // Lock an existing owner even when this is the first attempt/track.
                // Locking only a missing track would leave initialization races.
                var owner = (mysql
                    ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id = {characterId} FOR UPDATE")
                    : context.Character.Where(row => row.Id == characterId)).AsNoTracking().AsEnumerable().SingleOrDefault();
                Require(owner != null && owner.DeleteTime == null, "Reward character no longer exists.");
                context.OnSaveValidation(ValidateUnit);
                context.SavedChanges += OnFlushed;
                context.OnSaveSettled(_ => { terminal = true; context.SavedChanges -= OnFlushed; });
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        internal void RequireAccountOwner(uint accountId)
        {
            void Validate()
            {
                var current = context.Character.AsNoTracking().SingleOrDefault(row => row.Id == characterId);
                Require(accountId != 0 && current?.AccountId == accountId && current.DeleteTime == null,
                    "Reward load lost its original account-owned Character.");
            }
            try { Validate(); context.OnSaveValidation(_ => Validate()); }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        public CharacterChallengeAttemptState BeginAttempt(CharacterChallengeState expected,
            CharacterChallengeState activated, string attemptId, bool enableCharacterTracks)
            => BeginAttemptCore(expected, activated, attemptId, enableCharacterTracks, null);

        internal CharacterChallengeAttemptState BeginAttemptFromLegacy(CharacterChallengeState expected, CharacterChallengeState legacy,
            CharacterChallengeState activated, string attemptId, bool enableCharacterTracks)
            => BeginAttemptCore(expected, activated, attemptId, enableCharacterTracks, legacy);

        private CharacterChallengeAttemptState BeginAttemptCore(CharacterChallengeState expected,
            CharacterChallengeState activated, string attemptId, bool enableCharacterTracks, CharacterChallengeState legacy)
            => Stage(() =>
            {
                Require(activated != null && activated.CharacterId == characterId && activated.Activated
                    && activated.Tier == 0 && activated.LastRewardTier == 0 && ValidId(attemptId), "Invalid activation snapshot.");
                ChallengeEntry source = Source(activated.ChallengeId);
                CharacterChallengeAttemptModel duplicate = context.CharacterChallengeAttempt.AsNoTracking()
                    .SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId);
                if (duplicate != null)
                {
                    ValidateAttempt(duplicate, source);
                    Require(duplicate.ChallengeId == activated.ChallengeId, "Attempt identity belongs to another Challenge.");
                    return Attempt(duplicate);
                }
                if (legacy != null)
                {
                    uint previousCount = expected?.Completions ?? 0;
                    Require(expected?.AttemptId == null && legacy.AttemptId == null && !legacy.Activated
                        && legacy.CharacterId == characterId && legacy.ChallengeId == activated.ChallengeId
                        && (legacy.Completions == previousCount || previousCount < uint.MaxValue && legacy.Completions == previousCount + 1),
                        "Legacy activation predecessor changed its owner, marker or completion history.");
                }
                var predecessor = legacy ?? expected;
                Require(predecessor?.Activated != true && (predecessor?.Completions ?? 0) == activated.Completions,
                    "Activation cannot replace a live attempt or change completion history.");
                CharacterChallengeModel previous = ReadChallenge(activated.ChallengeId);
                Require(CharacterChallengeState.FromModel(previous) == expected, "Challenge baseline changed before activation.");
                if (previous?.ActiveAttemptId != null)
                {
                    var old = ReadAttempt(previous.ActiveAttemptId, activated.ChallengeId);
                    Require(old.Terminal, "Previous durable attempt is not terminal.");
                }
                CharacterChallengeTrackDefinition definition = catalog.GetTrack(source.RewardTrackId);
                bool tracked = enableCharacterTracks && definition != null && Eligible(definition);
                var attempt = new CharacterChallengeAttemptModel
                {
                    Id = characterId, AttemptId = attemptId, ChallengeId = activated.ChallengeId,
                    TrackId = source.RewardTrackId, RewardMode = (byte)(tracked
                        ? CharacterChallengeRewardMode.CharacterTrack : CharacterChallengeRewardMode.LegacyDirect)
                };
                context.CharacterChallengeAttempt.Add(attempt);
                Own(attempt, () => context.CharacterChallengeAttempt.AsNoTracking()
                    .SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId));
                StageChallenge(previous, activated with { AttemptId = attemptId });
                if (tracked) EnsureTrack(definition);
                return Attempt(attempt);
            });

        public CharacterChallengeMedalState CreditMedal(string attemptId, ushort challengeId, byte medal,
            ChallengeRewardModifierSnapshot modifier, CharacterChallengeState expected, CharacterChallengeState advanced,
            DateTime createdUtc)
            => Stage(() =>
            {
                Require(medal < 3 && createdUtc.Kind == DateTimeKind.Utc && createdUtc != default, "Invalid medal identity/time.");
                ChallengeEntry source = Source(challengeId);
                var attempt = ReadAttempt(attemptId, challengeId);
                ValidateAttempt(attempt, source);
                Require(attempt.RewardMode == (byte)CharacterChallengeRewardMode.CharacterTrack, "Legacy attempts cannot earn track medals.");
                var definition = catalog.GetTrack(attempt.TrackId);
                Require(definition != null, "Attempt catalog is unsupported.");
                CharacterChallengeRewardTrackModel track = ReadTrack(attempt.TrackId);
                Require(track != null, "Attempt track was not initialized.");
                var slots = ReadSlots(track);
                var receipt = context.CharacterChallengeMedalReceipt.AsNoTracking().SingleOrDefault(row =>
                    row.Id == characterId && row.AttemptId == attemptId && row.Medal == medal);
                if (receipt != null)
                {
                    ValidateMedal(receipt, attempt);
                    return new CharacterChallengeMedalState(false, medal, receipt.Points, receipt.Multiplier,
                        receipt.ModifierRevision, Attempt(attempt), Track(track, slots));
                }
                Require(!attempt.Terminal && attempt.EarnedMedalMask == (1u << medal) - 1,
                    "Medals must advance the same live attempt once in order.");
                Require(HasMedal(source, medal) && expected?.Activated == true && expected.Tier == medal
                    && expected.AttemptId == attemptId && advanced != null && advanced.AttemptId == attemptId
                    && advanced.CharacterId == characterId && advanced.ChallengeId == challengeId
                    && advanced.LastRewardTier == medal && (advanced.Tier == medal + 1 || !advanced.Activated),
                    "Challenge transition does not acknowledge this source medal.");
                Require(advanced.Completions == (advanced.Activated ? expected.Completions : checked(expected.Completions + 1)),
                    "Medal transition changed completion history.");
                Require(modifier.TryGetPoints(100, out uint increment), "Invalid frozen medal multiplier.");
                var before = ReadChallenge(challengeId);
                Require(CharacterChallengeState.FromModel(before) == expected, "Challenge baseline changed before medal credit.");
                uint points = checked(track.Points + increment);
                context.Attach(attempt); attempt.EarnedMedalMask |= 1u << medal;
                attempt.Terminal = !advanced.Activated;
                context.Attach(track); track.Points = points; track.Revision = checked(track.Revision + 1);
                var added = new CharacterChallengeMedalReceiptModel
                {
                    Id = characterId, AttemptId = attemptId, Medal = medal, TrackId = attempt.TrackId,
                    Points = increment, Multiplier = modifier.Multiplier, ModifierRevision = modifier.Revision, CreatedUtc = SqlTime(createdUtc)
                };
                context.CharacterChallengeMedalReceipt.Add(added);
                Own(attempt, () => context.CharacterChallengeAttempt.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId));
                Own(track, () => ReadTrack(track.TrackId));
                Own(added, () => context.CharacterChallengeMedalReceipt.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId && row.Medal == medal));
                StageChallenge(before, advanced);
                return new CharacterChallengeMedalState(true, medal, increment, modifier.Multiplier,
                    modifier.Revision, Attempt(attempt), Track(track, slots));
            });

        public CharacterChallengeAttemptState FinishAttempt(string attemptId, ushort challengeId,
            CharacterChallengeState expected, CharacterChallengeState finished)
            => Stage(() =>
            {
                var attempt = ReadAttempt(attemptId, challengeId);
                ValidateAttempt(attempt, Source(challengeId));
                if (attempt.Terminal) return Attempt(attempt);
                Require(expected?.Activated == true && expected.AttemptId == attemptId && finished != null
                    && !finished.Activated && finished.AttemptId == attemptId && finished.CharacterId == characterId
                    && finished.ChallengeId == challengeId && finished.LastRewardTier <= expected.LastRewardTier
                    && (finished.Completions == expected.Completions || finished.Completions == checked(expected.Completions + 1)),
                    "Invalid terminal Challenge transition.");
                var before = ReadChallenge(challengeId);
                Require(CharacterChallengeState.FromModel(before) == expected, "Challenge baseline changed before completion.");
                context.Attach(attempt); attempt.Terminal = true;
                Own(attempt, () => context.CharacterChallengeAttempt.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId));
                StageChallenge(before, finished);
                return Attempt(attempt);
            });

        private T Stage<T>(Func<T> action)
        {
            try
            {
                Require(!started && !terminal && !context.IsSaveRejected && !context.HasSavepointRollback,
                    "A reward unit stages exactly one operation.");
                started = true;
                T result = action();
                StageQuestChanges();
                context.ChangeTracker.DetectChanges();
                foreach (var row in owned.ToArray())
                {
                    EntityEntry entry = context.Entry(row.Entity);
                    owned[owned.IndexOf(row)] = row with
                    {
                        State = entry.State, Values = entry.CurrentValues.Clone(),
                        Originals = entry.State == EntityState.Added ? entry.OriginalValues.Clone() : row.Originals
                    };
                }
                enrolled = true;
                return result;
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        private void Own(object entity, Func<object> read)
        {
            Require(!owned.Any(row => ReferenceEquals(row.Entity, entity)), "Reward row has multiple staging owners.");
            EntityEntry entry = context.Entry(entity);
            owned.Add(new(entity, entry.State, entry.CurrentValues.Clone(), entry.OriginalValues.Clone(), read));
        }

        private void OnFlushed(object sender, SavedChangesEventArgs args)
        {
            if (args.EntitiesSavedCount > 0) flushed = true;
        }

        private void ValidateUnit(CharacterSaveValidationPhase phase)
        {
            Require(started && enrolled && !terminal && !context.IsSaveRejected && !context.HasSavepointRollback,
                "Reward unit is rejected, unfinished or terminal.");
            Require(ReferenceEquals(context.Database.CurrentTransaction, transaction), "Reward transaction ownership changed.");
            foreach (var source in sources)
            {
                var current = tables.Challenge?.GetEntry(source.Key);
                Require(current != null && current.Id == source.Key
                    && (current.RewardTrackId, current.ChallengeTierId00, current.ChallengeTierId01, current.ChallengeTierId02) == source.Value,
                    "Challenge source definition changed after staging.");
            }
            foreach (OwnedRow row in owned)
            {
                EntityEntry entry = context.Entry(row.Entity);
                bool deleted = row.State == EntityState.Deleted;
                Require((deleted ? entry.State == EntityState.Deleted || flushed && entry.State == EntityState.Detached
                        : entry.State != EntityState.Detached && entry.State != EntityState.Deleted)
                    && (flushed || entry.State == row.State)
                    && (flushed || SameValues(entry.OriginalValues, row.Originals))
                    && SameValues(entry.CurrentValues, row.Values), $"Staged reward ownership or values changed for {entry.Metadata.ClrType.Name}"
                    + $" (expected {row.State}, actual {entry.State}, flushed {flushed}; changed fields: "
                    + string.Join(",", row.Values.Properties.Where(property => !Equals(entry.CurrentValues[property.Name], row.Values[property.Name]))
                        .Select(property => property.Name)) + ").");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    Require(flushed, "Reward unit has not flushed.");
                    object durable = row.Read();
                    Require(deleted ? durable == null : durable != null && SameValues(context.Entry(durable).CurrentValues, row.Values),
                        "Reward unit SQL state does not match its complete staged snapshot.");
                }
            }
            foreach (EntityEntry entry in context.ChangeTracker.Entries())
                Require(owned.Any(row => ReferenceEquals(row.Entity, entry.Entity))
                    || entry.Entity is ItemModel item && deliveryItemIds.Contains(item.Id),
                    "Unrelated entity enrolled in the reward unit.");
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && owned.Count == 0)
                Require(!context.ChangeTracker.HasChanges(), "Read-only replay acquired unrelated writes.");
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit) ValidateQuestWitnesses();
        }

        private static bool SameValues(PropertyValues first, PropertyValues second)
            => first.Properties.All(property => Equals(first[property.Name], second[property.Name]));
        private bool Eligible(CharacterChallengeTrackDefinition definition) => definition.PrerequisiteId == 0 || meets(definition.PrerequisiteId);
        private ChallengeEntry Source(ushort id)
        {
            ChallengeEntry entry = tables.Challenge?.GetEntry(id);
            Require(entry != null && entry.Id == id, "Challenge source definition is missing.");
            sources.TryAdd(id, (entry.RewardTrackId, entry.ChallengeTierId00, entry.ChallengeTierId01, entry.ChallengeTierId02));
            return entry;
        }
        private static bool HasMedal(ChallengeEntry entry, byte medal)
            => medal switch { 0 => entry.ChallengeTierId00 != 0, 1 => entry.ChallengeTierId01 != 0, 2 => entry.ChallengeTierId02 != 0, _ => false };
        private CharacterChallengeModel ReadChallenge(ushort id) => context.CharacterChallenge.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.ChallengeId == id);
        private CharacterChallengeAttemptModel ReadAttempt(string id, ushort challenge)
        {
            Require(ValidId(id), "Invalid durable attempt identity.");
            var attempt = context.CharacterChallengeAttempt.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.AttemptId == id);
            Require(attempt != null && attempt.ChallengeId == challenge, "Attempt does not belong to this character/Challenge.");
            return attempt;
        }
        private static void ValidateAttempt(CharacterChallengeAttemptModel attempt, ChallengeEntry source)
            => Require(ValidId(attempt.AttemptId) && attempt.TrackId == source.RewardTrackId && attempt.ChallengeId == source.Id
                && attempt.RewardMode <= (byte)CharacterChallengeRewardMode.CharacterTrack
                && (attempt.RewardMode != (byte)CharacterChallengeRewardMode.LegacyDirect || attempt.EarnedMedalMask == 0)
                && (attempt.EarnedMedalMask is 0 or 1 or 3 or 7), "Attempt source/mode/medal state changed.");
        private static void ValidateMedal(CharacterChallengeMedalReceiptModel row, CharacterChallengeAttemptModel attempt)
        {
            var modifier = new ChallengeRewardModifierSnapshot(row.Multiplier, row.ModifierRevision);
            Require(row.Id == attempt.Id && row.AttemptId == attempt.AttemptId && row.Medal < 3 && row.CreatedUtc != default
                && row.TrackId == attempt.TrackId && (attempt.EarnedMedalMask & 1u << row.Medal) != 0
                && modifier.TryGetPoints(100, out uint points) && points == row.Points, "Durable medal receipt is inconsistent.");
        }
        private void StageChallenge(CharacterChallengeModel before, CharacterChallengeState after)
        {
            Require(after.CharacterId == characterId && after.ChallengeId != 0 && after.Tier < 3 && after.LastRewardTier < 3
                && double.IsFinite(after.ActiveSeconds) && double.IsFinite(after.CooldownSeconds) && double.IsFinite(after.AreaSeconds),
                "Invalid Challenge persistence state.");
            if (before == null) { before = after.ToModel(); context.CharacterChallenge.Add(before); }
            else { context.Attach(before); context.Entry(before).CurrentValues.SetValues(after.ToModel()); }
            ushort id = after.ChallengeId;
            Own(before, () => ReadChallenge(id));
            challengeChanged = true;
        }
        private static CharacterChallengeAttemptState Attempt(CharacterChallengeAttemptModel row)
            => new(row.AttemptId, row.ChallengeId, row.TrackId, (CharacterChallengeRewardMode)row.RewardMode, row.EarnedMedalMask, row.Terminal);
        private static bool ValidId(string value) => Guid.TryParseExact(value, "N", out _);
        private static DateTime SqlTime(DateTime value) => new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
