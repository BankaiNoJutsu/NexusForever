using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterAtomicSaveTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CharacterAtomicSaveTests() { }
    internal CharacterAtomicSaveTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task EnrollmentRunsSynchronouslyOnTheCallersStackAndInsideTheOwnedSerializableTransaction()
    {
        using var f = new Fixture(options); int thread = Environment.CurrentManagedThreadId; var stack = new AsyncLocal<object> { Value = new object() };
        object marker = stack.Value; bool enrolled = false; bool? settled = null;
        Task task = CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            Assert.Equal(thread, Environment.CurrentManagedThreadId); Assert.Same(marker, stack.Value);
            Assert.Equal(IsolationLevel.Serializable, context.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            context.OnSaveSettled(result => settled = result); Enroll(context); enrolled = true;
        });
        Assert.True(enrolled); await task; Assert.True(settled);
        using var read = f.Context(); Assert.Equal(13u, read.Character.Single().TotalXp);
    }

    [Theory]
    [InlineData("tracked")] [InlineData("transaction")] [InlineData("rejected")]
    public async Task InvalidContextFactoriesCannotDisposeAnotherOwnersContext(string mode)
    {
        using var f = new Fixture(options); await using var foreign = f.Context();
        await using var transaction = mode == "transaction" ? await foreign.Database.BeginTransactionAsync() : null;
        if (mode == "tracked") _ = foreign.Character.Single();
        if (mode == "rejected") foreign.RejectSave(new IOException("Already rejected by its owner."));
        int calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => foreign, 42, _ => calls++));
        Assert.Equal(0, calls); Assert.Single(await foreign.Character.AsNoTracking().ToArrayAsync());
        if (transaction != null) await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ConcurrentFactoryReuseDoesNotDisposeOrStealTheFirstActiveUnit()
    {
        using var f = new Fixture(options); var pause = new PauseFlush(); var context = f.Intercepted(pause); int calls = 0;
        Task first = CharacterAtomicSave.Execute(() => context, 42, c => { calls++; Enroll(c); });
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => context, 42, _ => calls++));
        }
        finally { pause.Release.TrySetResult(); }
        await first; Assert.Equal(1, calls); using var read = f.Context(); Assert.Equal(13u, read.Character.Single().TotalXp);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => context, 42, _ => calls++));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AnActiveOrdinarySaveCannotBeStolenEvenBeforeItHasOpenedATransaction()
    {
        using var f = new Fixture(options); var pause = new PauseFlush(clearTracking: true);
        await using var context = f.Intercepted(pause); Enroll(context);
        Task ordinary = context.SaveChangesAsync(); await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        int calls = 0;
        try
        {
            Assert.Null(context.Database.CurrentTransaction); Assert.Empty(context.ChangeTracker.Entries());
            await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => context, 42, _ => calls++));
        }
        finally { pause.Release.TrySetResult(); }
        await ordinary; Assert.Equal(0, calls); Assert.Single(await context.Character.AsNoTracking().ToArrayAsync());
    }

    [Theory]
    [InlineData("zero")] [InlineData("missing")] [InlineData("deleted")]
    public async Task NoGameplayCallbackRunsForAnInvalidCharacterOwner(string mode)
    {
        using var f = new Fixture(options); int calls = 0;
        if (mode == "deleted") { using var seed = f.Context(); seed.Character.Single().DeleteTime = DateTime.UtcNow; seed.SaveChanges(); }
        Task action() => CharacterAtomicSave.Execute(() => f.Context(), mode == "zero" ? 0ul : mode == "missing" ? 43ul : 42ul, _ => calls++);
        if (mode == "zero") await Assert.ThrowsAsync<ArgumentOutOfRangeException>(action);
        else await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("character")] [InlineData("currency")] [InlineData("objective")]
    [InlineData("item")] [InlineData("owner-delete")]
    public async Task MappedCharacterOwnershipAlsoCoversIndirectObjectiveAndItemKeys(string mode)
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            if (mode == "character") context.Character.Add(new() { Id = 43, Name = "Other owner" });
            if (mode == "currency") context.CharacterCurrency.Add(new() { Id = 43, CurrencyId = 1, Amount = 10 });
            if (mode == "objective") context.CharacterQuestObjective.Add(new() { Id = 43, QuestId = 9001, Index = 0, Progress = 1 });
            if (mode == "item") context.Item.Add(new() { Id = 900, OwnerId = 43, ItemId = 9001, StackCount = 1 });
            if (mode == "owner-delete") context.Character.Remove(context.Character.Single());
        }));
        using var read = f.Context(); Assert.Single(read.Character); Assert.Empty(read.CharacterCurrency); Assert.Empty(read.Item); Assert.Empty(read.CharacterQuestObjective);
    }

    [Fact]
    public async Task PartialStubsPreserveUntouchedSqlColumnsAndProviderDatePrecision()
    {
        using var f = new Fixture(options); DateTime instant = new DateTime(2026, 10, 31, 2, 3, 4, DateTimeKind.Utc).AddTicks(9876543); DateTime expected;
        using (var seed = f.Context())
        {
            // Ask the actual provider for its normal conversion as a reference.
            seed.Character.Single().LastOnline = instant; seed.SaveChanges(); seed.ChangeTracker.Clear(); expected = seed.Character.Single().LastOnline.Value;
        }
        await CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            var stub = new CharacterModel { Id = 42, LastOnline = instant }; context.Attach(stub); context.Entry(stub).Property(row => row.LastOnline).IsModified = true;
        });
        using var read = f.Context(); Assert.Equal(expected, read.Character.Single().LastOnline); Assert.Equal("Character reward repository", read.Character.Single().Name);
    }

    [Fact]
    public async Task ServerGeneratedDefaultsMaySettleButUnrelatedTargetsCannotChangeWithThem()
    {
        using var f = new Fixture(options);
        await CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            context.CharacterCostume.Add(new() { Id = 42, Index = 1, VisibilityMask = 1 });
        });
        using var read = f.Context(); Assert.NotEqual(default, read.CharacterCostume.Single().Timestamp);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ConfiguredRetryStrategiesNeverReplayActorCallbacksAfterSyncOrAsyncFailures(bool asynchronous)
    {
        using var f = new Fixture(options); int calls = 0;
        var configured = new DbContextOptionsBuilder<CharacterContext>(f.Options).AddInterceptors(new ThrowFlush(asynchronous));
        // Keep the injected strategy scoped to these options without adding a
        // replacement-service profile to EF's process-wide provider cache.
        using (var provider = f.Context())
        {
            if (provider.Database.IsMySql())
                new MySqlDbContextOptionsBuilder(configured).ExecutionStrategy(dependencies => new RetryStrategy(dependencies));
            else
                new SqliteDbContextOptionsBuilder(configured).ExecutionStrategy(dependencies => new RetryStrategy(dependencies));
        }
        CharacterContext Factory() => new(configured.Options);
        await Assert.ThrowsAnyAsync<Exception>(() => CharacterAtomicSave.Execute(Factory, 42, context =>
        {
            calls++; Enroll(context); if (!asynchronous) throw new IOException("Synchronous actor failure.");
        }));
        Assert.Equal(1, calls); using var read = f.Context(); Assert.Equal(0u, read.Character.Single().TotalXp);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EnrollmentCannotPerformItsOwnFlushOrCommitAndContinueAfterCatchingTheError(bool commit)
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            Enroll(context);
            Assert.Throws<InvalidOperationException>(() => { if (commit) context.Database.CurrentTransaction!.Commit(); else context.SaveChanges(); });
        }));
        using var read = f.Context(); Assert.Equal(0u, read.Character.Single().TotalXp);
    }

    private static void Enroll(CharacterContext context)
    { var stub = new CharacterModel { Id = 42, TotalXp = 13 }; context.Attach(stub); context.Entry(stub).Property(row => row.TotalXp).IsModified = true; }
    private sealed class PauseFlush(bool clearTracking = false) : SaveChangesInterceptor
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { if (clearTracking) eventData.Context!.ChangeTracker.Clear(); Entered.TrySetResult(); await Release.Task; return result; }
    }
    private sealed class ThrowFlush(bool active) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => active ? throw new IOException("Asynchronous atomic save failure.") : new(result);
    }
    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is IOException;
    }
}
