using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.API.Account.Client;
using NexusForever.API.Character.Client;
using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Database.Friendship.Repository;
using NexusForever.Game.Static.Chat;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Server.Friendship.Network.Internal;
using FriendshipAccount = NexusForever.Server.Friendship.Game.Account.Account;
using FriendshipCharacter = NexusForever.Server.Friendship.Game.Character.Character;
using LoginHandler = NexusForever.Server.Friendship.Network.Internal.Handler.Player.PlayerLoggedInHandler;
using LogoutHandler = NexusForever.Server.Friendship.Network.Internal.Handler.Player.PlayerLoggedOutHandler;

namespace NexusForever.Game.Tests.Database;

public sealed class FriendshipPresenceTransactionTests
{
    private readonly DbContextOptions<FriendshipContext> options;
    public FriendshipPresenceTransactionTests() { }
    internal FriendshipPresenceTransactionTests(DbContextOptions<FriendshipContext> options) => this.options = options;

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CachedOppositePresenceCannotSuppressTheRealLoginOrLogoutWrite(bool login)
    {
        using var f = new Fixture(options); await f.Seed(login);
        await using var context = f.Context(); using var services = Services(context);
        var accounts = services.GetRequiredService<AccountManager>();
        var characters = services.GetRequiredService<CharacterManager>();
        var cached = await characters.GetCharacterAsync(new() { Id = 101, RealmId = 1 });
        var account = await cached.GetAccountAsync();
        Assert.Equal(login ? AccountPresenceState.Available : AccountPresenceState.Invisible, account.Presence);
        await using (var foreign = f.Context())
        {
            var row = await foreign.Account.SingleAsync();
            row.Presence = login ? AccountPresenceState.Invisible : AccountPresenceState.Available;
            row.ActiveCharacterId = login ? null : 101; row.ActiveRealmId = login ? null : (ushort)1;
            row.LastOnline = login ? Fixture.Earlier : null;
            (await foreign.Character.SingleAsync()).LastOnline = row.LastOnline;
            await foreign.SaveChangesAsync();
        }
        if (login) await new LoginHandler(context, services.GetRequiredService<OutboxMessagePublisher>(), characters, accounts)
            .Handle(new PlayerLoggedInMessage { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 } });
        else await new LogoutHandler(context, characters).Handle(new PlayerLoggedOutMessage { Identity = new() { Id = 101, RealmId = 1 } });
        await using var read = f.Context(); var stored = await read.Account.SingleAsync();
        Assert.Equal(login ? AccountPresenceState.Available : AccountPresenceState.Invisible, stored.Presence);
        Assert.Equal(login ? 101ul : null, stored.ActiveCharacterId); Assert.Equal(login ? (ushort)1 : null, stored.ActiveRealmId);
        Assert.Equal(login, stored.LastOnline == null); Assert.Equal(login, (await read.Character.SingleAsync()).LastOnline == null);
        Assert.True(await read.InternalMessage.CountAsync() >= 3); AssertPreferences(stored);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailureBeforeThePresenceFlushRollsBackStateAndNotificationsTogether(bool login)
    {
        using var f = new Fixture(options); await f.Seed(!login);
        var fault = new FlushFault();
        await using (var context = f.Intercepted(fault))
        {
            using var services = Services(context);
            if (login) await Assert.ThrowsAsync<IOException>(() => new LoginHandler(context, services.GetRequiredService<OutboxMessagePublisher>(),
                services.GetRequiredService<CharacterManager>(), services.GetRequiredService<AccountManager>()).Handle(new()
                    { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 } }));
            else await Assert.ThrowsAsync<IOException>(() => new LogoutHandler(context, services.GetRequiredService<CharacterManager>())
                .Handle(new() { Identity = new() { Id = 101, RealmId = 1 } }));
            Assert.True(fault.Triggered);
        }
        await using var read = f.Context(); var stored = await read.Account.SingleAsync();
        Assert.Equal(login ? AccountPresenceState.Invisible : AccountPresenceState.Available, stored.Presence);
        Assert.Equal(login ? null : 101ul, stored.ActiveCharacterId); Assert.Empty(await read.InternalMessage.ToArrayAsync());
        AssertPreferences(stored);
    }

    private static void AssertPreferences(AccountModel stored)
    {
        Assert.Equal("preserved status", stored.Status); Assert.Equal("Preserved", stored.Nickname);
        Assert.True(stored.BlockAccountFriendRequests); Assert.True(stored.InvitePrivilegesSuspended);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task WholeUnitRetryDropsRolledBackModelsCachesAndOutboxRows(bool login, bool afterFlush)
    {
        using var f = new Fixture(options); await f.Seed(!login);
        var flush = new RetryFlushFault(); var commit = new RetryCommitFault();
        var configured = new DbContextOptionsBuilder<FriendshipContext>(f.Options)
            .ReplaceService<IExecutionStrategyFactory, RetryFactory>().AddInterceptors(afterFlush ? commit : flush).Options;
        await using (var context = new FriendshipContext(configured))
        {
            using var services = Services(context);
            await Handle(context, services, login);
        }
        Assert.True(afterFlush ? commit.Triggered : flush.Triggered);
        await using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(login ? 101ul : null, row.ActiveCharacterId); Assert.Equal(login, row.LastOnline == null);
        var messages = await read.InternalMessage.ToArrayAsync(); Assert.Equal(login ? 8 : 3, messages.Length);
        Assert.Equal(messages.Length, messages.Select(message => message.Id).Distinct().Count()); AssertPreferences(row);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IndependentOverlappingHandlersWaitForOwnershipBeforePublishingFinalPresence(bool loginFirst)
    {
        using var f = new Fixture(options); await f.Seed(!loginFirst);
        var barrier = new PauseFirstSave();
        await using var first = f.Intercepted(barrier); await using var second = f.Context();
        using var a = Services(first); using var b = Services(second);
        Task firstTask = Task.Run(() => Handle(first, a, loginFirst));
        await barrier.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task secondTask = Task.Run(async () => { started.SetResult(); await Handle(second, b, !loginFirst); });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(100); Assert.False(secondTask.IsCompleted);
        }
        finally { barrier.Release.TrySetResult(); }
        await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromSeconds(20));
        await using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(loginFirst ? null : 101ul, row.ActiveCharacterId); Assert.Equal(loginFirst, row.LastOnline != null);
        Assert.Equal(11, await read.InternalMessage.CountAsync()); AssertPreferences(row);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OldDifferentCharacterOrRealmLogoutCannotClearTheCurrentAccountIdentity(bool anotherRealm)
    {
        using var f = new Fixture(options); await f.Seed(false);
        ushort realm = anotherRealm ? (ushort)2 : (ushort)1; ulong characterId = anotherRealm ? 101ul : 102ul;
        await using (var seed = f.Context())
        {
            seed.Character.Add(new() { CharacterId = characterId, RealmId = realm, RealmName = anotherRealm ? "OtherRealm" : "Synthetic",
                Name = "CurrentCharacter", AccountId = 7 });
            var account = await seed.Account.SingleAsync(); account.ActiveCharacterId = characterId; account.ActiveRealmId = realm;
            account.Presence = AccountPresenceState.Available; account.LastOnline = null; await seed.SaveChangesAsync();
        }
        await using (var context = f.Context()) { using var services = Services(context); await Handle(context, services, false); }
        await using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(characterId, row.ActiveCharacterId); Assert.Equal(realm, row.ActiveRealmId);
        Assert.Equal(AccountPresenceState.Available, row.Presence); Assert.Null(row.LastOnline);
        Assert.NotNull((await read.Character.SingleAsync(c => c.CharacterId == 101 && c.RealmId == 1)).LastOnline);
        Assert.Null((await read.Character.SingleAsync(c => c.CharacterId == characterId && c.RealmId == realm)).LastOnline);
        Assert.Single(await read.InternalMessage.ToArrayAsync()); AssertPreferences(row);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HandlerCannotDiscardOrPersistUnrelatedPendingScopeChanges(bool login)
    {
        using var f = new Fixture(options); await f.Seed(!login);
        await using var context = f.Context(); var pending = await context.Account.SingleAsync(); pending.Status = "unrelated pending edit";
        using var services = Services(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(context, services, login));
        Assert.Equal("unrelated pending edit", pending.Status); Assert.True(context.ChangeTracker.HasChanges());
        await using var read = f.Context(); AssertPreferences(await read.Account.SingleAsync()); Assert.Empty(await read.InternalMessage.ToArrayAsync());
    }

    [Theory]
    [InlineData("account")] [InlineData("character")] [InlineData("ownership")]
    public async Task MissingOrForeignLoginDependenciesCannotPublishPartialPresence(string missing)
    {
        using var f = new Fixture(options);
        if (missing == "account") { await using var schema = f.Context(); await schema.Database.EnsureCreatedAsync(); }
        else
        {
            await f.Seed(false); await using var changed = f.Context();
            if (missing == "character") changed.Character.Remove(await changed.Character.SingleAsync());
            else
            {
                changed.Account.Add(new() { AccountId = 8, Email = "other-presence@example.test", Nickname = "Other" });
                (await changed.Character.SingleAsync()).AccountId = 8;
            }
            await changed.SaveChangesAsync();
        }
        await using (var context = f.Context())
        {
            using var services = Services(context);
            if (missing == "ownership") await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(context, services, true));
            else await Handle(context, services, true);
        }
        await using var read = f.Context(); Assert.Empty(await read.InternalMessage.ToArrayAsync());
        var account = await read.Account.SingleOrDefaultAsync(row => row.AccountId == 7);
        if (missing == "account") Assert.Null(account);
        else { Assert.Null(account.ActiveCharacterId); Assert.Equal(AccountPresenceState.Invisible, account.Presence); AssertPreferences(account); }
    }

    [Fact]
    public async Task MissingLogoutCharacterDoesNotBootstrapAnAccountOrPublishNotifications()
    {
        using var f = new Fixture(options); await using var context = f.Context(); await context.Database.EnsureCreatedAsync();
        using var services = Services(context); await Handle(context, services, false);
        Assert.Empty(await context.Account.ToArrayAsync()); Assert.Empty(await context.InternalMessage.ToArrayAsync());
    }

    private static Task Handle(FriendshipContext context, ServiceProvider services, bool login)
        => login ? new LoginHandler(context, services.GetRequiredService<OutboxMessagePublisher>(),
            services.GetRequiredService<CharacterManager>(), services.GetRequiredService<AccountManager>()).Handle(new()
                { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 } })
            : new LogoutHandler(context, services.GetRequiredService<CharacterManager>()).Handle(new() { Identity = new() { Id = 101, RealmId = 1 } });

    private sealed class ProbeTransientException : IOException;
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryStrategy(dependencies);
    }
    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is ProbeTransientException;
    }
    private sealed class RetryFlushFault : SaveChangesInterceptor
    {
        internal bool Triggered;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Triggered && eventData.Context!.ChangeTracker.Entries<InternalMessageModel>().Any(row => row.State == EntityState.Added))
                { Triggered = true; throw new ProbeTransientException(); }
            return new(result);
        }
    }
    private sealed class RetryCommitFault : DbTransactionInterceptor
    {
        internal bool Triggered;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!Triggered) { Triggered = true; throw new ProbeTransientException(); }
            return new(result);
        }
    }
    private sealed class PauseFirstSave : SaveChangesInterceptor
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int called;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref called, 1) == 0)
            {
                Entered.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }

    internal static ServiceProvider Services(FriendshipContext context, HttpMessageHandler accountApi = null, HttpMessageHandler characterApi = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<AccountRepository>(); services.AddSingleton<CharacterRepository>(); services.AddSingleton<InternalMessageRepository>();
        services.AddSingleton<OutboxMessagePublisher>(); services.AddSingleton<AccountManager>(); services.AddSingleton<CharacterManager>();
        services.AddTransient(sp => new FriendshipAccount(NullLogger<FriendshipAccount>.Instance, sp,
            sp.GetRequiredService<OutboxMessagePublisher>(), sp.GetRequiredService<CharacterManager>()));
        services.AddTransient(sp => new FriendshipCharacter(NullLogger<FriendshipCharacter>.Instance, sp,
            sp.GetRequiredService<OutboxMessagePublisher>(), sp.GetRequiredService<AccountManager>()));
        services.AddSingleton(new AccountAPIClient(new HttpClient(accountApi ?? new NoApi(), false) { BaseAddress = new("http://unused-account.test") }));
        services.AddSingleton(new CharacterAPIClient(new HttpClient(characterApi ?? new NoApi(), false) { BaseAddress = new("http://unused-character.test") }));
        return services.BuildServiceProvider();
    }
    private sealed class NoApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
    private sealed class FlushFault : SaveChangesInterceptor
    {
        internal bool Triggered;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<InternalMessageModel>().Any(row => row.State == EntityState.Added))
                { Triggered = true; throw new IOException("Owned presence flush failure."); }
            return new(result);
        }
    }
    internal sealed class Fixture : IDisposable
    {
        internal static readonly DateTime Earlier = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-FriendshipPresence-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<FriendshipContext> options;
        internal Fixture(DbContextOptions<FriendshipContext> options)
        {
            Directory.CreateDirectory(directory);
            this.options = options ?? new DbContextOptionsBuilder<FriendshipContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "presence.sqlite")};Pooling=False;Default Timeout=10").Options;
        }
        internal FriendshipContext Context() => new(options);
        internal DbContextOptions<FriendshipContext> Options => options;
        internal FriendshipContext Intercepted(params IInterceptor[] interceptors)
            => new(new DbContextOptionsBuilder<FriendshipContext>(options).AddInterceptors(interceptors).Options);
        internal async Task Seed(bool online)
        {
            await using var context = Context(); await context.Database.EnsureCreatedAsync();
            context.Account.Add(new() { AccountId = 7, Email = "presence@example.test", Nickname = "Preserved", Status = "preserved status",
                BlockAccountFriendRequests = true, InvitePrivilegesSuspended = true, Presence = AccountPresenceState.Invisible, LastOnline = Earlier });
            context.Character.Add(new() { CharacterId = 101, RealmId = 1, RealmName = "Synthetic", Name = "PresenceTest", AccountId = 7, LastOnline = Earlier });
            await context.SaveChangesAsync();
            if (online)
            {
                var row = await context.Account.SingleAsync(); row.Presence = AccountPresenceState.Available;
                row.ActiveCharacterId = 101; row.ActiveRealmId = 1; row.LastOnline = null;
                (await context.Character.SingleAsync()).LastOnline = null; await context.SaveChangesAsync();
            }
        }
        public void Dispose()
        {
            string full = Path.GetFullPath(directory); string temporary = Path.GetFullPath(Path.GetTempPath());
            if (!full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("NexusForever-FriendshipPresence-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture path.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }
}
