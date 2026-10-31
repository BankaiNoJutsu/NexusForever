using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Static.Entity;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeRewardExecutorTests
{
    private readonly DbContextOptions<CharacterContext> options;
    [ThreadStatic] private static bool inActorCall;
    public CharacterChallengeRewardExecutorTests() { }
    internal CharacterChallengeRewardExecutorTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task LoadAndActivationReturnCommittedCopiedChallengeStateAwayFromTheActorStack()
    {
        using var f = new Fixture(options);
        CharacterContext Context() { Assert.False(inActorCall); return f.Context(); }
        var executor = new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables);
        Task<CharacterChallengeRewardExecution> task;
        inActorCall = true;
        try { task = executor.ExecuteAsync(CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true)); }
        finally { inActorCall = false; }
        var result = await task;
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.True(result.Changed); Assert.True(result.OriginalUnitEnded);
        Assert.Equal(f.State(16), Assert.Single(result.State.Challenges));
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeState>)result.State.Challenges).Clear());
        var load = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Load(42));
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, load.Outcome); Assert.False(load.Changed);
        Assert.Equal(result.State.Challenges, load.State.Challenges);
    }

    [Theory]
    [InlineData("activate")] [InlineData("medal")] [InlineData("finish")] [InlineData("choose")]
    public async Task LostCommitAcknowledgementsResolveThroughFreshUnitsWithoutRepeatingTheMutation(string kind)
    {
        using var f = new Fixture(options);
        CharacterChallengeRewardCommand command;
        if (kind == "activate") command = CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true);
        else
        {
            await f.Begin(16);
            var before = f.State(16);
            command = kind switch
            {
                "medal" => CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1, LastRewardTier = 0 }, 0, new(1.25f, 71), DateTime.UtcNow),
                "finish" => CharacterChallengeRewardCommand.Finish(before, before with { Activated = false, ActiveSeconds = 0 }),
                _ => CharacterChallengeRewardCommand.Choose(42, 15, new Dictionary<uint, ulong> { [16] = 1 })
            };
        }
        var fault = new CommitFault(after: true);
        int contexts = 0;
        var executor = new CharacterChallengeRewardExecutor(() => { Interlocked.Increment(ref contexts); return f.Intercepted(fault); }, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.True(result.Recovered); Assert.True(result.Changed);
        Assert.True(result.OriginalUnitEnded); Assert.True(contexts >= 3);
        using var read = f.Context();
        Assert.Single(read.CharacterChallengeAttempt);
        if (kind == "medal") { Assert.Single(read.CharacterChallengeMedalReceipt); Assert.Equal(125u, read.CharacterChallengeRewardTrack.Single().Points); }
        if (kind == "finish") Assert.True(read.CharacterChallengeAttempt.Single().Terminal);
        if (kind == "choose") { Assert.Equal(2, read.CharacterChallengeRewardTrack.Count()); Assert.Equal(15u, read.CharacterChallengeRewardTrack.Single(row => row.Active).TrackId); }
    }

    [Fact]
    public async Task CommittedClaimDoesNotPublishUntilTheActorAcknowledgesTheResult()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0);
        var command = Claim(input);
        var executor = new CharacterChallengeRewardExecutor(f.Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(command, held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome);
        Assert.Equal(InventoryRewardReservationState.Committed, held.State);
        Assert.Throws<ArgumentException>(() => f.Inventory.GetItem(InventoryLocation.Inventory, 0)); Assert.Empty(f.SessionProxy.Invocations);
        using (var read = f.Context()) { Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim); }
        Assert.True(held.PublishCommitted()); Assert.NotNull(f.Inventory.GetItem(InventoryLocation.Inventory, 0));
        Assert.NotEmpty(f.SessionProxy.Invocations);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClaimCommitFaultsResolveBeforeRetryAndRetainEveryAllocatedId(bool after)
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0); var command = Claim(input);
        var fault = new CommitFault(after);
        var executor = new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(command, held);
        Assert.True(result.Recovered); Assert.True(result.OriginalUnitEnded);
        Assert.Equal(after ? CharacterChallengeRewardExecutionOutcome.Committed : CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome);
        Assert.Equal(after ? InventoryRewardReservationState.Committed : InventoryRewardReservationState.ReadyToRetry, held.State);
        Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId")); Assert.Empty(f.SessionProxy.Invocations);
        if (!after) result = await executor.ExecuteAsync(command, held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId"));
        Assert.True(held.PublishCommitted()); using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Fact]
    public async Task CancellationAfterTheDatabaseCommitStillReturnsTheCommittedOutcome()
    {
        using var f = new Fixture(options); using var cancellation = new CancellationTokenSource();
        var fault = new CommitFault(true, () => cancellation.Cancel());
        var executor = new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true), cancellationToken: cancellation.Token);
        Assert.True(cancellation.IsCancellationRequested); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome);
        Assert.True(result.Recovered); using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt);
    }

    [Fact]
    public async Task PrecancelledExecutionDoesNotOpenAContext()
    {
        using var f = new Fixture(options); int contexts = 0; using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var executor = new CharacterChallengeRewardExecutor(() => { contexts++; return f.Context(); }, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Load(42), cancellationToken: cancellation.Token);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome); Assert.IsAssignableFrom<OperationCanceledException>(result.Error);
        Assert.Equal(0, contexts); Assert.True(result.OriginalUnitEnded);
    }

    [Fact]
    public async Task FrozenPrerequisitesAndVersionsDoNotReadChangedCallerCollectionsOnTheWorker()
    {
        using var f = new Fixture(options);
        f.Tracks.Single(row => row.Id == 16).PrerequisiteId = 999; f.RebuildCatalog();
        var prerequisites = new List<uint> { 999 };
        var command = CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true, prerequisites);
        prerequisites.Clear();
        var executor = new CharacterChallengeRewardExecutor(f.Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(command); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome);
        Assert.Equal(CharacterChallengeRewardMode.CharacterTrack, Assert.IsType<CharacterChallengeAttemptState>(result.Operation).Mode);
        var versions = new Dictionary<uint, ulong> { [16] = 1 };
        var choose = CharacterChallengeRewardCommand.Choose(42, 15, versions); versions[16] = 700;
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await executor.ExecuteAsync(choose)).Outcome);
    }

    [Fact]
    public async Task AmbiguousSelectionFollowedByAnotherWriterRequiresReloadInsteadOfOverwritingIt()
    {
        using var f = new Fixture(options); await f.Begin(16);
        var fault = new CommitFault(true, () =>
        {
            using var foreign = f.Context();
            foreach (var row in foreign.CharacterChallengeRewardTrack) { row.Active = row.TrackId == 16; row.Revision++; }
            foreign.SaveChanges();
        });
        var executor = new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Choose(42, 15, new Dictionary<uint, ulong> { [16] = 1 }));
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Conflict, result.Outcome);
        using var read = f.Context(); Assert.Equal(16u, read.CharacterChallengeRewardTrack.Single(row => row.Active).TrackId);
    }

    [Fact]
    public async Task AnUnavailableRecoveryKeepsTheDeliveryHeldAndCanBeResolvedByAFreshExecution()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0); var command = Claim(input);
        var fault = new CommitFault(true); int contexts = 0;
        var executor = new CharacterChallengeRewardExecutor(() => Interlocked.Increment(ref contexts) > 2
            ? throw new IOException("Owned recovery unavailable.") : f.Intercepted(fault), f.Catalog, f.Tables);
        var failed = await executor.ExecuteAsync(command, held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Uncertain, failed.Outcome); Assert.Equal(InventoryRewardReservationState.Uncertain, held.State);
        Assert.False(held.Cancel()); Assert.False(f.Inventory.TryAcquireSaveLease(out _));
        var fresh = new CharacterChallengeRewardExecutor(f.Context, f.Catalog, f.Tables);
        var unproved = await fresh.ExecuteAsync(command, held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Uncertain, unproved.Outcome); Assert.False(unproved.OriginalUnitEnded);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fresh.ResumeAsync(command, failed with { }, held));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fresh.ResumeAsync(Claim(input), failed, held));
        var resolved = await fresh.ResumeAsync(command, failed, held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, resolved.Outcome); Assert.True(resolved.Recovered);
        Assert.True(resolved.Changed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fresh.ResumeAsync(command, failed, held));
        Assert.Equal(InventoryRewardReservationState.Committed, held.State); Assert.True(held.PublishCommitted());
        using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Fact]
    public async Task TheProviderStrategyRetriesTheEntireFreshUnitAfterVerifiedAbsence()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0); var fault = new CommitFault(false, transient: true); int contexts = 0;
        CharacterContext Context()
        {
            Interlocked.Increment(ref contexts);
            return new(new DbContextOptionsBuilder<CharacterContext>(f.Options).ReplaceService<IExecutionStrategyFactory, RetryFactory>().AddInterceptors(fault).Options);
        }
        var executor = new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(Claim(input), held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.True(contexts >= 5);
        Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId")); Assert.True(held.PublishCommitted());
        using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)] [InlineData(19u)] [InlineData(20u)]
    [InlineData(21u)] [InlineData(22u)] [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task EverySupportedCatalogSurvivesActivationAcknowledgementLossWithoutResampling(uint track)
    {
        using var f = new Fixture(options); var fault = new CommitFault(true);
        var executor = new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables);
        var command = CharacterChallengeRewardCommand.Activate(null, f.Activated(track), Guid.NewGuid().ToString("N"), true,
            f.Catalog.Tracks.Select(row => row.PrerequisiteId));
        var result = await executor.ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.True(result.Recovered);
        var state = Assert.Single(result.State.Tracks); Assert.Equal(track, state.TrackId);
        Assert.Equal(f.Catalog.GetTrack(track).Slots.Select(row => row.Cost), state.Slots.Select(row => row.RequiredPoints));
        using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt);
        Assert.Equal(state.Slots.Select(row => row.RewardRowId), read.CharacterChallengeRewardSlot.OrderBy(row => row.Ordinal).Select(row => row.RewardRowId));
    }

    [Theory]
    [InlineData("database")] [InlineData("tracked")]
    public async Task ADriftingOrAlreadyEnrolledContextFactoryCannotWriteAnotherDatabase(string fault)
    {
        using var f = new Fixture(options); int calls = 0;
        CharacterContext Context()
        {
            int call = Interlocked.Increment(ref calls);
            if (fault == "tracked") { var tracked = f.Context(); tracked.Character.Single(); return tracked; }
            if (call == 1) return f.Context();
            using var reference = f.Context();
            var builder = new DbConnectionStringBuilder { ConnectionString = reference.Database.GetDbConnection().ConnectionString };
            if (reference.Database.ProviderName!.Contains("MySql"))
            {
                builder["Database"] = "nexus_assessment_rejected_context_do_not_create";
                return new(new DbContextOptionsBuilder<CharacterContext>().UseMySql(builder.ConnectionString, new MySqlServerVersion(new Version(8, 0, 0))).Options);
            }
            builder["Data Source"] = Path.Combine(Path.GetTempPath(), "NexusForever-Rejected-" + Guid.NewGuid().ToString("N") + ".sqlite");
            return new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(builder.ConnectionString).Options);
        }
        var executor = new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true));
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome); Assert.IsType<InvalidOperationException>(result.Error);
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack);
    }

    [Fact]
    public async Task AForeignClaimWinnerIsReportedAsConflictBeforeAnyLocalIdsAreAllocated()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0); var choice = f.Catalog.GetReward(input.Row).Reward.GetChoice(0);
        using (var foreign = f.Context())
        {
            foreign.CharacterChallengeRewardSlot.Single(row => row.Ordinal == 0).Claimed = true;
            foreign.CharacterChallengeRewardTrack.Single().Revision++;
            var after = new InventoryRewardItemSnapshot(900, 42, choice.ItemId, InventoryLocation.Inventory, 0, choice.Count, 1, 1, 0, true, null, null);
            var plan = new InventoryRewardDeliveryPlan(42, choice.ItemId, choice.Count, [new(false, choice.Count, null, after)]);
            foreign.Item.Add(after.ToModel());
            foreign.CharacterChallengeRewardClaim.Add(new() { Id = 42, TrackId = 16, CycleId = input.Cycle, Ordinal = 0,
                RewardRowId = input.Row, Choice = 0, ClaimId = Guid.NewGuid().ToString("N"), CreatedUtc = DateTime.UtcNow, DeliveryVersion = 1, DeliveryJson = plan.ToJson() });
            foreign.SaveChanges();
        }
        var executor = new CharacterChallengeRewardExecutor(f.Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(Claim(input), held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Conflict, result.Outcome); Assert.True(result.OriginalUnitEnded);
        Assert.Empty(f.ManagerProxy.GetInvocations("get_NextItemId")); Assert.Equal(InventoryRewardReservationState.Prepared, held.State); Assert.True(held.Cancel());
        using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ChangedOrConsumedCommittedItemsQuarantineRecoveryWithoutResurrectingThem(bool consume)
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0);
        var fault = new CommitFault(true, () =>
        {
            using var changed = f.Context();
            if (consume) changed.Item.ExecuteDelete();
            else { changed.Item.Single().BagIndex = 1; changed.SaveChanges(); }
        });
        var executor = new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(Claim(input), held);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Conflict, result.Outcome);
        Assert.Equal(InventoryRewardReservationState.Quarantined, held.State); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.False(held.PublishCommitted()); Assert.Empty(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"));
        using var read = f.Context(); Assert.Single(read.CharacterChallengeRewardClaim); Assert.Equal(consume ? 0 : 1, read.Item.Count());
        if (!consume) Assert.Equal(1u, read.Item.Single().BagIndex);
    }

    [Fact]
    public void CharacterDatabaseContextFactoryRequiresInitialization()
        => Assert.Throws<InvalidOperationException>(() => new CharacterDatabase().CreateContext());

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ContextCleanupFailureCannotEraseAConfirmedTransactionOutcome(bool committed)
    {
        using var f = new Fixture(options); int calls = 0;
        var fault = new CommitFault(false);
        CharacterContext Context()
        {
            if (Interlocked.Increment(ref calls) != 2) return f.Context();
            var configured = committed ? f.Options : new DbContextOptionsBuilder<CharacterContext>(f.Options).AddInterceptors(fault).Options;
            return new CleanupFaultContext(configured);
        }
        var executor = new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables);
        var result = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true));
        Assert.Equal(committed ? CharacterChallengeRewardExecutionOutcome.Committed : CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome);
        Assert.True(result.OriginalUnitEnded); Assert.IsType<IOException>(result.Error);
        using var read = f.Context(); Assert.Equal(committed ? 1 : 0, read.CharacterChallengeAttempt.Count());
        if (!committed) Assert.True(result.Recovered);
    }

    [Fact]
    public async Task DatabaseIdentityRemainsPinnedAcrossIndependentExecutions()
    {
        using var f = new Fixture(options); bool changed = false; string rejectedFile = null;
        CharacterContext Context()
        {
            if (!changed) return f.Context();
            using var reference = f.Context();
            var builder = new DbConnectionStringBuilder { ConnectionString = reference.Database.GetDbConnection().ConnectionString };
            if (reference.Database.ProviderName!.Contains("MySql"))
            {
                builder["Database"] = "nexus_assessment_rejected_context_do_not_create";
                return new(new DbContextOptionsBuilder<CharacterContext>().UseMySql(builder.ConnectionString, new MySqlServerVersion(new Version(8, 0, 0))).Options);
            }
            rejectedFile = Path.Combine(Path.GetTempPath(), "NexusForever-Rejected-" + Guid.NewGuid().ToString("N") + ".sqlite");
            builder["Data Source"] = rejectedFile;
            return new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(builder.ConnectionString).Options);
        }
        var executor = new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await executor.ExecuteAsync(CharacterChallengeRewardCommand.Load(42))).Outcome);
        changed = true;
        var rejected = await executor.ExecuteAsync(CharacterChallengeRewardCommand.Load(42));
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, rejected.Outcome); Assert.IsType<InvalidOperationException>(rejected.Error);
        if (rejectedFile != null) Assert.False(File.Exists(rejectedFile));
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt);
    }

    private sealed class CleanupFaultContext(DbContextOptions<CharacterContext> options) : CharacterContext(options)
    {
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            throw new IOException("Owned context cleanup acknowledgement fault.");
        }
    }

    private static CharacterChallengeRewardCommand Claim((uint Row, ulong Cycle, ulong Revision) input)
        => CharacterChallengeRewardCommand.Claim(42, 16, input.Cycle, input.Revision, input.Row, 0,
            Guid.NewGuid().ToString("N"), DateTime.UtcNow);

    private sealed class ProbeTransientException : IOException;
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryStrategy(dependencies);
    }
    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception error) => error is ProbeTransientException || error.InnerException is ProbeTransientException;
    }
    private sealed class CommitFault(bool after, Action onFault = null, bool transient = false) : DbTransactionInterceptor
    {
        private int remaining = 1;
        private void Fail()
        {
            if (Interlocked.Exchange(ref remaining, 0) == 0) return;
            onFault?.Invoke();
            if (transient) throw new ProbeTransientException();
            throw new IOException("Owned commit acknowledgement fault.");
        }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!after) Fail(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (after) Fail(); return Task.CompletedTask; }
    }
}
