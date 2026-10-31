using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.API.Account.Client;
using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Database.Friendship.Repository;
using NexusForever.Game.Static.Chat;
using NexusForever.Server.Friendship.Game.Account;
using FriendshipAccount = NexusForever.Server.Friendship.Game.Account.Account;

namespace NexusForever.Game.Tests.Database;

public sealed class FriendshipAccountBootstrapTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-friendship-bootstrap-{Guid.NewGuid():N}.sqlite");

    [Fact]
    public async Task ConcurrentIndependentApiMissesCreateOneAccountAndKeepScopedCache()
    {
        await CreateDatabase();
        int requests = 0;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var api = new ApiHandler(async _ =>
        {
            if (Interlocked.Increment(ref requests) == 2) ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return Response();
        });
        await using var a = Context();
        await using var b = Context();
        var first = Manager(a, api);
        var second = Manager(b, api);
        FriendshipAccount[] results = await Task.WhenAll(Task.Run(() => first.GetAccountRemoteAsync(7)), Task.Run(() => second.GetAccountRemoteAsync(7)));
        Assert.All(results, r => Assert.Equal(7u, r.Id));
        Assert.Same(results[0], await first.GetAccountRemoteAsync(7));
        Assert.Equal(2, requests);
        Assert.Equal(0, await a.SaveChangesAsync());
        Assert.Equal(0, await b.SaveChangesAsync());
        await using var verify = Context();
        Assert.Single(await verify.Account.ToListAsync());
    }

    [Fact]
    public async Task LateApiBootstrapCannotResetWinnerPreferencesOrNickname()
    {
        await CreateDatabase();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var api = new ApiHandler(async _ =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return Response();
        });
        await using var first = Context();
        Task<FriendshipAccount> pending = Manager(first, api).GetAccountRemoteAsync(7);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await using var second = Context();
            second.Account.Add(new()
            {
                AccountId = 7, Email = "person@example.test", Nickname = "Preserved",
                Status = "custom status", Presence = (AccountPresenceState)2,
                BlockAccountFriendRequests = true, InvitePrivilegesSuspended = true,
                LastOnline = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await second.SaveChangesAsync();
        }
        finally { release.SetResult(); }
        FriendshipAccount result = await pending;
        Assert.Equal("Preserved", result.Nickname);
        Assert.True(result.BlockAccountFriendRequests);
        Assert.True(result.InvitePrivilegesSuspended);
        Assert.Equal("custom status", result.Status);
        Assert.Equal((AccountPresenceState)2, result.Presence);
        Assert.Equal(2026, result.LastOnline!.Value.Year);
        Assert.Equal(0, await first.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BootstrapAndOutboxFollowOuterCommitOrRollback(bool commit)
    {
        await CreateDatabase();
        await using var context = Context();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await new AccountRepository(context).EnsureAccountAsync(new() { AccountId = 7, Email = "person@example.test" });
            context.InternalMessage.Add(new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Type = "test-message", Payload = "{}" });
            await context.SaveChangesAsync();
            if (commit) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
        }
        await using var verify = Context();
        Assert.Equal(commit ? 1 : 0, await verify.Account.CountAsync());
        Assert.Equal(commit ? 1 : 0, await verify.InternalMessage.CountAsync());
    }

    [Fact]
    public async Task DuplicatePrimaryKeyKeepsWinnerAndLoadsExistingFriendRelationships()
    {
        await CreateDatabase();
        await using (var seed = Context())
        {
            seed.Account.AddRange(new AccountModel { AccountId = 7, Email = "person@example.test" },
                new AccountModel { AccountId = 8, Email = "friend@example.test" });
            seed.FriendAccount.Add(new() { Id = 8, InviterAccountId = 7, InviteeAccountId = 8 });
            seed.AccountFriend.Add(new() { AccountId = 7, FriendAccountId = 8 });
            await seed.SaveChangesAsync();
        }
        await using var context = Context();
        AccountModel winner = await new AccountRepository(context).EnsureAccountAsync(new() { AccountId = 7, Email = "stale@example.test" });
        Assert.Equal("person@example.test", winner.Email);
        Assert.Equal(8u, Assert.Single(winner.Friends).FriendAccountId);
    }

    [Fact]
    public async Task UniqueEmailConflictCannotBeMistakenForSameAccount()
    {
        await CreateDatabase();
        await using var context = Context();
        context.Account.Add(new() { AccountId = 7, Email = "person@example.test" });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => new AccountRepository(context)
            .EnsureAccountAsync(new() { AccountId = 8, Email = "person@example.test" }));
        Assert.Equal(7u, (await context.Account.SingleAsync()).AccountId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoteLookupByIdOrEmailBootstrapsOnce(bool email)
    {
        await CreateDatabase();
        int requests = 0;
        using var api = new ApiHandler(_ => { requests++; return Task.FromResult(Response()); });
        await using var context = Context();
        var manager = Manager(context, api);
        FriendshipAccount account = email ? await manager.GetAccountByEmailAsync("person@example.test") : await manager.GetAccountRemoteAsync(7);
        Assert.Equal(7u, account.Id);
        Assert.Same(account, await manager.GetAccountByEmailAsync("person@example.test"));
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task MissingApiAccountDoesNotSeedOrPublish()
    {
        await CreateDatabase();
        using var api = new ApiHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        await using var context = Context();
        Assert.Null(await Manager(context, api).GetAccountRemoteAsync(7));
        Assert.Empty(await context.Account.ToListAsync());
        Assert.Empty(await context.InternalMessage.ToListAsync());
    }

    private FriendshipContext Context() => new(new DbContextOptionsBuilder<FriendshipContext>()
        .UseSqlite($"Data Source={file};Pooling=False;Default Timeout=10").Options);
    private async Task CreateDatabase() { await using var c = Context(); await c.Database.EnsureCreatedAsync(); }
    private static AccountManager Manager(FriendshipContext context, HttpMessageHandler handler)
    {
        var services = new ServiceCollection().AddTransient<FriendshipAccount>(sp => new FriendshipAccount(NullLogger<FriendshipAccount>.Instance, sp, null, null)).BuildServiceProvider();
        return new AccountManager(new AccountRepository(context),
            new AccountAPIClient(new HttpClient(handler, false) { BaseAddress = new Uri("http://account-api.test") }), services);
    }
    private static HttpResponseMessage Response() => new(HttpStatusCode.OK)
    { Content = JsonContent.Create(new NexusForever.API.Model.Account.Account { Id = 7, Email = "person@example.test" }) };
    public void Dispose() { if (File.Exists(file)) File.Delete(file); }
    private sealed class ApiHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response(request);
    }
}
