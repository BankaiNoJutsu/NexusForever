using System.Data;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.GameTable;
using NexusForever.Game.Quest.Reward;

namespace NexusForever.Game.Challenges.Reward
{
    /// <summary>
    /// Runs complete repository units away from the actor. It never publishes
    /// inventory packets or adopts live Challenge state. The actor retains its
    /// save/mutation ownership until this result has been handled.
    /// </summary>
    public sealed class CharacterChallengeRewardExecutor(Func<CharacterContext> contexts,
        CharacterChallengeRewardCatalog catalog, IGameTableManager tables)
    {
        private string configuredDatabase;
        private sealed record ExecutionProof(CharacterChallengeRewardCommand Command, IInventoryRewardReservation Reservation,
            Attempt Attempt, string Database)
        {
            public int Resumed;
        }
        private static readonly ConditionalWeakTable<CharacterChallengeRewardExecution, ExecutionProof> proofs = new();
        private sealed class Attempt
        {
            public object Operation;
            public CharacterChallengeRewardsState Before, Candidate;
            public IReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot> CandidateQuests;
            public bool HadWrites, CommitStarted, Committed, Ended;
            public Exception Error;
        }

        public Task<CharacterChallengeRewardExecution> ExecuteAsync(CharacterChallengeRewardCommand command,
            IInventoryRewardReservation reservation = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            // SQLite and some provider operations complete synchronously; even
            // those must not run SQL on the world actor's update thread.
            return Task.Run(() => RunAsync(command, reservation, cancellationToken, null));
        }

        internal Task<CharacterChallengeRewardExecution> ExecuteOwnedAsync(CharacterChallengeRewardCommand command,
            string database, IInventoryRewardReservation reservation = null, CancellationToken cancellation = default)
            => Task.Run(() => RunAsync(command, reservation, cancellation, null, database));

        internal static bool IsCompletionOf(CharacterChallengeRewardExecution result, CharacterChallengeRewardCommand command,
            string database, IInventoryRewardReservation reservation)
            => result != null && proofs.TryGetValue(result, out var proof) && ReferenceEquals(proof.Command, command)
                && ReferenceEquals(proof.Reservation, reservation) && proof.Database == database;

        public Task<CharacterChallengeRewardExecution> ResumeAsync(CharacterChallengeRewardCommand command,
            CharacterChallengeRewardExecution previous, IInventoryRewardReservation reservation = null,
            CancellationToken cancellationToken = default)
        {
            if (previous == null || previous.Outcome != CharacterChallengeRewardExecutionOutcome.Uncertain || !previous.OriginalUnitEnded
                || !proofs.TryGetValue(previous, out var proof) || !ReferenceEquals(proof.Command, command)
                || !ReferenceEquals(proof.Reservation, reservation) || Interlocked.CompareExchange(ref proof.Resumed, 1, 0) != 0)
                throw new InvalidOperationException("Resume requires the exact retained command, reservation and terminal execution proof.");
            return Task.Run(() => RunAsync(command, reservation, cancellationToken, proof));
        }

        private async Task<CharacterChallengeRewardExecution> RunAsync(CharacterChallengeRewardCommand command,
            IInventoryRewardReservation reservation, CancellationToken cancellation, ExecutionProof resume, string expectedDatabase = null)
        {
            CharacterContext strategyContext = null;
            Attempt last = resume?.Attempt;
            CharacterChallengeRewardExecution result = null;
            string database = resume?.Database ?? expectedDatabase;
            try
            {
                if (resume == null && reservation?.State == InventoryRewardReservationState.Uncertain)
                    return new(CharacterChallengeRewardExecutionOutcome.Uncertain, null, null, false, false, false,
                        new InvalidOperationException("An uncertain delivery requires its retained execution proof."));
                cancellation.ThrowIfCancellationRequested();
                strategyContext = CreateContext(ref database);
                var strategy = strategyContext.Database.CreateExecutionStrategy();
                result = await strategy.ExecuteAsync(async () =>
                {
                    if (last != null && NeedsResolution(last, reservation))
                    {
                        var resolved = await RecoverAsync(command, reservation, last, database);
                        if (resolved.Outcome != CharacterChallengeRewardExecutionOutcome.Failed) return resolved;
                    }
                    last = new Attempt();
                    return await ExecuteUnitAsync(command, reservation, last, database, cancellation);
                });
            }
            catch (Exception error)
            {
                if (last != null && NeedsResolution(last, reservation))
                    result = await RecoverWithStrategyAsync(command, reservation, last, database, strategyContext);
                else result = Failure(last?.Ended != false ? CharacterChallengeRewardExecutionOutcome.Failed
                    : CharacterChallengeRewardExecutionOutcome.Uncertain, last, error);
            }
            finally
            {
                if (strategyContext != null)
                {
                    try { await strategyContext.DisposeAsync(); }
                    catch (Exception error)
                    {
                        // This context owns no transaction. A cleanup error must
                        // not turn an already proved award into a failed retry.
                        if (result != null && result.Error == null) result = result with { Error = error };
                    }
                }
            }
            if (database != null) proofs.Add(result, new(command, reservation, last ?? new Attempt { Ended = true }, database));
            return result;
        }

        private static bool NeedsResolution(Attempt attempt, IInventoryRewardReservation reservation)
            => attempt.Ended && (attempt.HadWrites || attempt.CommitStarted
                || reservation?.State == InventoryRewardReservationState.Uncertain);

        // A non-transient acknowledgement error exits the original strategy
        // scope before this catch-path runs. Recovery still opens an explicit
        // transaction, so retry-enabled providers require a new strategy scope.
        // Only outcome observation is retried here, never actor callbacks or RNG.
        private async Task<CharacterChallengeRewardExecution> RecoverWithStrategyAsync(CharacterChallengeRewardCommand command,
            IInventoryRewardReservation reservation, Attempt original, string database, CharacterContext strategyContext)
        {
            if (!original.Ended) return Failure(CharacterChallengeRewardExecutionOutcome.Uncertain, original, original.Error);
            bool ownsContext = strategyContext == null; CharacterChallengeRewardExecution result = null;
            try
            {
                strategyContext ??= CreateContext(ref database);
                string capturedDatabase = database;
                result = await strategyContext.Database.CreateExecutionStrategy()
                    .ExecuteAsync(() => RecoverAsync(command, reservation, original, capturedDatabase));
            }
            catch (Exception error)
            {
                result = Failure(error is InvalidOperationException or JsonException or KeyNotFoundException
                    ? CharacterChallengeRewardExecutionOutcome.Conflict : CharacterChallengeRewardExecutionOutcome.Uncertain, original, error);
            }
            finally
            {
                if (ownsContext && strategyContext != null)
                {
                    try { await strategyContext.DisposeAsync(); }
                    catch (Exception error) { if (result != null && result.Error == null) result = result with { Error = error }; }
                }
            }
            return result;
        }

        private CharacterContext CreateContext(ref string database)
        {
            CharacterContext context = contexts() ?? throw new InvalidOperationException("Character context factory returned null.");
            try
            {
                string actual = Item.GetRewardDatabaseKey(context);
                if (database != null && database != actual)
                    throw new InvalidOperationException("Reward execution changed its retained database identity.");
                string configured = Interlocked.CompareExchange(ref configuredDatabase, actual, null) ?? actual;
                database ??= actual;
                if (configured != actual || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Reward execution changed database or reused an enrolled context.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private async Task<CharacterChallengeRewardExecution> ExecuteUnitAsync(CharacterChallengeRewardCommand command,
            IInventoryRewardReservation reservation, Attempt attempt, string database, CancellationToken cancellation)
        {
            CharacterContext context = null;
            IDbContextTransaction transaction = null;
            bool transactionEnded = false, contextEnded = true;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                context = CreateContext(ref database); contextEnded = false;
                transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellation); transactionEnded = false;
                var repository = new CharacterChallengeRewardRepository(context, command.CharacterId, catalog, tables, command.Meets);
                if (command.ExpectedAccountId is uint accountId) repository.RequireAccountOwner(accountId);
                attempt.Before = repository.ReadStateForExecution();
                attempt.Operation = command.Stage(repository, reservation);
                attempt.HadWrites = context.ChangeTracker.HasChanges();
                await context.SaveChangesAsync(cancellation);
                attempt.Candidate = repository.ReadStateForExecution();
                attempt.CandidateQuests = repository.ReadQuestParticipantsForExecution();
                attempt.CommitStarted = true;
                await transaction.CommitAsync(cancellation);
                attempt.Committed = true;
            }
            catch (Exception error) { attempt.Error = error; }
            finally
            {
                if (transaction != null)
                {
                    try { await transaction.DisposeAsync(); transactionEnded = true; }
                    catch (Exception error) { attempt.Error ??= error; }
                }
                if (context != null)
                {
                    try { await context.DisposeAsync(); contextEnded = true; }
                    catch (Exception error) { attempt.Error ??= error; }
                }
                // Successful transaction disposal already establishes terminal
                // ownership, even if later connection/context cleanup reports an
                // error. Without either confirmation, recovery must stay blocked.
                attempt.Ended = attempt.Committed || transactionEnded || contextEnded;
            }
            if (!attempt.Committed) throw attempt.Error ?? new InvalidOperationException("Reward unit did not reach a terminal commit.");
            if (attempt.Operation is CharacterChallengeClaimState claim && !claim.Applied && reservation != null
                && claim.ClaimId != command.Identity)
                return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, attempt,
                    new InvalidOperationException("Another claim won while this actor retained a prepared delivery."));
            return new(CharacterChallengeRewardExecutionOutcome.Committed, attempt.Operation, attempt.Candidate,
                attempt.HadWrites, false, true, attempt.Error) { QuestStates = attempt.CandidateQuests };
        }

        private async Task<CharacterChallengeRewardExecution> RecoverAsync(CharacterChallengeRewardCommand command,
            IInventoryRewardReservation reservation, Attempt original, string database)
        {
            if (!original.Ended) return Failure(CharacterChallengeRewardExecutionOutcome.Uncertain, original, original.Error);
            try
            {
                // Cancellation is not evidence that a commit failed. Recovery has
                // independent provider timeouts and must establish an outcome.
                if (reservation?.State == InventoryRewardReservationState.Uncertain)
                {
                    await using var resolve = CreateContext(ref database);
                    await using var transaction = await resolve.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    reservation.ResolveOutcome(resolve);
                    await transaction.CommitAsync();
                }
                if (reservation?.State == InventoryRewardReservationState.Quarantined)
                    return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                await using var context = CreateContext(ref database);
                await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var repository = new CharacterChallengeRewardRepository(context, command.CharacterId, catalog, tables, command.Meets);
                if (command.ExpectedAccountId is uint accountId) repository.RequireAccountOwner(accountId);
                var current = repository.ReadStateForExecution();
                bool present;
                switch (command.Kind)
                {
                    case CharacterChallengeRewardCommandKind.Load:
                        present = true;
                        break;
                    case CharacterChallengeRewardCommandKind.Activate:
                    case CharacterChallengeRewardCommandKind.Finish:
                        var attempt = current.Attempts.SingleOrDefault(row => row.AttemptId == command.Identity);
                        present = attempt != null && (command.Kind != CharacterChallengeRewardCommandKind.Finish || attempt.Terminal);
                        if (present && original.Operation is CharacterChallengeAttemptState frozen
                            && (attempt.ChallengeId != frozen.ChallengeId || attempt.TrackId != frozen.TrackId || attempt.Mode != frozen.Mode))
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    case CharacterChallengeRewardCommandKind.Medal:
                        present = context.CharacterChallengeMedalReceipt.AsNoTracking().Any(row => row.Id == command.CharacterId
                            && row.AttemptId == command.Identity && row.Medal == command.Medal);
                        break;
                    case CharacterChallengeRewardCommandKind.LegacyAdvance:
                        var actualChallenge = current.Challenges.SingleOrDefault(row => row.ChallengeId == command.After.ChallengeId);
                        present = original.Candidate != null && actualChallenge == original.Candidate.Challenges
                            .SingleOrDefault(row => row.ChallengeId == command.After.ChallengeId);
                        if (!present && (original.Before == null || actualChallenge != original.Before.Challenges
                            .SingleOrDefault(row => row.ChallengeId == command.After.ChallengeId)))
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    case CharacterChallengeRewardCommandKind.Claim:
                        var reference = catalog.GetReward(command.RewardRowId);
                        var receipt = reference == null ? null : context.CharacterChallengeRewardClaim.AsNoTracking().SingleOrDefault(row =>
                            row.Id == command.CharacterId && row.TrackId == command.TrackId && row.CycleId == command.CycleId && row.Ordinal == reference.Slot.Position);
                        present = receipt != null;
                        if (present && receipt.ClaimId != command.Identity && original.HadWrites)
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    case CharacterChallengeRewardCommandKind.Choose:
                        present = original.Candidate != null && SameTracks(current, original.Candidate);
                        if (!present && original.Before != null && !SameTracks(current, original.Before))
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    case CharacterChallengeRewardCommandKind.ChooseContract:
                        present = original.Candidate != null && CharacterChallengeRewardRepository.SameContracts(current.ContractTracks, original.Candidate.ContractTracks);
                        if (!present && original.Before != null && !CharacterChallengeRewardRepository.SameContracts(current.ContractTracks, original.Before.ContractTracks))
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    case CharacterChallengeRewardCommandKind.ClaimContract:
                        var contractReceipt = context.CharacterContractRewardClaim.AsNoTracking().SingleOrDefault(row => row.Id == command.CharacterId
                            && row.TrackId == command.TrackId && row.CycleId == command.CycleId && row.Ordinal == command.ContractQuote.Award.Ordinal);
                        present = contractReceipt != null;
                        var contractStage = original.Operation as ContractClaimStage;
                        var expectedContracts = original.Candidate?.ContractTracks ?? (contractStage == null ? null
                            : command.ContractBefore.Select(row => row.TrackId == command.TrackId ? contractStage.Track : row).ToArray());
                        if (present)
                        {
                            if (contractReceipt.ClaimId != command.Identity || contractStage == null || expectedContracts == null
                                || !CharacterChallengeRewardRepository.SameContracts(current.ContractTracks, expectedContracts)
                                || !SameContractCurrency(context, command, true))
                                return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        }
                        else if (!CharacterChallengeRewardRepository.SameContracts(current.ContractTracks, original.Before?.ContractTracks ?? command.ContractBefore)
                            || context.CharacterContractAccountOutbox.AsNoTracking().Any(row => row.ClaimId == command.Identity)
                            || !SameContractCurrency(context, command, false))
                            return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                        break;
                    default: throw new InvalidOperationException("Unsupported recovery command.");
                }
                if (!present)
                {
                    repository.VerifyQuestChanges(command.QuestChanges, applied: false);
                    repository.LoadState();
                    await context.SaveChangesAsync(); await unit.CommitAsync();
                    return Failure(CharacterChallengeRewardExecutionOutcome.Failed, original, original.Error) with { Recovered = true, State = current };
                }
                if (command.Kind is CharacterChallengeRewardCommandKind.Activate or CharacterChallengeRewardCommandKind.Medal or CharacterChallengeRewardCommandKind.Finish or CharacterChallengeRewardCommandKind.LegacyAdvance
                    && original.Candidate != null)
                {
                    ushort source = command.After.ChallengeId;
                    if (current.Challenges.SingleOrDefault(row => row.ChallengeId == source)
                        != original.Candidate.Challenges.SingleOrDefault(row => row.ChallengeId == source))
                        return Failure(CharacterChallengeRewardExecutionOutcome.Conflict, original, original.Error);
                }
                object operation;
                if (command.Kind is CharacterChallengeRewardCommandKind.Choose or CharacterChallengeRewardCommandKind.ChooseContract)
                {
                    repository.LoadState(); operation = command.Kind == CharacterChallengeRewardCommandKind.ChooseContract
                        ? current.ContractTracks.Single(row => row.TrackId == command.TrackId)
                        : current.Tracks.Single(row => row.TrackId == command.TrackId);
                }
                else operation = command.Stage(repository, reservation);
                if (context.ChangeTracker.HasChanges())
                    throw new InvalidOperationException("Outcome recovery attempted a new mutation.");
                var questStates = repository.ReadQuestParticipantsForExecution();
                await context.SaveChangesAsync(); await unit.CommitAsync();
                return new(CharacterChallengeRewardExecutionOutcome.Committed, operation, current, original.HadWrites, true, true, original.Error)
                    { QuestStates = questStates };
            }
            catch (Exception error)
            {
                bool conflict = error is InvalidOperationException or JsonException or KeyNotFoundException;
                return Failure(conflict ? CharacterChallengeRewardExecutionOutcome.Conflict
                    : CharacterChallengeRewardExecutionOutcome.Uncertain, original, error);
            }
        }

        private static bool SameContractCurrency(CharacterContext context, CharacterChallengeRewardCommand command, bool applied)
        {
            if (command.ContractQuote.Award.CharacterCurrency == null) return true;
            var delivery = ContractCurrencyDelivery.Capture(command.ContractQuote.Award.CharacterCurrency, command.ContractCurrencyBefore);
            var row = context.CharacterCurrency.AsNoTracking().SingleOrDefault(value => value.Id == command.CharacterId && value.CurrencyId == (byte)delivery.CurrencyId);
            return applied ? row?.Amount == delivery.AfterAmount : (row != null) == delivery.BeforeExists && (row?.Amount ?? 0) == delivery.BeforeAmount;
        }

        private static bool SameTracks(CharacterChallengeRewardsState first, CharacterChallengeRewardsState second)
            => first.Tracks.Count == second.Tracks.Count && first.Tracks.Zip(second.Tracks).All(pair =>
                pair.First.TrackId == pair.Second.TrackId && pair.First.CycleId == pair.Second.CycleId
                && pair.First.Points == pair.Second.Points && pair.First.Revision == pair.Second.Revision
                && pair.First.Active == pair.Second.Active && pair.First.Slots.SequenceEqual(pair.Second.Slots));

        private static CharacterChallengeRewardExecution Failure(CharacterChallengeRewardExecutionOutcome outcome, Attempt attempt, Exception error)
            => new(outcome, null, attempt?.Candidate, false, false, attempt?.Ended ?? true, error);
    }
}
