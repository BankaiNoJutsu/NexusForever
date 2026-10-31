using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Quest.Reward
{
    // Joins a frozen completion to the ordinary Character save enrollment. The
    // actor integration must freeze its complete normal save before Auth work;
    // this unit retains scalar inputs only and never replays gameplay callbacks.
    internal sealed class ContractNormalSaveUnit
    {
        private readonly CharacterContext context;
        private readonly ContractCompletionCommand command;
        private readonly ContractRewardCatalog catalog;
        private readonly QuestTransactionParticipants participants;
        private readonly IDbContextTransaction transaction;
        private readonly string payload;
        private readonly CharacterContractRewardTrackModel track;
        private bool enrolled;
        private bool flushed;
        private Owned[] owned;
        private sealed record Owned(object Entity, EntityState State, PropertyValues Values, PropertyValues Original);
        private int outcome;
        // False means no positive EF commit observation, not proof of rollback:
        // an earlier interceptor can throw after SQL committed. Any failed save
        // task, regardless of this observation, requires authoritative reload.
        internal bool? TerminalCommitObserved => System.Threading.Volatile.Read(ref outcome) switch { 1 => true, -1 => false, _ => null };
        internal IReadOnlyList<ContractTrackState> CandidateTracks { get; private set; }

        internal ContractNormalSaveUnit(CharacterContext context, ContractCompletionCommand command, ContractRewardCatalog catalog)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.command = command ?? throw new ArgumentNullException(nameof(command));
            this.catalog = catalog;
            try
            {
                ArgumentNullException.ThrowIfNull(catalog);
                Require(context.AtomicCharacterId == command.CharacterId && !context.IsSaveRejected && !context.HasSavepointRollback
                    && !context.ChangeTracker.Entries().Any(), "Contract normal save requires its fresh atomic Character owner.");
                transaction = context.Database.CurrentTransaction;
                Require(transaction?.GetDbTransaction().IsolationLevel == IsolationLevel.Serializable, "Contract normal save lost its serializable transaction.");
                var definition = catalog.GetTrack(command.Source.TrackId);
                Require(definition != null && definition.Type == command.Source.TrackType && definition.PrerequisiteId == command.Source.TrackPrerequisiteId,
                    "Contract normal save source and catalog disagree.");
                participants = new(context, command.CharacterId, command.Changes);
                participants.VerifyQuestChanges(command.Changes, false);
                payload = command.Payload(participants);
                Require(payload.Length <= 1_048_576, "Contract normal save payload exceeds its bound.");
                // Gameplay side effects are never replayed against an existing
                // receipt. A failed/lost-ack ordinary save requires actor reload.
                Require(!context.CharacterContractCompletion.AsNoTracking().Any(row => row.Id == command.CharacterId && row.AcceptanceId == command.AcceptanceId),
                    "Contract generation already completed; reload authoritative rewards.");
                track = context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == command.CharacterId && row.TrackId == command.Source.TrackId);
                if (command.Selection != null)
                {
                    var actual = ContractRewardSnapshot.Read(context, command.CharacterId, () => catalog).SingleOrDefault(row => row.TrackId == command.Source.TrackId);
                    Require(ContractRewardSnapshot.Same(actual, command.SelectionBaseline), "Contract selection baseline changed before enrollment.");
                    var selection = ContractRewardSelection.Restore(catalog, command.Selection.Json);
                    Require(selection.TrackId == command.Source.TrackId && selection.CycleId == (track?.CycleId ?? 0)
                        && selection.AcceptanceId == command.AcceptanceId, "Contract selection changed its completion/cycle owner.");
                }
                context.OnSaveValidation(Validate);
                context.OnSaveSettled(committed =>
                {
                    context.SavedChanges -= OnFlushed;
                    System.Threading.Volatile.Write(ref outcome, committed ? 1 : -1);
                });
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        // Call after all ordinary managers have enrolled their frozen targets.
        // CharacterAtomicSave seals every enrolled model before its only flush.
        internal ContractCompletionStage Enroll()
        {
            try
            {
                Require(!enrolled && outcome == 0 && !context.IsSaveRejected && !context.HasSavepointRollback
                    && ReferenceEquals(context.Database.CurrentTransaction, transaction), "Contract normal save is terminal or already enrolled.");
                uint points = checked((track?.Points ?? 0) + command.Points);
                ulong revision = checked((track?.Revision ?? 0) + 1);
                var next = track ?? new CharacterContractRewardTrackModel { Id = command.CharacterId, TrackId = command.Source.TrackId };
                if (track == null) context.CharacterContractRewardTrack.Add(next); else context.Attach(next);
                next.Points = points; next.Revision = revision;
                var slots = context.CharacterContractRewardSlot.AsNoTracking().Where(row => row.Id == command.CharacterId
                    && row.TrackId == next.TrackId && row.CycleId == next.CycleId).OrderBy(row => row.Ordinal).ToList();
                if (command.Selection != null)
                {
                    Require(!next.SelectionInitialized && slots.Count == 0, "Contract cycle already owns a selection.");
                    var definition = catalog.GetTrack(next.TrackId);
                    foreach (var slot in definition.Slots)
                    {
                        var row = new CharacterContractRewardSlotModel
                        {
                            Id = command.CharacterId, TrackId = next.TrackId, CycleId = next.CycleId, Ordinal = slot.Ordinal,
                            RequiredPoints = slot.RequiredPoints, RewardRowId = command.Selection.Rewards[slot.Ordinal], SelectionJson = command.Selection.Json
                        };
                        context.CharacterContractRewardSlot.Add(row); slots.Add(row);
                    }
                    next.SelectionInitialized = true;
                }
                var receipt = new CharacterContractCompletionModel
                {
                    Id = command.CharacterId, AcceptanceId = command.AcceptanceId, QuestId = command.Source.QuestId,
                    TrackId = command.Source.TrackId, BasePoints = command.Source.BasePoints, Points = command.Points,
                    PayloadVersion = 1, PayloadJson = payload,
                    CreatedUtc = new DateTime(command.CreatedUtc.Ticks - command.CreatedUtc.Ticks % 10, DateTimeKind.Utc)
                };
                context.CharacterContractCompletion.Add(receipt);
                context.ChangeTracker.DetectChanges();
                owned = new object[] { next, receipt }.Concat(command.Selection == null ? [] : slots.Cast<object>()).Select(entity =>
                {
                    var entry = context.Entry(entity);
                    return new Owned(entity, entry.State, entry.CurrentValues.Clone(), entry.OriginalValues.Clone());
                }).ToArray();
                context.SavedChanges += OnFlushed;
                enrolled = true;
                return new(true, new(receipt.Id, receipt.AcceptanceId, receipt.QuestId, receipt.TrackId, receipt.BasePoints, receipt.Points, receipt.PayloadJson, receipt.CreatedUtc),
                    new(next.Id, next.TrackId, next.CycleId, next.Points, next.Revision, next.Active)
                    { SelectionInitialized = next.SelectionInitialized, Slots = Array.AsReadOnly(slots.Select(row => new ContractRewardSlotState(row.Ordinal, row.RewardRowId, row.RequiredPoints, row.SelectionJson)).ToArray()) },
                    new System.Collections.ObjectModel.ReadOnlyDictionary<ushort, Challenges.Reward.CharacterChallengeQuestSnapshot>(
                        command.Changes.ToDictionary(change => change.QuestId, change => participants.EncodeQuest(change.After))));
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }
        private void Validate(CharacterSaveValidationPhase phase)
        {
            Require(enrolled && outcome == 0 && ReferenceEquals(context.Database.CurrentTransaction, transaction), "Contract normal save is incomplete or lost its owner.");
            foreach (var row in owned)
            {
                var entry = context.Entry(row.Entity);
                Require(entry.State != EntityState.Detached && entry.State != EntityState.Deleted
                    && (flushed || entry.State == row.State) && Same(entry.CurrentValues, row.Values)
                    && (flushed || Same(entry.OriginalValues, row.Original)), "Contract normal receipt or points changed after enrollment.");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    var stored = entry.GetDatabaseValues();
                    Require(flushed && stored != null && Same(stored, row.Values), "Contract normal save lost its exact receipt or point target.");
                }
            }
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
            {
                participants.VerifyQuestChanges(command.Changes, true);
                CandidateTracks = ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(context, command.CharacterId, () => catalog), command.CharacterId, catalog);
            }
        }
        private void OnFlushed(object sender, SavedChangesEventArgs args) { if (args.EntitiesSavedCount > 0) flushed = true; }
        private static bool Same(PropertyValues first, PropertyValues second)
            => first.Properties.All(property => Equals(first[property.Name], second[property.Name]));
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
