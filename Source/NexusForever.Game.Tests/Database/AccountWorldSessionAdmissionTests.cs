using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class AccountWorldSessionAdmissionTests
{
    private readonly DbContextOptions<AuthContext> options;
    public AccountWorldSessionAdmissionTests() { }
    internal AccountWorldSessionAdmissionTests(DbContextOptions<AuthContext> options) => this.options = options;
    private const string Key = "00112233445566778899AABBCCDDEEFF";
    [ThreadStatic] private static bool inActorCall;

    [Fact]
    public async Task ReopenedAdmissionsHaveDurableIncreasingEpochsAndPreserveTheCompleteAccountGraph()
    {
        using var f = new Fixture(options); await f.Seed();
        for (ulong epoch = 1; epoch <= 3; epoch++)
        {
            var admitted = await new AccountWorldSessionAdmission(f.Context).AdmitAsync(7, "admission@example.test", Key);
            Assert.Equal(epoch, admitted.WorldSessionEpoch); Assert.Equal(7u, admitted.Id);
            Assert.Equal(new byte[] { 15, 17 }, admitted.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => row.EntitlementId));
            Assert.All(admitted.AccountEntitlement, row => Assert.Equal(1u, row.Amount));
            Assert.Equal(0ul, Assert.Single(admitted.AccountCurrency).Amount); Assert.Equal(1ul, admitted.NextPendingItemId);
            Assert.Equal("unchanged-game-token", admitted.GameToken); Assert.Equal(Key, admitted.SessionKey);
        }
        await using var read = f.Context(); Assert.Equal(3ul, (await read.AccountWorldSession.SingleAsync()).Epoch);
        Assert.Equal(0ul, (await read.Account.AsNoTracking().SingleAsync()).WorldSessionEpoch);
        Assert.Empty(await read.AccountLoyalty.ToArrayAsync()); Assert.Empty(await read.AccountPendingItem.ToArrayAsync());
    }

    [Fact]
    public async Task ConcurrentIndependentServicesAllocateDistinctEpochsOnTheSharedOwner()
    {
        using var f = new Fixture(options); await f.Seed();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => new AccountWorldSessionAdmission(f.Context)
            .AdmitAsync(7, "admission@example.test", Key)));
        Assert.Equal(Enumerable.Range(1, 8).Select(value => (ulong)value), results.Select(row => row.WorldSessionEpoch).Order());
        await using var read = f.Context(); Assert.Equal(8ul, (await read.AccountWorldSession.SingleAsync()).Epoch);
        Assert.Equal(2, await read.AccountEntitlement.CountAsync());
    }

    [Theory]
    [InlineData("id")] [InlineData("email")] [InlineData("key")]
    [InlineData("malformed")] [InlineData("null")] [InlineData("retired")]
    public async Task InvalidOrRetiredRealmCredentialsDoNotAllocateAnEpoch(string invalid)
    {
        using var f = new Fixture(options); await f.Seed();
        string key = invalid switch { "key" => new string('F', 32), "malformed" => "bad", "null" => null, _ => Key };
        if (invalid == "retired") { await using var change = f.Context(); (await change.Account.SingleAsync()).SessionKey = new string('F', 32); await change.SaveChangesAsync(); }
        var result = await new AccountWorldSessionAdmission(f.Context).AdmitAsync(invalid == "id" ? 8u : 7u,
            invalid == "email" ? "other@example.test" : "admission@example.test", key);
        Assert.Null(result); await using var read = f.Context(); Assert.Empty(await read.AccountWorldSession.ToArrayAsync());
        Assert.Equal(2, await read.AccountEntitlement.CountAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task WholeUnitRetryNeverReusesAnUncertainCommittedEpoch(bool afterCommit)
    {
        using var f = new Fixture(options); await f.Seed(); var fault = new CommitFault(afterCommit);
        var configured = new DbContextOptionsBuilder<AuthContext>(f.Options).ReplaceService<IExecutionStrategyFactory, RetryFactory>().AddInterceptors(fault).Options;
        var admitted = await new AccountWorldSessionAdmission(() => new AuthContext(configured)).AdmitAsync(7, "admission@example.test", Key);
        Assert.True(fault.Triggered); Assert.Equal(afterCommit ? 2ul : 1ul, admitted.WorldSessionEpoch);
        await using var read = f.Context(); Assert.Equal(admitted.WorldSessionEpoch, (await read.AccountWorldSession.SingleAsync()).Epoch);
        Assert.Equal(2, await read.AccountEntitlement.CountAsync());
    }

    [Theory]
    [InlineData("suppress")] [InlineData("tracked")] [InlineData("sql")]
    public async Task ACounterWriteMustMatchItsFrozenAllocationBeforeCommit(string mode)
    {
        using var f = new Fixture(options); await f.Seed();
        var fault = new CounterFault(mode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AccountWorldSessionAdmission(() => f.Intercepted(fault))
            .AdmitAsync(7, "admission@example.test", Key));
        await using var read = f.Context(); Assert.Empty(await read.AccountWorldSession.ToArrayAsync());
        Assert.Equal(2, await read.AccountEntitlement.CountAsync());
    }

    [Fact]
    public async Task ExhaustedEpochCannotWrapOrResetTheDurableCounter()
    {
        using var f = new Fixture(options); await f.Seed();
        await using (var seed = f.Context()) { seed.AccountWorldSession.Add(new() { AccountId = 7, Epoch = ulong.MaxValue }); await seed.SaveChangesAsync(); }
        await Assert.ThrowsAsync<OverflowException>(() => new AccountWorldSessionAdmission(f.Context).AdmitAsync(7, "admission@example.test", Key));
        await using var read = f.Context(); Assert.Equal(ulong.MaxValue, (await read.AccountWorldSession.SingleAsync()).Epoch);
    }

    [Fact]
    public async Task RealmCredentialMaterialIsNotIncludedInTheAdmissionSqlOrParameters()
    {
        using var f = new Fixture(options); await f.Seed(); var trace = new SqlTrace();
        Assert.NotNull(await new AccountWorldSessionAdmission(() => f.Intercepted(trace)).AdmitAsync(7, "admission@example.test", Key));
        Assert.NotEmpty(trace.Commands); Assert.DoesNotContain(trace.Commands, text => text.Contains(Key, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ContextFactoriesRunAwayFromTheCallingActorStack()
    {
        using var f = new Fixture(options); await f.Seed();
        var service = new AccountWorldSessionAdmission(() => { Assert.False(inActorCall); return f.Context(); });
        Task<AccountModel> pending;
        inActorCall = true;
        try { pending = service.AdmitAsync(7, "admission@example.test", Key); }
        finally { inActorCall = false; }
        var result = await pending;
        Assert.Equal(1ul, result.WorldSessionEpoch);
    }

    [Fact]
    public async Task PretrackedContextCannotBeBorrowedForAnEpochAllocation()
    {
        using var f = new Fixture(options); await f.Seed(); var context = f.Context(); await context.Account.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AccountWorldSessionAdmission(() => context).AdmitAsync(7, "admission@example.test", Key));
        await using var read = f.Context(); Assert.Empty(await read.AccountWorldSession.ToArrayAsync());
    }

    [Fact]
    public async Task DatabaseIdentityCannotChangeBetweenTheStrategyAndWriteUnit()
    {
        using var f = new Fixture(options); await f.Seed(); int calls = 0;
        var configured = new DbContextOptionsBuilder<AuthContext>().UseSqlite("Data Source=:memory:").Options;
        var service = new AccountWorldSessionAdmission(() => Interlocked.Increment(ref calls) == 1 ? f.Context() : new AuthContext(configured));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdmitAsync(7, "admission@example.test", Key));
        await using var read = f.Context(); Assert.Empty(await read.AccountWorldSession.ToArrayAsync());
    }

    [Fact]
    public void UninitializedAuthDatabaseCannotBeginAnAdmission()
    {
        using var f = new Fixture(options);
        Assert.Throws<InvalidOperationException>(() => { _ = new AuthDatabase().AdmitWorldSessionAsync(7, "admission@example.test", Key); });
        Assert.Throws<ArgumentNullException>(() => new AccountWorldSessionAdmission(null));
    }

    private sealed class SqlTrace : DbCommandInterceptor
    {
        internal ConcurrentQueue<string> Commands = new();
        private void Capture(DbCommand command) => Commands.Enqueue(command.CommandText + string.Join("|", command.Parameters.Cast<DbParameter>().Select(p => p.Value?.ToString())));
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken token = default) { Capture(command); return new(result); }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result) { Capture(command); return result; }
    }
    private sealed class CounterFault(string mode) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (mode == "suppress") return new(InterceptionResult<int>.SuppressWithResult(0));
            if (mode == "tracked") eventData.Context!.ChangeTracker.Entries<AccountWorldSessionModel>().Single().Entity.Epoch++;
            return new(result);
        }
        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken token = default)
        {
            if (mode == "sql") await ((AuthContext)eventData.Context!).AccountWorldSession.ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 9ul), token);
            return result;
        }
    }
    private sealed class Transient : IOException;
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new Retry(dependencies);
    }
    private sealed class Retry(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception error) => error is Transient;
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        internal bool Triggered;
        private void Fail() { if (!Triggered) { Triggered = true; throw new Transient(); } }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken token = default) { if (!after) Fail(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken token = default)
            { if (after) Fail(); return Task.CompletedTask; }
    }
    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-WorldAdmission-" + Guid.NewGuid().ToString("N"));
        internal DbContextOptions<AuthContext> Options { get; }
        internal Fixture(DbContextOptions<AuthContext> options)
        {
            Directory.CreateDirectory(directory);
            Options = options ?? new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(directory, "admission.sqlite")};Pooling=False;Default Timeout=10").Options;
        }
        internal AuthContext Context() => new(Options);
        internal AuthContext Intercepted(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<AuthContext>(Options).AddInterceptors(interceptors).Options);
        internal async Task Seed()
        {
            await using var context = Context(); await context.Database.EnsureCreatedAsync();
            context.Account.Add(new() { Id = 7, Email = "admission@example.test", GameToken = "unchanged-game-token", SessionKey = Key });
            context.AccountEntitlement.AddRange(new AccountEntitlementModel { Id = 7, EntitlementId = 15, Amount = 1 }, new AccountEntitlementModel { Id = 7, EntitlementId = 17, Amount = 1 });
            context.AccountCurrency.Add(new() { Id = 7, CurrencyId = 8, Amount = 0 }); await context.SaveChangesAsync();
        }
        public void Dispose()
        {
            string full = Path.GetFullPath(directory); string temporary = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("NexusForever-WorldAdmission-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture path.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }
}
