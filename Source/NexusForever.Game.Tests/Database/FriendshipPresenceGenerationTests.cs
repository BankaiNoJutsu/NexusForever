using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Game.Static.Chat;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Server.Friendship.Network.Internal;
using LoginHandler = NexusForever.Server.Friendship.Network.Internal.Handler.Player.PlayerLoggedInHandler;
using LogoutHandler = NexusForever.Server.Friendship.Network.Internal.Handler.Player.PlayerLoggedOutHandler;
using Fixture = NexusForever.Game.Tests.Database.FriendshipPresenceTransactionTests.Fixture;

namespace NexusForever.Game.Tests.Database;

public sealed class FriendshipPresenceGenerationTests
{
    private readonly DbContextOptions<FriendshipContext> options;
    public FriendshipPresenceGenerationTests() { }
    internal FriendshipPresenceGenerationTests(DbContextOptions<FriendshipContext> options) => this.options = options;
    private static readonly DateTime Time = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LogoutBeforeLoginRetainsItsTombstoneEvenWithoutCharacterMetadata(bool characterExists)
    {
        using var f = new Fixture(options); await f.Seed(false);
        if (!characterExists) { await using var remove = f.Context(); await remove.Character.ExecuteDeleteAsync(); }
        await Send(f, Out(2, 1)); var before = await State(f);
        Assert.True(before.PresenceClosed); Assert.Equal(Time, before.LastOnline); Assert.Null(before.ActiveCharacterId);
        int messages = await MessageCount(f);
        await Send(f, In(2, 1)); await Send(f, Out(2, 1));
        var after = await State(f); Assert.Equal(before.LastOnline, after.LastOnline); Assert.True(after.PresenceClosed);
        Assert.Equal(messages, await MessageCount(f)); Assert.Equal(2ul, after.PresenceSessionEpoch);
        await using var read = f.Context(); Assert.Equal(characterExists ? 1 : 0, await read.Character.CountAsync());
    }

    [Theory]
    [InlineData("LLOOL")] [InlineData("OLL")] [InlineData("LOO")] [InlineData("OOL")]
    public async Task DuplicateAndReorderedPhasesCloseOnceWithoutReplayingNotifications(string order)
    {
        using var f = new Fixture(options); await f.Seed(false);
        foreach (char phase in order) await Send(f, phase == 'L' ? In(1, 1) : Out(1, 1));
        var state = await State(f); Assert.True(state.PresenceClosed); Assert.Null(state.ActiveCharacterId);
        Assert.Equal(order[0] == 'L' ? 11 : 3, await MessageCount(f));
        Assert.Equal(Time, state.LastOnline);
    }

    [Theory]
    [InlineData("login")] [InlineData("logout")] [InlineData("legacy-login")] [InlineData("legacy-logout")]
    public async Task OlderOrLegacyEventsCannotOverwriteANewerSameCharacterSession(string kind)
    {
        using var f = new Fixture(options); await f.Seed(false); await Send(f, In(3, 1)); int count = await MessageCount(f);
        object older = kind switch
        {
            "login" => In(2, ulong.MaxValue), "logout" => Out(2, ulong.MaxValue),
            "legacy-login" => In(0, 0), _ => Out(0, 0)
        };
        await Send(f, older);
        var state = await State(f); Assert.Equal(3ul, state.PresenceSessionEpoch); Assert.False(state.PresenceClosed);
        Assert.Equal(101ul, state.ActiveCharacterId); Assert.Null(state.LastOnline); Assert.Equal(count, await MessageCount(f));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NewerAdmissionOrEpochReopensAClosedSession(bool sameEpoch)
    {
        using var f = new Fixture(options); await f.Seed(false); await Send(f, Out(2, 5));
        await Send(f, In(sameEpoch ? 2ul : 3ul, sameEpoch ? 6ul : 1ul));
        var state = await State(f); Assert.False(state.PresenceClosed); Assert.Null(state.LastOnline);
        Assert.Equal(101ul, state.ActiveCharacterId); Assert.Equal(sameEpoch ? 6ul : 1ul, state.PresenceAdmission);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SameGenerationCannotChangeItsSourceCharacterOrRealm(bool anotherRealm)
    {
        using var f = new Fixture(options); await f.Seed(false); await Send(f, In(2, 1)); int count = await MessageCount(f);
        var collision = Out(2, 1); collision.Identity = new() { Id = anotherRealm ? 101ul : 102ul, RealmId = anotherRealm ? (ushort)2 : (ushort)1 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(f, collision));
        Assert.Equal(count, await MessageCount(f)); Assert.False((await State(f)).PresenceClosed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SupersedingACharacterOrRealmClosesItsOldPresenceAndIgnoresLateOldLogout(bool anotherRealm)
    {
        using var f = new Fixture(options); await f.Seed(false);
        ulong id = anotherRealm ? 101ul : 102ul; ushort realm = anotherRealm ? (ushort)2 : (ushort)1;
        await using (var seed = f.Context())
        {
            seed.Character.Add(new() { CharacterId = id, RealmId = realm, RealmName = anotherRealm ? "OtherRealm" : "Synthetic",
                Name = "NewPresence", AccountId = 7, LastOnline = Time.AddDays(-1) }); await seed.SaveChangesAsync();
        }
        await Send(f, In(2, 1)); var next = In(2, 2); next.Identity = new() { Id = id, RealmId = realm }; next.OccurredUtc = Time.AddMinutes(1);
        await Send(f, next); int count = await MessageCount(f); await Send(f, Out(2, 1));
        await using var read = f.Context();
        Assert.Equal(Time.AddMinutes(1), (await read.Character.SingleAsync(c => c.CharacterId == 101 && c.RealmId == 1)).LastOnline);
        Assert.Null((await read.Character.SingleAsync(c => c.CharacterId == id && c.RealmId == realm)).LastOnline);
        Assert.Equal(id, (await read.Account.SingleAsync()).ActiveCharacterId); Assert.Equal(count, await MessageCount(f));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FirstLogoutBootstrapsAClosedAccountBeforeAnyLoginAndKeepsItClosed(bool characterAvailable)
    {
        using var f = new Fixture(options); await using (var schema = f.Context()) await schema.Database.EnsureCreatedAsync();
        using var accountApi = new Api(_ => new(HttpStatusCode.OK) { Content = JsonContent.Create(new NexusForever.API.Model.Account.Account { Id = 7, Email = "presence@example.test" }) });
        using var characterApi = new Api(_ => characterAvailable ? new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new NexusForever.API.Model.Character.Character
            {
                Identity = new() { Id = 101, RealmId = 1 }, IdentityName = new() { Name = "PresenceTest", RealmName = "Synthetic" },
                AccountId = 7, Stats = [], LastOnline = Time.AddDays(-1)
            })
        } : new(HttpStatusCode.NotFound));
        await using (var context = f.Context())
        {
            using var services = FriendshipPresenceTransactionTests.Services(context, accountApi, characterApi);
            await new LogoutHandler(context, services.GetRequiredService<CharacterManager>()).Handle(Out(1, 1));
        }
        int requests = characterApi.Requests; await Send(f, In(1, 1));
        var state = await State(f); Assert.True(state.PresenceClosed); Assert.Null(state.ActiveCharacterId);
        Assert.Equal(1ul, state.PresenceSessionEpoch); Assert.Equal(requests, characterApi.Requests);
        await using var read = f.Context(); Assert.Equal(characterAvailable ? 1 : 0, await read.Character.CountAsync());
    }

    [Theory]
    [InlineData("epoch")] [InlineData("admission")] [InlineData("account")]
    [InlineData("time")] [InlineData("utc")] [InlineData("identity")] [InlineData("realm")]
    public async Task MalformedVersionedEventsCannotMutateOrPublish(string invalid)
    {
        using var f = new Fixture(options); await f.Seed(false); var message = Out(1, 1);
        if (invalid == "epoch") message.SessionEpoch = 0;
        if (invalid == "admission") message.Admission = 0;
        if (invalid == "account") message.AccountId = 0;
        if (invalid == "time") message.OccurredUtc = null;
        if (invalid == "utc") message.OccurredUtc = DateTime.SpecifyKind(Time, DateTimeKind.Unspecified);
        if (invalid == "identity") message.Identity = null;
        if (invalid == "realm") message.Identity = new() { Id = 101, RealmId = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(f, message));
        Assert.Equal(0ul, (await State(f)).PresenceSessionEpoch); Assert.Equal(0, await MessageCount(f));
    }

    [Fact]
    public async Task ClockRollbackDoesNotReplaceTheAuthoritativeGenerationOrder()
    {
        using var f = new Fixture(options); await f.Seed(false); var first = In(1, 1); first.OccurredUtc = Time.AddDays(1);
        await Send(f, first); var newer = Out(2, 1); newer.OccurredUtc = Time.AddDays(-1); await Send(f, newer);
        await Send(f, first); var row = await State(f); Assert.True(row.PresenceClosed);
        Assert.Equal(2ul, row.PresenceSessionEpoch); Assert.Equal(Time.AddDays(-1), row.LastOnline);
    }

    [Theory]
    [InlineData("account")] [InlineData("identity")] [InlineData("realm")]
    public async Task ForeignApiMetadataCannotCreateATombstoneForTheRequestedIdentity(string wrong)
    {
        using var f = new Fixture(options); await f.Seed(false);
        await using (var remove = f.Context()) await remove.Character.ExecuteDeleteAsync();
        using var api = new Api(_ => new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new NexusForever.API.Model.Character.Character
            {
                Identity = new() { Id = wrong == "identity" ? 102ul : 101ul, RealmId = wrong == "realm" ? (ushort)2 : (ushort)1 },
                IdentityName = new() { Name = "Wrong", RealmName = "Synthetic" }, AccountId = wrong == "account" ? 8u : 7u, Stats = []
            })
        });
        await using (var context = f.Context())
        {
            using var services = FriendshipPresenceTransactionTests.Services(context, characterApi: api);
            await Assert.ThrowsAsync<InvalidOperationException>(() => new LogoutHandler(context, services.GetRequiredService<CharacterManager>()).Handle(Out(1, 1)));
        }
        Assert.Equal(0ul, (await State(f)).PresenceSessionEpoch); Assert.Equal(0, await MessageCount(f));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AbsentApiAccountCannotBootstrapAPartialVersionedPresence(bool login)
    {
        using var f = new Fixture(options); await using (var schema = f.Context()) await schema.Database.EnsureCreatedAsync();
        await Send(f, login ? In(1, 1) : Out(1, 1));
        await using var read = f.Context(); Assert.Empty(await read.Account.ToArrayAsync()); Assert.Empty(await read.Character.ToArrayAsync());
        Assert.Empty(await read.InternalMessage.ToArrayAsync());
    }

    [Fact]
    public async Task ForeignCharacterCannotCreateAnAccountTombstone()
    {
        using var f = new Fixture(options); await f.Seed(false);
        await using (var changed = f.Context())
        {
            changed.Account.Add(new() { AccountId = 8, Email = "other@example.test", Nickname = "Other" });
            (await changed.Character.SingleAsync()).AccountId = 8; await changed.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(f, Out(1, 1)));
        await using var read = f.Context(); Assert.Equal(0ul, (await read.Account.SingleAsync(a => a.AccountId == 7)).PresenceSessionEpoch);
        Assert.Equal(0, await MessageCount(f));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task RetriedTransactionRebuildsTheGenerationAndOutboxTogether(bool login, bool afterCommit)
    {
        using var f = new Fixture(options); await f.Seed(false); var fault = new CommitFault(afterCommit);
        var configured = new DbContextOptionsBuilder<FriendshipContext>(f.Options).ReplaceService<IExecutionStrategyFactory, RetryFactory>().AddInterceptors(fault).Options;
        await using (var context = new FriendshipContext(configured))
        {
            using var services = FriendshipPresenceTransactionTests.Services(context);
            if (login) await new LoginHandler(context, services.GetRequiredService<OutboxMessagePublisher>(), services.GetRequiredService<CharacterManager>(),
                services.GetRequiredService<AccountManager>()).Handle(In(1, 1));
            else await new LogoutHandler(context, services.GetRequiredService<CharacterManager>()).Handle(Out(1, 1));
        }
        Assert.True(fault.Triggered); var state = await State(f); Assert.Equal(1ul, state.PresenceSessionEpoch);
        Assert.Equal(!login, state.PresenceClosed); Assert.Equal(login ? 8 : 3, await MessageCount(f));
    }

    private static PlayerLoggedInMessage In(ulong epoch, ulong admission) => new()
        { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 }, SessionEpoch = epoch, Admission = admission, OccurredUtc = Time };
    private static PlayerLoggedOutMessage Out(ulong epoch, ulong admission) => new()
        { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 }, SessionEpoch = epoch, Admission = admission, OccurredUtc = Time };
    private static async Task Send(Fixture f, object message)
    {
        await using var context = f.Context(); using var services = FriendshipPresenceTransactionTests.Services(context);
        if (message is PlayerLoggedInMessage login) await new LoginHandler(context, services.GetRequiredService<OutboxMessagePublisher>(),
            services.GetRequiredService<CharacterManager>(), services.GetRequiredService<AccountManager>()).Handle(login);
        else await new LogoutHandler(context, services.GetRequiredService<CharacterManager>()).Handle((PlayerLoggedOutMessage)message);
    }
    private static async Task<AccountModel> State(Fixture f) { await using var read = f.Context(); return await read.Account.AsNoTracking().SingleAsync(); }
    private static async Task<int> MessageCount(Fixture f) { await using var read = f.Context(); return await read.InternalMessage.CountAsync(); }
    private sealed class Api(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        internal int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            { Requests++; return Task.FromResult(response(request)); }
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
            InterceptionResult result, CancellationToken token = default)
        {
            if (!after) Fail();
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken token = default)
            { if (after) Fail(); return Task.CompletedTask; }
    }
}
