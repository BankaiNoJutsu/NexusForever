using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Database;

/// <summary>
/// <see cref="CharacterAtomicSave"/> runs the actor enrollment inside the provider's execution strategy but refuses to replay it ("Actor enrollment cannot be retried"), and its asynchronous
/// commit stage is not a strategy retry at all. Nothing exercised either branch: the Contract test strategy never retries. These cases install a strategy that really retries once on a
/// transient fault (SQLite, so Source/SQLite evidence only; the production MySQL strategy decides transience differently) and pin that a transient fault never causes a second enrollment or a
/// double commit, and that the unit releases its ownership afterwards.
/// </summary>
public sealed class CharacterAtomicSaveRetryingStrategyTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "nf-atomic-retrying-" + Guid.NewGuid().ToString("N"));
    private readonly FaultOnce fault = new();
    private readonly DbContextOptions<CharacterContext> options;

    public CharacterAtomicSaveRetryingStrategyTests()
    {
        Directory.CreateDirectory(directory);
        options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "atomic.sqlite")};Pooling=False;Default Timeout=10")
            .ReplaceService<IExecutionStrategyFactory, ReplayingStrategyFactory>()
            .AddInterceptors(fault)
            .Options;
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }

    [Fact]
    public async Task ATransientFaultBeforeEnrollmentIsNotReplayedAndLeavesNothingCommitted()
    {
        using var f = new Fixture(options);
        int enrolled = 0;
        fault.Arm("SELECT");
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CharacterAtomicSave.Execute(() => f.Context(), 42, context => { enrolled++; Enroll(context); }));

        Assert.Contains("cannot be retried", error.Message);
        Assert.Equal(1, fault.Thrown);
        Assert.Equal(0, enrolled);
        using (var read = f.Context())
            Assert.Equal(0u, read.Character.Single().TotalXp);

        // the failed unit released its ownership: a later unit enrolls once and commits
        await CharacterAtomicSave.Execute(() => f.Context(), 42, context => { enrolled++; Enroll(context); });
        Assert.Equal(1, enrolled);
        using var after = f.Context();
        Assert.Equal(13u, after.Character.Single().TotalXp);
    }

    [Fact]
    public async Task ATransientFaultDuringTheCommitStageIsNotReplayedAndRollsBack()
    {
        using var f = new Fixture(options);
        int enrolled = 0;
        fault.Arm("UPDATE");
        DbUpdateException error = await Assert.ThrowsAsync<DbUpdateException>(
            () => CharacterAtomicSave.Execute(() => f.Context(), 42, context => { enrolled++; Enroll(context); }));

        Assert.IsType<TransientTestException>(error.InnerException);
        Assert.Equal(1, fault.Thrown);
        Assert.Equal(1, enrolled);
        using (var read = f.Context())
            Assert.Equal(0u, read.Character.Single().TotalXp);

        await CharacterAtomicSave.Execute(() => f.Context(), 42, context => { enrolled++; Enroll(context); });
        Assert.Equal(2, enrolled);
        using var after = f.Context();
        Assert.Equal(13u, after.Character.Single().TotalXp);
    }

    private static void Enroll(CharacterContext context)
    {
        var stub = new CharacterModel { Id = 42, TotalXp = 13 };
        context.Attach(stub);
        context.Entry(stub).Property(row => row.TotalXp).IsModified = true;
    }

    private sealed class TransientTestException() : Exception("Injected transient fault.");

    /// <summary>Throws <see cref="TransientTestException"/> on the first command whose text starts with the armed verb.</summary>
    private sealed class FaultOnce : DbCommandInterceptor
    {
        private string verb;
        public int Thrown;

        public void Arm(string commandVerb)
        {
            verb = commandVerb;
            Thrown = 0;
        }

        private void Check(DbCommand command)
        {
            if (verb == null || Thrown != 0 || !command.CommandText.TrimStart().StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                return;

            Thrown++;
            verb = null;
            throw new TransientTestException();
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Check(command); return result; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Check(command); return ValueTask.FromResult(result); }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        { Check(command); return result; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { Check(command); return ValueTask.FromResult(result); }
    }

    private sealed class ReplayingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new ReplayingStrategy(dependencies);
    }

    /// <summary>Retries (once, immediately) when the operation fails with <see cref="TransientTestException"/>.</summary>
    private sealed class ReplayingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientTestException;
    }
}
