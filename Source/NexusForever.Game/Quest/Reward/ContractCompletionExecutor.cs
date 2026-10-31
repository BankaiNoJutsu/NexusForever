using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Quest.Reward
{
    internal enum ContractCompletionOutcome { Committed, Failed, Conflict, Uncertain }
    internal sealed record ContractCompletionExecution(ContractCompletionOutcome Outcome, ContractCompletionStage State,
        bool Changed, bool Recovered, bool OriginalUnitEnded, Exception Error = null);

    // No actor callbacks run on this executor. Its production queue/adoption and
    // ordinary Quest reward participants must be integrated before live use.
    internal sealed class ContractCompletionExecutor(Func<CharacterContext> contexts, ContractRewardCatalog catalog)
    {
        private string configuredDatabase;
        private sealed class Attempt
        {
            internal ContractCompletionStage State;
            internal bool HadWrites, CommitStarted, Committed, Ended;
            internal Exception Error;
        }
        private sealed record Proof(ContractCompletionExecutor Owner, ContractCompletionCommand Command, string Database, Attempt Attempt)
        {
            internal int Resumed;
        }
        private static readonly ConditionalWeakTable<ContractCompletionExecution, Proof> proofs = new();
        private static readonly ConditionalWeakTable<CharacterContext, object> contextOwners = new();

        internal Task<ContractCompletionExecution> ExecuteAsync(ContractCompletionCommand command, CancellationToken cancellation = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            return Task.Run(() => Run(command, cancellation, null));
        }
        internal static bool IsCompletionOf(ContractCompletionExecution result, ContractCompletionExecutor owner,
            ContractCompletionCommand command, string database)
            => result != null && proofs.TryGetValue(result, out var proof) && ReferenceEquals(proof.Owner, owner)
                && ReferenceEquals(proof.Command, command) && proof.Database == database;

        internal Task<ContractCompletionExecution> ResumeAsync(ContractCompletionCommand command, ContractCompletionExecution previous,
            CancellationToken cancellation = default)
        {
            if (previous?.Outcome != ContractCompletionOutcome.Uncertain || !previous.OriginalUnitEnded
                || !proofs.TryGetValue(previous, out var proof) || !ReferenceEquals(proof.Owner, this)
                || !ReferenceEquals(proof.Command, command) || Interlocked.CompareExchange(ref proof.Resumed, 1, 0) != 0)
                throw new InvalidOperationException("Contract recovery requires the exact executor, command and terminal uncertain result.");
            return Task.Run(() => Run(command, cancellation, proof));
        }

        private async Task<ContractCompletionExecution> Run(ContractCompletionCommand command, CancellationToken cancellation, Proof resume)
        {
            CharacterContext strategyContext = null; string database = resume?.Database;
            Attempt last = resume?.Attempt; ContractCompletionExecution result = null;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                strategyContext = CreateContext(ref database);
                result = await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    if (last != null && NeedsRecovery(last))
                    {
                        var recovered = await Recover(command, last, database);
                        if (recovered.Outcome != ContractCompletionOutcome.Failed) return recovered;
                    }
                    last = new Attempt(); return await ExecuteUnit(command, last, database, cancellation);
                });
            }
            catch (Exception error)
            {
                if (last != null && NeedsRecovery(last)) result = await Recover(command, last, database);
                else result = Failure(last?.Ended != false ? ContractCompletionOutcome.Failed : ContractCompletionOutcome.Uncertain, last, error);
            }
            finally
            {
                if (strategyContext != null)
                    try { await strategyContext.DisposeAsync(); }
                    catch (Exception error) { if (result != null && result.Error == null) result = result with { Error = error }; }
            }
            if (database != null) proofs.Add(result, new(this, command, database, last ?? new Attempt { Ended = true }));
            return result;
        }

        private static bool NeedsRecovery(Attempt attempt) => attempt.Ended && (attempt.HadWrites || attempt.CommitStarted);
        private CharacterContext CreateContext(ref string database)
        {
            var context = contexts() ?? throw new InvalidOperationException("Contract context factory returned null.");
            lock (contextOwners)
            {
                // A rejected duplicate may still belong to a live operation.
                // Never dispose that other operation's context on this path.
                if (contextOwners.TryGetValue(context, out _)) throw new InvalidOperationException("Contract context factory reused a previously owned context.");
                contextOwners.Add(context, this);
            }
            try
            {
                string actual = Item.GetRewardDatabaseKey(context);
                if (database != null && database != actual) throw new InvalidOperationException("Contract execution changed its retained database.");
                string configured = Interlocked.CompareExchange(ref configuredDatabase, actual, null) ?? actual;
                database ??= actual;
                if (configured != actual || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any()
                    || context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Contract execution requires a fresh context in its retained database.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private async Task<ContractCompletionExecution> ExecuteUnit(ContractCompletionCommand command, Attempt attempt, string database, CancellationToken cancellation)
        {
            CharacterContext context = null; IDbContextTransaction transaction = null;
            bool contextEnded = true, transactionEnded = false;
            try
            {
                cancellation.ThrowIfCancellationRequested(); context = CreateContext(ref database); contextEnded = false;
                transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellation);
                attempt.State = new ContractCompletionRepository(context, command.CharacterId, catalog).Complete(command);
                attempt.HadWrites = context.ChangeTracker.HasChanges();
                await context.SaveChangesAsync(cancellation); attempt.CommitStarted = true;
                await transaction.CommitAsync(cancellation); attempt.Committed = true;
            }
            catch (Exception error) { attempt.Error = error; }
            finally
            {
                if (transaction != null)
                    try { await transaction.DisposeAsync(); transactionEnded = true; }
                    catch (Exception error) { attempt.Error ??= error; }
                if (context != null)
                    try { await context.DisposeAsync(); contextEnded = true; }
                    catch (Exception error) { attempt.Error ??= error; }
                attempt.Ended = attempt.Committed || transactionEnded || contextEnded;
            }
            if (!attempt.Committed) throw attempt.Error ?? new InvalidOperationException("Contract unit did not reach a terminal commit.");
            return new(ContractCompletionOutcome.Committed, attempt.State, attempt.HadWrites, false, true, attempt.Error);
        }

        private async Task<ContractCompletionExecution> Recover(ContractCompletionCommand command, Attempt original, string database)
        {
            if (!original.Ended) return Failure(ContractCompletionOutcome.Uncertain, original, original.Error);
            try
            {
                // Cancellation cannot establish whether a commit succeeded.
                // Recovery uses fresh bounded provider operations independently.
                await using var context = CreateContext(ref database);
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var repository = new ContractCompletionRepository(context, command.CharacterId, catalog);
                bool present = await context.CharacterContractCompletion.AsNoTracking().AnyAsync(row => row.Id == command.CharacterId && row.AcceptanceId == command.AcceptanceId);
                if (!present)
                {
                    new QuestTransactionParticipants(context, command.CharacterId, command.Changes).VerifyQuestChanges(command.Changes, false);
                    await transaction.RollbackAsync();
                    return Failure(ContractCompletionOutcome.Failed, original, original.Error) with { Recovered = true };
                }
                var observed = repository.Complete(command);
                if (observed.Applied || context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Contract recovery attempted a new mutation.");
                await context.SaveChangesAsync(); await transaction.CommitAsync();
                return new(ContractCompletionOutcome.Committed, observed, original.HadWrites, true, true, original.Error);
            }
            catch (Exception error)
            {
                return Failure(error is InvalidOperationException or JsonException or KeyNotFoundException
                    ? ContractCompletionOutcome.Conflict : ContractCompletionOutcome.Uncertain, original, error);
            }
        }
        private static ContractCompletionExecution Failure(ContractCompletionOutcome outcome, Attempt attempt, Exception error)
            => new(outcome, attempt?.State, false, false, attempt?.Ended ?? true, error);
    }
}
