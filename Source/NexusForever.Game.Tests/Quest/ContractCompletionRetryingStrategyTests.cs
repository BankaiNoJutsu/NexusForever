using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.Quest;

/// <summary>
/// The production MySQL options install a retrying execution strategy, which makes EF reject any user-initiated
/// transaction that is not executed through <c>Database.CreateExecutionStrategy()</c>. SQLite has no such strategy, so the default
/// executor tests cannot see a recovery path that opens its own transaction outside the strategy. These cases rerun the existing
/// lost-commit-acknowledgement and cancellation cases on SQLite with a retrying strategy installed (it never retries, it only enforces the rule).
/// </summary>
public sealed class ContractCompletionRetryingStrategyTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "nf-contract-retrying-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<CharacterContext> options;

    public ContractCompletionRetryingStrategyTests()
    {
        Directory.CreateDirectory(directory);
        options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "contract.sqlite")};Pooling=False;Default Timeout=10")
            .ReplaceService<IExecutionStrategyFactory, EnforcingStrategyFactory>()
            .Options;
        using var context = new CharacterContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task LostCommitAcknowledgementIsResolvedUnderARetryingStrategy(bool committed)
        => new ContractCompletionExecutorTests(options).LostCommitAcknowledgementIsResolvedBeforeTheSameCommandMayCreditAgain(committed);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task RecoveryReadFailureResumesUnderARetryingStrategy(bool committed)
        => new ContractCompletionExecutorTests(options).RecoveryReadFailureRetainsAnExactSingleUseProofUntilTheDatabaseCanBeObserved(committed);

    [Fact]
    public Task CancellationAfterACommitIsResolvedUnderARetryingStrategy()
        => new ContractCompletionExecutorTests(options).CancellationAfterACommitDoesNotHideItsDurableReceiptOrCauseAnotherCredit();

    private sealed class EnforcingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new EnforcingStrategy(dependencies);
    }

    /// <summary>Claims to retry on failure (so EF enforces the user-transaction rule) but never actually retries.</summary>
    private sealed class EnforcingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => false;
    }
}
