using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Friendship;
using NexusForever.Network.Internal.Message.Player;
using System.Text.Json;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Tests.Database;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Internal.Message.Friendship.Shared;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Friend;
using NexusForever.Server.Friendship.Network.Internal;
using NexusForever.Server.Friendship.Network.Internal.Handler.Friendship;
using Fixture = NexusForever.Game.Tests.Database.FriendshipPresenceTransactionTests.Fixture;

namespace NexusForever.Game.Tests.Friendship;

public partial class FriendshipAccountCommandOwnershipTests
{
    public static IEnumerable<object[]> StaleOwners() =>
        from kind in new[] { "presence", "block", "status", "nickname" }
        from changed in new[] { "epoch", "admission", "character", "realm", "closed" }
        select new object[] { kind, changed };

    [Theory]
    [MemberData(nameof(StaleOwners))]
    public async Task DelayedOrForeignCommandsCannotOverwriteTheLatestPresence(string kind, string changed)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        var owner = Owner();
        if (changed == "epoch") owner.SessionEpoch--;
        if (changed == "admission") owner.Admission--;
        if (changed == "character") owner.Identity = new() { Id = 102, RealmId = 1 };
        if (changed == "realm") owner.Identity = new() { Id = 101, RealmId = 2 };
        if (changed == "closed")
        {
            using var context = f.Context(); var row = await context.Account.SingleAsync();
            row.PresenceClosed = true; row.ActiveCharacterId = null; row.ActiveRealmId = null;
            await context.SaveChangesAsync();
        }
        await Send(f, kind, owner); await AssertOriginal(f);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task CommandBeforeItsLoginRetriesAfterTheActualPresenceAdmission(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        var owner = Owner(); owner.SessionEpoch = 4; owner.Admission = 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(f, kind, owner)); await AssertOriginal(f);
        using (var context = f.Context())
        {
            using var services = FriendshipPresenceTransactionTests.Services(context);
            await new NexusForever.Server.Friendship.Network.Internal.Handler.Player.PlayerLoggedInHandler(context,
                services.GetRequiredService<OutboxMessagePublisher>(), services.GetRequiredService<CharacterManager>(),
                services.GetRequiredService<AccountManager>()).Handle(new PlayerLoggedInMessage
                { AccountId = 7, Identity = owner.Identity, SessionEpoch = 4, Admission = 1, OccurredUtc = DateTime.UtcNow });
            await context.InternalMessage.ExecuteDeleteAsync();
        }
        await Send(f, kind, owner); await AssertChanged(f, kind);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task RefusedCommitRollsBackSettingAndOutboxAndFreshRetryCanSucceed(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f); var fault = new RefuseCommit();
        using (var context = f.Intercepted(fault))
            await Assert.ThrowsAsync<IOException>(() => Send(context, Command(kind, Owner())));
        Assert.True(fault.ObservedOutbox); await AssertOriginal(f);
        await Send(f, kind, Owner()); await AssertChanged(f, kind);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task StoredOwnerIsRefreshedUnderTheCommandTransaction(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        using var context = f.Context(); await context.Account.SingleAsync();
        using (var newer = f.Context())
        { (await newer.Account.SingleAsync()).PresenceSessionEpoch = 4; await newer.SaveChangesAsync(); }
        await Send(context, Command(kind, Owner())); await AssertOriginal(f);
        using var read = f.Context(); Assert.Equal(4ul, (await read.Account.SingleAsync()).PresenceSessionEpoch);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task WholeUnitRetryRebuildsTheScopedGraphAndCommitsOneOutbox(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f); var fault = new RetryCommit();
        var options = new DbContextOptionsBuilder<FriendshipContext>(f.Options).ReplaceService<IExecutionStrategyFactory, RetryFactory>()
            .AddInterceptors(fault).Options;
        using (var context = new FriendshipContext(options)) await Send(context, Command(kind, Owner()));
        Assert.Equal(2, fault.Models.Count); Assert.NotSame(fault.Models[0], fault.Models[1]);
        await AssertChanged(f, kind); using var read = f.Context(); Assert.Single(await read.InternalMessage.ToArrayAsync());
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task UnversionedLegacyAccountRetainsItsExistingCommandContract(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await Send(f, kind, null); await AssertChanged(f, kind);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task PartialGenerationCannotBecomeALegacyCommand(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        var owner = Owner(); owner.Admission = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Send(f, kind, owner)); await AssertOriginal(f);
    }

    [Theory]
    [InlineData("status")] [InlineData("nickname")]
    public async Task InvalidTextResultUsesTheAdmittedOwnerRatherThanAnUnrelatedLegacyTarget(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        object message = Command(kind, Owner());
        if (message is FriendshipAccountStatusUpdateMessage status) status.Identity = new() { Id = 999, RealmId = 2 };
        else ((FriendshipAccountNicknameUpdateMessage)message).Source = new() { Id = 999, RealmId = 2 };
        using (var context = f.Context()) await Send(context, message, validText: false);
        using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal("Preserved", row.Nickname); Assert.Equal("preserved status", row.Status);
        var result = JsonSerializer.Deserialize<FriendshipResultMessage>((await read.InternalMessage.SingleAsync()).Payload)!;
        Assert.Equal(Owner().Identity, result.Target);
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task LegacyCommandsCannotOverwriteTheRecordedModernOwner(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        await Send(f, kind, null);
        using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(AccountPresenceState.Available, row.Presence); Assert.True(row.BlockAccountFriendRequests);
        Assert.Equal("Preserved", row.Nickname); Assert.Equal("preserved status", row.Status);
        Assert.Empty(await read.InternalMessage.ToArrayAsync());
    }

    [Theory]
    [InlineData("presence")] [InlineData("block")] [InlineData("status")] [InlineData("nickname")]
    public async Task CurrentCommandsKeepTheirExistingMutationAndDurableOutbox(string kind)
    {
        using var f = new Fixture(null); await f.Seed(true); await RecordOwner(f);
        await Send(f, kind, Owner());
        await AssertChanged(f, kind);
    }

    private static async Task AssertChanged(Fixture f, string kind)
    {
        using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(kind == "presence" ? AccountPresenceState.Away : AccountPresenceState.Available, row.Presence);
        Assert.Equal(kind != "block", row.BlockAccountFriendRequests);
        Assert.Equal(kind == "nickname" ? "New name" : "Preserved", row.Nickname);
        Assert.Equal(kind == "status" ? "New status" : "preserved status", row.Status);
        Assert.NotEmpty(await read.InternalMessage.ToArrayAsync());
    }

    private static FriendshipAccountCommandOwner Owner() => new() { Identity = new() { Id = 101, RealmId = 1 }, SessionEpoch = 3, Admission = 2 };
    private static async Task RecordOwner(Fixture f)
    {
        using var context = f.Context(); var row = await context.Account.SingleAsync();
        row.PresenceSessionEpoch = 3; row.PresenceAdmission = 2; row.PresenceCharacterId = 101; row.PresenceRealmId = 1;
        row.PresenceClosed = false;
        await context.SaveChangesAsync();
    }
    private static async Task AssertOriginal(Fixture f)
    {
        using var read = f.Context(); var row = await read.Account.SingleAsync();
        Assert.Equal(AccountPresenceState.Available, row.Presence); Assert.True(row.BlockAccountFriendRequests);
        Assert.Equal("Preserved", row.Nickname); Assert.Equal("preserved status", row.Status);
        Assert.Empty(await read.InternalMessage.ToArrayAsync());
    }
    private static object Command(string kind, FriendshipAccountCommandOwner owner) => kind switch
    {
        "presence" => new FriendshipAccountPresenceUpdateMessage { AccountId = 7, Presence = AccountPresenceState.Away, PresenceOwner = owner },
        "block" => new FriendshipAccountBlockUpdateMessage { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 }, BlockFriendRequests = false, PresenceOwner = owner },
        "status" => new FriendshipAccountStatusUpdateMessage { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 }, Status = "New status", PresenceOwner = owner },
        _ => new FriendshipAccountNicknameUpdateMessage { AccountId = 7, Source = new() { Id = 101, RealmId = 1 }, AccountNickname = "New name", PresenceOwner = owner }
    };
    private static async Task Send(Fixture f, string kind, FriendshipAccountCommandOwner owner)
    {
        using var context = f.Context();
        await Send(context, Command(kind, owner));
    }
    private static async Task Send(FriendshipContext context, object command, bool validText = true)
    {
        using var services = FriendshipPresenceTransactionTests.Services(context);
        var accounts = services.GetRequiredService<AccountManager>();
        var publisher = services.GetRequiredService<OutboxMessagePublisher>(); var results = new FriendshipResultPublisher();
        var text = RecordingDispatchProxy<ITextFilterManager>.Create(out var tp); tp.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), validText);
        if (command is FriendshipAccountPresenceUpdateMessage presence) await new FriendshipAccountPresenceUpdateHandler(context, accounts).Handle(presence);
        else if (command is FriendshipAccountBlockUpdateMessage block) await new FriendshipAccountBlockUpdateHandler(context, publisher, accounts, results).Handle(block);
        else if (command is FriendshipAccountStatusUpdateMessage status) await new FriendshipAccountStatusUpdateHandler(context, publisher, accounts, text, results).Handle(status);
        else await new FriendshipAccountNicknameUpdateHandler(context, publisher, accounts, text, results).Handle((FriendshipAccountNicknameUpdateMessage)command);
    }

    private sealed class RefuseCommit : DbTransactionInterceptor
    {
        internal bool ObservedOutbox;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken token = default)
        {
            ObservedOutbox = ((FriendshipContext)data.Context!).InternalMessage.AsNoTracking().Any();
            throw new IOException("Owned Friendship command commit refusal after flush.");
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
    private sealed class RetryCommit : DbTransactionInterceptor
    {
        internal readonly List<object> Models = [];
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken token = default)
        {
            var context = (FriendshipContext)data.Context!;
            Assert.Single(context.InternalMessage.AsNoTracking()); Models.Add(context.Account.Local.Single());
            if (Models.Count == 1) throw new Transient();
            return new(result);
        }
    }
}
