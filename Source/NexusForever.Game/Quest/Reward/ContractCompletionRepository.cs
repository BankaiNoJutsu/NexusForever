using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest.Reward
{
    // One caller-owned serializable unit. Results remain provisional until its
    // terminal commit. This repository grants no item, currency or actor effects.
    internal sealed class ContractCompletionRepository
    {
        private readonly CharacterContext context;
        private readonly ulong characterId;
        private readonly ContractRewardCatalog catalog;
        private readonly IDbContextTransaction transaction;
        private readonly List<Owned> owned = [];
        private readonly List<(PropertyValues Values, Func<object> Read)> witnesses = [];
        private QuestTransactionParticipants participants;
        private bool started, enrolled, flushed, terminal;
        private sealed record Owned(object Entity, EntityState State, PropertyValues Values, PropertyValues Original, Func<object> Read);

        internal ContractCompletionRepository(CharacterContext context, ulong characterId, ContractRewardCatalog catalog)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context)); this.characterId = characterId; this.catalog = catalog;
            try
            {
                ArgumentNullException.ThrowIfNull(catalog);
                Require(characterId != 0 && !context.IsSaveRejected && !context.HasSavepointRollback && !context.ChangeTracker.Entries().Any(), "Contract unit requires a fresh Character context.");
                Require(context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel == IsolationLevel.Serializable, "Contract unit requires its caller's serializable transaction.");
                transaction = context.Database.CurrentTransaction;
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                Require(mysql || context.Database.IsSqlite(), "Unsupported Contract provider.");
                var owner = (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={characterId} FOR UPDATE")
                    : context.Character.Where(row => row.Id == characterId)).AsNoTracking().AsEnumerable().SingleOrDefault();
                Require(owner != null && owner.DeleteTime == null, "Contract character no longer exists.");
                context.OnSaveValidation(Validate);
                context.SavedChanges += OnFlushed;
                context.OnSaveSettled(_ => { terminal = true; context.SavedChanges -= OnFlushed; });
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        internal ContractCompletionStage Complete(ContractCompletionCommand command)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(command);
                Require(command.Selection == null, "Frozen Contract selections belong to the production normal-save owner.");
                Require(!started && !terminal && !context.IsSaveRejected && !context.HasSavepointRollback, "Contract unit stages one operation.");
                started = true;
                Require(command.CharacterId == characterId, "Contract command belongs to another character.");
                participants = new(context, characterId, command.Changes);
                string payload = command.Payload(participants);
                Require(payload.Length <= 1_048_576, "Contract completion payload exceeds its supported bound.");
                var duplicate = ReadReceipt(command.AcceptanceId); var track = ReadTrack(command.Source.TrackId);
                ContractCompletionStage result;
                if (duplicate != null)
                {
                    Require(duplicate.PayloadVersion == 1 && duplicate.PayloadJson == payload
                        && duplicate.QuestId == command.Source.QuestId && duplicate.TrackId == command.Source.TrackId
                        && duplicate.BasePoints == command.Source.BasePoints && duplicate.Points == command.Points,
                        "Acceptance receipt belongs to a different completion/evaluation.");
                    Require(track != null, "Contract receipt lost its progression owner.");
                    Observe(duplicate, () => ReadReceipt(command.AcceptanceId)); Observe(track, () => ReadTrack(command.Source.TrackId));
                    // A later accepted generation may already own this Quest.
                    // A duplicate receipt proves its old credit without restoring
                    // the old completed Quest graph or overwriting later points.
                    result = new(false, Receipt(duplicate), Track(track), participants.ReadQuestParticipantsForExecution());
                }
                else
                {
                    var definition = catalog.GetTrack(command.Source.TrackId);
                    Require(definition != null && definition.Type == command.Source.TrackType && definition.PrerequisiteId == command.Source.TrackPrerequisiteId,
                        "Contract source and catalog owners disagree.");
                    uint nextPoints = checked((track?.Points ?? 0) + command.Points);
                    ulong nextRevision = checked((track?.Revision ?? 0) + 1);
                    if (track == null)
                    {
                        track = new() { Id = characterId, TrackId = command.Source.TrackId };
                        context.CharacterContractRewardTrack.Add(track);
                    }
                    else context.Attach(track);
                    track.Points = nextPoints; track.Revision = nextRevision;
                    Own(track, () => ReadTrack(command.Source.TrackId));
                    var receipt = new CharacterContractCompletionModel
                    {
                        Id = characterId, AcceptanceId = command.AcceptanceId, QuestId = command.Source.QuestId,
                        TrackId = command.Source.TrackId, BasePoints = command.Source.BasePoints, Points = command.Points,
                        PayloadVersion = 1, PayloadJson = payload,
                        CreatedUtc = new DateTime(command.CreatedUtc.Ticks - command.CreatedUtc.Ticks % 10, DateTimeKind.Utc)
                    };
                    context.CharacterContractCompletion.Add(receipt); Own(receipt, () => ReadReceipt(command.AcceptanceId));
                    participants.StageQuestChanges(true, Own);
                    var targets = new System.Collections.ObjectModel.ReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot>(
                        command.Changes.ToDictionary(change => change.QuestId, change => participants.EncodeQuest(change.After)));
                    result = new(true, Receipt(receipt), Track(track), targets);
                }
                context.ChangeTracker.DetectChanges();
                for (int i = 0; i < owned.Count; ++i)
                {
                    var entry = context.Entry(owned[i].Entity);
                    owned[i] = owned[i] with { State = entry.State, Values = entry.CurrentValues.Clone(),
                        Original = entry.State == EntityState.Added ? entry.OriginalValues.Clone() : owned[i].Original };
                }
                enrolled = true; return result;
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        private CharacterContractCompletionModel ReadReceipt(string acceptance)
            => context.CharacterContractCompletion.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.AcceptanceId == acceptance);
        private CharacterContractRewardTrackModel ReadTrack(uint track)
            => context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.TrackId == track);
        private static ContractCompletionReceipt Receipt(CharacterContractCompletionModel row)
            => new(row.Id, row.AcceptanceId, row.QuestId, row.TrackId, row.BasePoints, row.Points, row.PayloadJson, row.CreatedUtc);
        private ContractTrackState Track(CharacterContractRewardTrackModel row)
        {
            var slots = context.CharacterContractRewardSlot.AsNoTracking().Where(slot => slot.Id == characterId && slot.TrackId == row.TrackId
                && slot.CycleId == row.CycleId).OrderBy(slot => slot.Ordinal).ToArray();
            foreach (var slot in slots) Observe(slot, () => context.CharacterContractRewardSlot.AsNoTracking().SingleOrDefault(stored =>
                stored.Id == characterId && stored.TrackId == slot.TrackId && stored.CycleId == slot.CycleId && stored.Ordinal == slot.Ordinal));
            return new(row.Id, row.TrackId, row.CycleId, row.Points, row.Revision, row.Active)
            {
                SelectionInitialized = row.SelectionInitialized,
                Slots = slots.Length == 0 ? Array.Empty<ContractRewardSlotState>() : Array.AsReadOnly(slots
                    .Select(slot => new ContractRewardSlotState(slot.Ordinal, slot.RewardRowId, slot.RequiredPoints, slot.SelectionJson)).ToArray())
            };
        }
        private void Own(object entity, Func<object> read)
        {
            Require(!owned.Any(row => ReferenceEquals(row.Entity, entity)), "Contract row has multiple staging owners.");
            var entry = context.Entry(entity); owned.Add(new(entity, entry.State, entry.CurrentValues.Clone(), entry.OriginalValues.Clone(), read));
        }
        private void Observe(object entity, Func<object> read) => witnesses.Add((context.Entry(entity).CurrentValues.Clone(), read));
        private void OnFlushed(object sender, SavedChangesEventArgs args) { if (args.EntitiesSavedCount > 0) flushed = true; }
        private void Validate(CharacterSaveValidationPhase phase)
        {
            Require(started && enrolled && !terminal && !context.IsSaveRejected && !context.HasSavepointRollback,
                "Contract unit is rejected, unfinished or terminal.");
            Require(ReferenceEquals(context.Database.CurrentTransaction, transaction), "Contract transaction ownership changed.");
            foreach (var row in owned)
            {
                var entry = context.Entry(row.Entity); bool deleted = row.State == EntityState.Deleted;
                Require((deleted ? entry.State == EntityState.Deleted || flushed && entry.State == EntityState.Detached
                        : entry.State != EntityState.Detached && entry.State != EntityState.Deleted)
                    && (flushed || entry.State == row.State) && (flushed || Same(entry.OriginalValues, row.Original))
                    && Same(entry.CurrentValues, row.Values), "Contract staged row changed its owner, originals or target values.");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    Require(flushed, "Contract unit has not flushed."); var stored = row.Read();
                    Require(deleted ? stored == null : stored != null && Same(context.Entry(stored).CurrentValues, row.Values), "Contract SQL no longer matches its complete target.");
                }
            }
            foreach (var entry in context.ChangeTracker.Entries()) Require(owned.Any(row => ReferenceEquals(row.Entity, entry.Entity)), "Unrelated entity enrolled in the Contract unit.");
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
            {
                foreach (var witness in witnesses)
                {
                    var stored = witness.Read(); Require(stored != null && Same(context.Entry(stored).CurrentValues, witness.Values), "Contract replay witness changed.");
                }
                participants?.ValidateQuestWitnesses();
            }
        }
        private static bool Same(PropertyValues first, PropertyValues second) => first.Properties.All(property => Equals(first[property.Name], second[property.Name]));
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
