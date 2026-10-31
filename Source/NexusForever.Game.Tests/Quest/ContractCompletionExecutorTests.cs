using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Quest.Reward;
using Fixture = NexusForever.Game.Tests.Quest.ContractCompletionRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractCompletionExecutorTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractCompletionExecutorTests() { }
    internal ContractCompletionExecutorTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(9880u)] [InlineData(9884u)]
    public async Task BothContractOwnersExecuteTheFrozenUnitAndReplayWithoutRecrediting(uint quest)
    {
        using var f = new Fixture(options, quest); var command = f.Command(); var executor = new ContractCompletionExecutor(() => f.Context(), f.Catalog);
        var result = await executor.ExecuteAsync(command);
        Assert.True(result.Outcome == ContractCompletionOutcome.Committed, result.Error?.ToString());
        Assert.True(result.Changed); Assert.True(result.OriginalUnitEnded); Assert.False(result.Recovered);
        var replay = await executor.ExecuteAsync(command); Assert.Equal(ContractCompletionOutcome.Committed, replay.Outcome); Assert.False(replay.Changed);
        Assert.Equal(result.State.Track, replay.State.Track); using var read = f.Context(); Assert.Single(read.CharacterContractCompletion);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LostCommitAcknowledgementIsResolvedBeforeTheSameCommandMayCreditAgain(bool committed)
    {
        using var f = new Fixture(options); var command = f.Command(); int contexts = 0;
        var executor = new ContractCompletionExecutor(() => Interlocked.Increment(ref contexts) == 2 ? f.Context(new CommitFault(committed)) : f.Context(), f.Catalog);
        var result = await executor.ExecuteAsync(command); Assert.True(result.Recovered); Assert.True(result.OriginalUnitEnded);
        Assert.Equal(committed ? ContractCompletionOutcome.Committed : ContractCompletionOutcome.Failed, result.Outcome);
        using (var read = f.Context()) Assert.Equal(committed ? 1 : 0, read.CharacterContractCompletion.Count());
        var retry = await executor.ExecuteAsync(command); Assert.Equal(ContractCompletionOutcome.Committed, retry.Outcome); Assert.Equal(!committed, retry.Changed);
        using var final = f.Context(); Assert.Single(final.CharacterContractCompletion); Assert.Equal(command.Points, final.CharacterContractRewardTrack.Single().Points);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoveryReadFailureRetainsAnExactSingleUseProofUntilTheDatabaseCanBeObserved(bool committed)
    {
        using var f = new Fixture(options); var command = f.Command(); int contexts = 0;
        var executor = new ContractCompletionExecutor(() => Interlocked.Increment(ref contexts) switch
        {
            2 => f.Context(new CommitFault(committed)), 3 => throw new IOException("Contract recovery observation unavailable."), _ => f.Context()
        }, f.Catalog);
        var uncertain = await executor.ExecuteAsync(command); Assert.Equal(ContractCompletionOutcome.Uncertain, uncertain.Outcome); Assert.True(uncertain.OriginalUnitEnded);
        var equivalent = new ContractCompletionCommand(command.CharacterId, command.AcceptanceId, command.Source, command.Points,
            command.Evaluation, command.RewardChoice, command.CreatedUtc, command.Changes);
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ResumeAsync(equivalent, uncertain));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ContractCompletionExecutor(() => f.Context(), f.Catalog).ResumeAsync(command, uncertain));
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ResumeAsync(command, uncertain with { }));
        var resolved = await executor.ResumeAsync(command, uncertain); Assert.True(resolved.Outcome == ContractCompletionOutcome.Committed, resolved.Error?.ToString());
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ResumeAsync(command, uncertain));
        using var read = f.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Equal(command.Points, read.CharacterContractRewardTrack.Single().Points);
    }

    [Fact]
    public async Task CancellationAfterACommitDoesNotHideItsDurableReceiptOrCauseAnotherCredit()
    {
        using var f = new Fixture(options); using var cancellation = new CancellationTokenSource(); int contexts = 0; var command = f.Command();
        var executor = new ContractCompletionExecutor(() => Interlocked.Increment(ref contexts) == 2 ? f.Context(new CommitFault(true, cancellation.Cancel)) : f.Context(), f.Catalog);
        var result = await executor.ExecuteAsync(command, cancellation.Token);
        Assert.True(cancellation.IsCancellationRequested); Assert.Equal(ContractCompletionOutcome.Committed, result.Outcome); Assert.True(result.Recovered);
        using var read = f.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Equal(command.Points, read.CharacterContractRewardTrack.Single().Points);
    }

    [Fact]
    public async Task CancellationBeforeExecutionDoesNotCreateAContextOrAnyRewardRows()
    {
        using var f = new Fixture(options); using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); int contexts = 0;
        var executor = new ContractCompletionExecutor(() => { contexts++; return f.Context(); }, f.Catalog);
        var result = await executor.ExecuteAsync(f.Command(), cancellation.Token);
        Assert.Equal(ContractCompletionOutcome.Failed, result.Outcome); Assert.IsAssignableFrom<OperationCanceledException>(result.Error); Assert.Equal(0, contexts); f.AssertNoCredit();
    }

    [Fact]
    public async Task AFactoryCannotReuseItsStrategyContextForTheWritingUnit()
    {
        using var f = new Fixture(options); using var shared = f.Context();
        var result = await new ContractCompletionExecutor(() => shared, f.Catalog).ExecuteAsync(f.Command());
        Assert.Equal(ContractCompletionOutcome.Failed, result.Outcome); Assert.IsType<InvalidOperationException>(result.Error); f.AssertNoCredit();
    }

    [Fact]
    public async Task AContextAlreadyOwnedByAnotherExecutorIsRejectedWithoutDisposingThatOwnersContext()
    {
        using var f = new Fixture(options); using var shared = f.Context(); var command = f.Command();
        using var release = new ManualResetEventSlim(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int calls = 0;
        var first = new ContractCompletionExecutor(() =>
        {
            if (Interlocked.Increment(ref calls) == 1) return shared;
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Contract test context gate expired.");
            return f.Context();
        }, f.Catalog);
        var running = first.ExecuteAsync(command);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var rejected = await new ContractCompletionExecutor(() => shared, f.Catalog).ExecuteAsync(command);
            Assert.Equal(ContractCompletionOutcome.Failed, rejected.Outcome);
            Assert.True(await shared.Database.CanConnectAsync()); // The live owner was not disposed by rejection.
        }
        finally { release.Set(); }
        Assert.Equal(ContractCompletionOutcome.Committed, (await running).Outcome);
        using var read = f.Context(); Assert.Single(read.CharacterContractCompletion);
    }

    private sealed class CommitFault(bool committed, Action afterCommit = null) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
            => committed ? ValueTask.FromResult(result) : throw new IOException("Contract before-commit fault.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!committed) return Task.CompletedTask;
            afterCommit?.Invoke(); return Task.FromException(new IOException("Contract commit acknowledgement lost."));
        }
    }
}
