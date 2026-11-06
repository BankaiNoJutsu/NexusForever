using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.API.Character.Character;
using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Database.Friendship.Repository;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Friendship;
using NexusForever.Game.Tests.Database;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Server.Friendship.Configuration;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Server.Friendship.Game.Friend;
using NexusForever.Server.Friendship.Network.Internal;
using NexusForever.Server.Friendship.Network.Internal.Handler.Friendship;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Internal.Handler.Friendship;
using NexusForever.WorldServer.Network.Message.Handler.Friendship;
using InternalAccount = NexusForever.Network.Internal.Message.Friendship.Shared.Account;
using CharacterManager = NexusForever.Server.Friendship.Game.Character.CharacterManager;

namespace NexusForever.Game.Tests.Friendship;

public partial class FriendshipSettingOwnershipTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ActualRequestHandlersCommitInvitesOnlyForAnUnlockedCharacter(bool blocked, bool byName)
    {
        using var f = new AdmissionFixture(); await f.Seed();
        new ClientFriendshipBlockHandler().HandleMessage(f.Main.Session, Read<ClientFriendshipBlock>(w => w.Write(blocked)));
        await f.Main.Persist();
        using var context = f.Relationships.Context(); using var api = new AdmissionFixture.CharacterApi(f);
        using var services = FriendshipPresenceTransactionTests.Services(context, characterApi: api, configure: s =>
        {
            s.AddSingleton<FriendRepository>(); s.AddSingleton<FriendManager>();
            s.AddSingleton(Options.Create(new InviteOptions { InviteLifetimeMinutes = 30 }));
            s.AddTransient<FriendInvite>(); s.AddTransient<CharacterFriendInvite>(); s.AddTransient<CharacterFriendInvitePending>();
        });
        var characters = services.GetRequiredService<CharacterManager>();
        var validator = f.CreateValidator(characters);
        var outbox = services.GetRequiredService<OutboxMessagePublisher>();
        var request = new FriendRequestHandler(null, new(context, services.GetRequiredService<FriendManager>()), null);
        if (byName)
            await new FriendshipNameInviteRequestHandler(context, outbox, characters, validator, request, new()).Handle(new()
            { Inviter = new() { Id = 102, RealmId = 1 }, InviteeName = new() { Name = "Receiver", RealmName = "Synthetic" }, Type = FriendshipType.Friend });
        else
            await new FriendshipIdentityInviteRequestHandler(context, outbox, characters, validator, request, new()).Handle(new()
            { Inviter = new() { Id = 102, RealmId = 1 }, Invitee = new() { Id = 42, RealmId = 1 }, Type = FriendshipType.Friend });
        using var read = f.Relationships.Context();
        Assert.Equal(blocked ? 0 : 1, await read.FriendInvite.CountAsync());
        var messages = await read.InternalMessage.ToArrayAsync();
        if (blocked)
        {
            var result = JsonSerializer.Deserialize<FriendshipResultMessage>(Assert.Single(messages).Payload)!;
            Assert.Equal(FriendshipResult.FriendsBlocked, result.Result);
        }
        else
        {
            var invite = await read.FriendInvite.SingleAsync();
            Assert.Equal(42ul, invite.InviteeCharacterId); Assert.Equal(102ul, invite.InviterCharacterId);
            Assert.Single(await read.CharacterFriendInvite.ToArrayAsync());
            Assert.Single(await read.CharacterFriendInvitePending.ToArrayAsync());
            Assert.NotEmpty(messages);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommittedCharacterLockControlsNewRequestsThroughTheOwningApi(bool blocked)
    {
        using var f = new AdmissionFixture(); await f.Seed();
        new ClientFriendshipBlockHandler().HandleMessage(f.Main.Session, Read<ClientFriendshipBlock>(w => w.Write(blocked)));
        await f.Main.Persist();
        Assert.Equal(blocked ? FriendshipResult.FriendsBlocked : null, await f.ValidateCharacter());
        // A later request must read the committed flag rather than cached identity metadata.
        new ClientFriendshipBlockHandler().HandleMessage(f.Main.Session, Read<ClientFriendshipBlock>(w => w.Write(!blocked)));
        await f.Main.Persist();
        Assert.Equal(!blocked ? FriendshipResult.FriendsBlocked : null, await f.ValidateCharacter());
    }

    [Theory]
    [InlineData("old-api")]
    [InlineData("missing")]
    [InlineData("identity")]
    [InlineData("account")]
    public async Task MissingOrForeignApiPreferenceCannotAdmitANewFriendRequest(string failure)
    {
        using var f = new AdmissionFixture(); await f.Seed(); f.ApiFailure = failure;
        Assert.Equal(FriendshipResult.UnableToProcess, await f.ValidateCharacter());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountIgnorePersistsPublishesAndControlsAccountInvites(bool blocked)
    {
        using var f = new AdmissionFixture(); await f.Seed();
        using var context = f.Relationships.Context();
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var routed);
        routed.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        new ClientFriendshipIgnoreStrangersStateHandler(NullLogger<ClientFriendshipIgnoreStrangersStateHandler>.Instance, publisher)
            .HandleMessage(f.Main.Session, Read<ClientFriendshipIgnoreStrangersState>(w => w.Write(blocked)));
        var command = Assert.IsType<FriendshipAccountBlockUpdateMessage>(Assert.Single(routed.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))).Arguments[0]);
        await f.SetAccount(context, command);
        using (var read = f.Relationships.Context())
            Assert.Equal(blocked, (await read.Account.SingleAsync(row => row.AccountId == 7)).BlockAccountFriendRequests);
        using var services = FriendshipPresenceTransactionTests.Services(context);
        var accounts = services.GetRequiredService<AccountManager>();
        Assert.Equal(blocked ? FriendshipResult.BlockedForStrangers : null,
            await f.AccountValidator.ValidateAsync(await accounts.GetAccountAsync(8), await accounts.GetAccountAsync(7), null));
        var outbox = await context.InternalMessage.AsNoTracking().SingleAsync();
        var committed = JsonSerializer.Deserialize<FriendshipAccountPersonalStatusUpdatedMessage>(outbox.Payload)!;
        await f.Deliver(committed);
        Assert.Empty(f.Main.Packets<ServerFriendshipAccountPersonalStatus>());
        f.Main.Actor.Events.Update(0);
        Assert.Equal(blocked, Assert.Single(f.Main.Packets<ServerFriendshipAccountPersonalStatus>()).BlockStrangerRequests);
    }

    [Fact]
    public async Task AccountSaveRefusalRollsBackItsStateAndReadbackTogetherAndCanRetry()
    {
        using var f = new AdmissionFixture(); await f.Seed();
        using (var rejected = f.Relationships.Intercepted(new SocialFlushFault()))
            await Assert.ThrowsAsync<IOException>(() => f.SetAccount(rejected, false));
        using (var read = f.Relationships.Context())
        { Assert.True((await read.Account.SingleAsync(row => row.AccountId == 7)).BlockAccountFriendRequests); Assert.Empty(await read.InternalMessage.ToArrayAsync()); }
        using var retry = f.Relationships.Context(); await f.SetAccount(retry, false);
        using var final = f.Relationships.Context();
        Assert.False((await final.Account.SingleAsync(row => row.AccountId == 7)).BlockAccountFriendRequests);
        Assert.Single(await final.InternalMessage.ToArrayAsync());
    }

    [Fact]
    public async Task QueuedAccountReadbackCannotFollowAReplacementPlayer()
    {
        using var f = new AdmissionFixture(); await f.Seed();
        await f.Deliver(new() { Account = new InternalAccount { Id = 7, BlockAccountFriendRequests = true } });
        f.Main.Session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
        f.Main.Actor.Events.Update(0);
        Assert.Empty(f.Main.Packets<ServerFriendshipAccountPersonalStatus>());
    }

    private sealed class SocialFlushFault : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (data.Context!.ChangeTracker.Entries<InternalMessageModel>().Any(row => row.State == EntityState.Added))
                throw new IOException("Owned social-option flush refusal.");
            return new(result);
        }
    }

    private sealed class AdmissionFixture : IDisposable
    {
        internal readonly Fixture Main = new();
        internal readonly FriendshipPresenceTransactionTests.Fixture Relationships = new(null);
        internal readonly FriendAccountInviteValidator AccountValidator;
        internal string ApiFailure;
        private readonly ITextFilterManager text = RecordingDispatchProxy<ITextFilterManager>.Create(out _);
        private readonly IOptions<LimitOptions> limits = Options.Create(new LimitOptions { MaxFriends = 100, MaxRivals = 100,
            MaxIgnored = 100, MaxFriendRequests = 100, MaxAccountFriends = 100, MaxAccountFriendRequests = 100 });
        internal AdmissionFixture()
        {
            ((RecordingDispatchProxy<IAccount>)(object)Main.Actor.Player.Account).SetProperty(nameof(IAccount.Id), 7u);
            AccountValidator = new(limits, text);
        }
        internal async Task Seed()
        {
            using (var main = Main.Store.Context()) { main.Character.Single().AccountId = 7; await main.SaveChangesAsync(); }
            await Relationships.Seed(false);
            using var context = Relationships.Context();
            context.Account.Add(new() { AccountId = 8, Email = "inviter@example.test", Nickname = "Inviter" });
            context.Character.Add(new() { CharacterId = 42, RealmId = 1, Name = "Receiver", RealmName = "Synthetic", AccountId = 7 });
            context.Character.Add(new() { CharacterId = 102, RealmId = 1, Name = "Inviter", RealmName = "Synthetic", AccountId = 8 });
            await context.SaveChangesAsync();
        }
        internal async Task<FriendshipResult?> ValidateCharacter()
        {
            using var context = Relationships.Context(); using var api = new CharacterApi(this);
            using var services = FriendshipPresenceTransactionTests.Services(context, characterApi: api);
            var characters = services.GetRequiredService<CharacterManager>();
            var target = await characters.GetCharacterAsync(new() { Id = 42, RealmId = 1 });
            var inviter = await characters.GetCharacterAsync(new() { Id = 102, RealmId = 1 });
            var validator = CreateValidator(characters);
            return await validator.ValidateRequestAsync(inviter, target, FriendshipType.Friend, null);
        }
        internal FriendInviteValidator CreateValidator(CharacterManager characters) => new(limits, text, AccountValidator, characters);
        internal Task SetAccount(FriendshipContext context, bool blocked) => SetAccount(context,
            new FriendshipAccountBlockUpdateMessage { AccountId = 7, Identity = new() { Id = 42, RealmId = 1 }, BlockFriendRequests = blocked });
        internal async Task SetAccount(FriendshipContext context, FriendshipAccountBlockUpdateMessage command)
        {
            using var services = FriendshipPresenceTransactionTests.Services(context);
            var handler = new FriendshipAccountBlockUpdateHandler(context, services.GetRequiredService<OutboxMessagePublisher>(),
                services.GetRequiredService<AccountManager>(), new FriendshipResultPublisher());
            await handler.Handle(command);
        }
        internal Task Deliver(FriendshipAccountPersonalStatusUpdatedMessage message)
        {
            var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp);
            pp.SetMethodReturn(nameof(IPlayerManager.GetPlayerByAccountId), Main.Actor.Player);
            return new FriendshipAccountPersonalStatusUpdatedHandler(players).Handle(message);
        }
        public void Dispose() { Main.Dispose(); Relationships.Dispose(); }
        internal sealed class CharacterApi(AdmissionFixture f) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.EndsWith("/character/1/42", request.RequestUri!.AbsolutePath);
                if (f.ApiFailure == "missing") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                using var main = f.Main.Store.Context();
                var model = main.Character.Single().ToCharacter(new() { Id = 1, Name = "Synthetic" });
                if (f.ApiFailure == "old-api") model.BlocksFriendRequests = null;
                if (f.ApiFailure == "account") model.AccountId = 8;
                if (f.ApiFailure == "identity") model.Identity = new() { Id = 43, RealmId = 1 };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(model) });
            }
        }
    }
}
