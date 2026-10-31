using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.API.Character.Client;
using NexusForever.Database.Query;
using NexusForever.Database.Query.Model;
using NexusForever.Database.Query.Repository;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Character.Game.Character;
using NexusForever.Server.Character.Network.Internal.Handler.Player;
using QueryIdentity = NexusForever.Server.Character.Identity;
using ApiCharacter = NexusForever.API.Model.Character.Character;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterQueryBootstrapTests : IDisposable
{
    private readonly string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nf-query-bootstrap-{Guid.NewGuid():N}.sqlite");
    private static QueryIdentity Identity(ushort realm = 1) => new() { Id = 44, RealmId = realm };

    [Fact]
    public async Task IndependentMissingContextsAtomicallyBootstrapAndKeepBothPayloadColumns()
    {
        await CreateDatabase();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        using var api = new ApiHandler(async request =>
        {
            if (Interlocked.Increment(ref requests) == 2) ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return Response(request);
        });
        await using var first = Context();
        await using var second = Context();
        var a = Manager(first, api);
        var b = Manager(second, api);
        await Task.WhenAll(
            Task.Run(() => new PlayerWorldZoneUpdatedHandler(first, a).Handle(new() { Identity = new() { Id = 44, RealmId = 1 }, WorldZoneId = 123 })),
            Task.Run(() => new PlayerStatUpdatedHandler(second, b).Handle(new() { Identity = new() { Id = 44, RealmId = 1 }, Stat = Stat.Level, Value = 25 })));
        await using var verify = Context();
        CharacterModel row = Assert.Single(await verify.Character.ToListAsync());
        Assert.Equal((ushort)123, row.WorldZoneId);
        Assert.Equal(25u, row.Level);
        Assert.Equal(2, requests);
    }

    [Fact]
    public async Task ApiSnapshotThatReturnsLateCannotOverwriteConcurrentWinner()
    {
        await CreateDatabase();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        using var api = new ApiHandler(async request =>
        {
            if (Interlocked.Increment(ref requests) == 1)
            {
                entered.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            return Response(request);
        });
        await using var first = Context();
        await using var second = Context();
        Task login = Manager(first, api).UpdateCharacterRemoteAsync<DateTime?>(Identity(), c => c.LastOnline, null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try { await Manager(second, api).UpdateCharacterRemoteAsync(Identity(), c => c.WorldZoneId, (ushort)99); }
        finally { release.SetResult(); }
        await login;
        await using var verify = Context();
        CharacterModel row = Assert.Single(await verify.Character.ToListAsync());
        Assert.Equal((ushort)99, row.WorldZoneId);
        Assert.Null(row.LastOnline);
    }

    [Fact]
    public async Task ExistingTrackedNoOpPayloadWinsWithoutRevertingUnrelatedUpdates()
    {
        await CreateDatabase();
        await using var first = Context();
        first.Character.Add(new() { CharacterId = 44, RealmId = 1, LastOnline = null, Level = 1 });
        await first.SaveChangesAsync();
        await using (var other = Context())
        {
            CharacterModel row = await other.Character.SingleAsync();
            row.Level = 50;
            row.LastOnline = DateTime.UtcNow;
            await other.SaveChangesAsync();
        }
        using var api = new ApiHandler(_ => throw new InvalidOperationException("Existing rows must not fetch the API."));
        await Manager(first, api).UpdateCharacterRemoteAsync<DateTime?>(Identity(), c => c.LastOnline, null);
        await using var verify = Context();
        CharacterModel winner = await verify.Character.SingleAsync();
        Assert.Null(winner.LastOnline);
        Assert.Equal(50u, winner.Level);
    }

    [Fact]
    public async Task FailedPayloadSaveRollsBackBootstrapInsteadOfLeavingApiSeed()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = new FailingQueryContext(Options());
        await Assert.ThrowsAsync<DbUpdateException>(() => Manager(context, api)
            .UpdateCharacterRemoteAsync(Identity(), c => c.WorldZoneId, (ushort)99));
        await using var verify = Context();
        Assert.Empty(await verify.Character.ToListAsync());
    }

    [Fact]
    public async Task ExistingOuterTransactionRetainsCommitOwnership()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = Context();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await Manager(context, api).UpdateCharacterRemoteAsync(Identity(), c => c.Level, 33u);
            Assert.Equal(33u, (await context.Character.SingleAsync()).Level);
            await transaction.RollbackAsync();
        }
        await using var verify = Context();
        Assert.Empty(await verify.Character.ToListAsync());
    }

    [Fact]
    public async Task CharacterRealmKeysRemainIndependent()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var a = Context();
        await using var b = Context();
        await Manager(a, api).UpdateCharacterRemoteAsync(Identity(1), c => c.Level, 11u);
        await Manager(b, api).UpdateCharacterRemoteAsync(Identity(2), c => c.Level, 22u);
        await using var verify = Context();
        var rows = await verify.Character.OrderBy(c => c.RealmId).ToListAsync();
        Assert.Equal(new uint[] { 11, 22 }, rows.Select(c => c.Level));
    }

    [Fact]
    public async Task PublicRemoteLookupUsesWinningRowAndCachesWithoutPendingDuplicateInsert()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var a = Context();
        await using var b = Context();
        var first = await Manager(a, api).GetCharacterRemoteAsync(Identity());
        var second = await Manager(b, api).GetCharacterRemoteAsync(Identity());
        Assert.Equal(first.Identity, second.Identity);
        Assert.DoesNotContain(a.ChangeTracker.Entries<CharacterModel>(), e => e.State == EntityState.Added);
        Assert.DoesNotContain(b.ChangeTracker.Entries<CharacterModel>(), e => e.State == EntityState.Added);
        Assert.Equal(0, await b.SaveChangesAsync());
        await using var verify = Context();
        Assert.Single(await verify.Character.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingApiCharacterDoesNotCreateOrUpdate(bool lookup)
    {
        await CreateDatabase();
        using var api = new ApiHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        await using var context = Context();
        var manager = Manager(context, api);
        if (lookup) Assert.Null(await manager.GetCharacterRemoteAsync(Identity()));
        else await manager.UpdateCharacterRemoteAsync(Identity(), c => c.WorldZoneId, (ushort)99);
        Assert.Empty(await context.Character.ToListAsync());
    }

    [Fact]
    public async Task BootstrapDoesNotSuppressUnrelatedDatabaseConstraintFailure()
    {
        await CreateDatabase();
        await using var context = Context();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER deny_seed BEFORE INSERT ON character BEGIN SELECT RAISE(ABORT, 'seed denied'); END;");
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => Manager(context, api)
            .UpdateCharacterRemoteAsync(Identity(), c => c.Level, 2u));
        Assert.Contains("seed denied", error.Message);
        Assert.Empty(await context.Character.ToListAsync());
    }

    [Fact]
    public async Task NonLevelStatMessageDoesNotFetchOrWrite()
    {
        await CreateDatabase();
        using var api = new ApiHandler(_ => throw new InvalidOperationException("Unexpected API fetch."));
        await using var context = Context();
        await new PlayerStatUpdatedHandler(context, Manager(context, api)).Handle(new() { Stat = Stat.Health, Value = 99 });
        Assert.Empty(await context.Character.ToListAsync());
    }

    [Fact]
    public async Task PresenceAndGuildHandlersUpdateOnlyTheirOwnedFields()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = Context();
        var manager = Manager(context, api);
        var identity = new NexusForever.Network.Internal.Message.Shared.Identity { Id = 44, RealmId = 1 };
        await new PlayerLoggedInHandler(context, manager).Handle(new() { Identity = identity });
        Assert.Null((await context.Character.SingleAsync()).LastOnline);
        await new PlayerGuildAssociationUpdatedHandler(context, manager).Handle(new() { Identity = identity, GuildName = "Preserved Guild" });
        await new PlayerLoggedOutHandler(context, manager).Handle(new() { Identity = identity });
        await using var verify = Context();
        CharacterModel row = await verify.Character.SingleAsync();
        Assert.Equal("Preserved Guild", row.GuildName);
        Assert.NotNull(row.LastOnline);
        Assert.Equal(1u, row.Level);
        Assert.Equal((ushort)1, row.WorldZoneId);
    }

    [Fact]
    public async Task ConfiguredRetryStrategyOwnsBootstrapTransaction()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = new QueryContext(RetryOptions());
        await Manager(context, api).UpdateCharacterRemoteAsync(Identity(), c => c.Level, 12u);
        await using var verify = Context();
        Assert.Equal(12u, (await verify.Character.SingleAsync()).Level);
    }

    [Fact]
    public async Task TransientFailureAfterSeedRollsBackAndRetriesEntireOwnedUnit()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = new TransientQueryContext(RetryOptions());
        await Manager(context, api).UpdateCharacterRemoteAsync(Identity(), c => c.Level, 37u);
        Assert.Equal(2, context.SaveAttempts);
        await using var verify = Context();
        Assert.Equal(37u, Assert.Single(await verify.Character.ToListAsync()).Level);
    }

    [Fact]
    public async Task CallerRetryStrategyRetainsOuterTransactionRollbackOwnership()
    {
        await CreateDatabase();
        using var api = new ApiHandler(r => Task.FromResult(Response(r)));
        await using var context = new QueryContext(RetryOptions());
        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await Manager(context, api).UpdateCharacterRemoteAsync(Identity(), c => c.Level, 41u);
            await transaction.RollbackAsync();
        });
        await using var verify = Context();
        Assert.Empty(await verify.Character.ToListAsync());
    }

    private DbContextOptions<QueryContext> Options() => new DbContextOptionsBuilder<QueryContext>()
        .UseSqlite($"Data Source={file};Pooling=False;Default Timeout=10").Options;
    private DbContextOptions<QueryContext> RetryOptions() => new DbContextOptionsBuilder<QueryContext>()
        .UseSqlite($"Data Source={file};Pooling=False;Default Timeout=10")
        .ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>().Options;
    private QueryContext Context() => new(Options());
    private async Task CreateDatabase() { await using var c = Context(); await c.Database.EnsureCreatedAsync(); }
    private static CharacterManager Manager(QueryContext context, HttpMessageHandler handler)
    {
        var services = new ServiceCollection().AddTransient<NexusForever.Server.Character.Game.Character.Character>().BuildServiceProvider();
        return new CharacterManager(services, new CharacterRepository(context),
            new CharacterAPIClient(new HttpClient(handler, false) { BaseAddress = new Uri("http://character-api.test") }), context);
    }
    private static HttpResponseMessage Response(HttpRequestMessage request)
    {
        string[] path = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        ushort realm = ushort.Parse(path[1]);
        ulong id = ulong.Parse(path[2]);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ApiCharacter
        {
            Identity = new() { Id = id, RealmId = realm }, RealmId = realm,
            IdentityName = new() { Name = $"Player{id}", RealmName = $"Realm{realm}" },
            WorldZoneId = 1, Stats = [new() { Stat = Stat.Level, Value = 1 }], LastOnline = DateTime.UtcNow.AddDays(-1)
        }) };
    }
    public void Dispose() { if (File.Exists(file)) File.Delete(file); }
    private sealed class ApiHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => respond(request);
    }
    private sealed class FailingQueryContext(DbContextOptions<QueryContext> options) : QueryContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken token = default) => throw new DbUpdateException("injected payload save failure");
    }
    private sealed class TransientBootstrapException : Exception { }
    private sealed class TransientQueryContext(DbContextOptions<QueryContext> options) : QueryContext(options)
    {
        public int SaveAttempts { get; private set; }
        public override Task<int> SaveChangesAsync(CancellationToken token = default)
        {
            if (++SaveAttempts == 1) throw new TransientBootstrapException();
            return base.SaveChangesAsync(token);
        }
    }
    private sealed class RetryingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryingStrategy(dependencies);
    }
    private sealed class RetryingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientBootstrapException;
    }
}
