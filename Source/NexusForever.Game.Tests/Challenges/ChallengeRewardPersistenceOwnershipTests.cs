using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared.Game;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeRewardPersistenceOwnershipTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ChallengeRewardPersistenceOwnershipTests() { }
    internal ChallengeRewardPersistenceOwnershipTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task PreparedOwnershipBlocksBothDirectAndOtherLoadedChallengeSavesUntilReleased()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f);
        var owner = Manager(f); var other = Manager(f); other.Update(1);
        using var identity = f.Context();
        Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(f.Challenge(16))], out var held));
        using (held)
        {
            Assert.True(owner.HasPendingRewardOperation); Assert.False(f.Inventory.TryAcquireSaveLease(out _));
            using var direct = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(direct)); Assert.True(direct.IsSaveRejected);
            using var foreign = f.Context(); Assert.Throws<InvalidOperationException>(() => other.Save(foreign)); Assert.True(foreign.IsSaveRejected);
        }
        Assert.False(owner.HasPendingRewardOperation); Assert.True(f.Inventory.TryAcquireSaveLease(out var save)); save.Dispose();
        using var after = f.Context(); other.Save(after); await after.SaveChangesAsync();
        Assert.Equal(299d, after.CharacterChallenge.Single().ActiveTimerSeconds);
    }

    [Fact]
    public async Task QueueOwnershipBlocksPlayerBeforeAuthEvenBetweenSqlUnits()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var player = BarePlayer(f, owner);
        using (owner.HoldRewardQueue())
        {
            Assert.True(owner.HasPendingRewardOperation);
            Assert.True(f.Inventory.TryAcquireSaveLease(out var inventoryOnly)); inventoryOnly.Dispose();
            await Assert.ThrowsAsync<InvalidOperationException>(() => player.SaveDirect());
            using var auth = new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite("Data Source=:memory:").Options);
            Assert.Throws<InvalidOperationException>(() => player.Save(auth)); Assert.Empty(auth.ChangeTracker.Entries());
            using var character = f.Context(); Assert.Throws<InvalidOperationException>(() => player.Save(character)); Assert.True(character.IsSaveRejected);
        }
        Assert.False(owner.HasPendingRewardOperation);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => player.SaveDirect());
        Assert.Contains("AuthDatabase", error.Message); // first permitted Auth lookup on this deliberately bare Player
        Assert.True(f.Inventory.TryAcquireMutationLease(out var released)); released.Dispose();
    }

    [Fact]
    public async Task CommittedMedalAdoptsItsBaselineAndPreservesLaterTimerAndProgressIntent()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f);
        ushort id = f.Challenge(16); var intent = owner.CaptureRewardIntent(id);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [intent], out var held));
        using (held)
        {
            var before = held.Expected(id); var after = before with { Tier = 1, LastRewardTier = 0 };
            var command = CharacterChallengeRewardCommand.Credit(before, after, 0, new(1.25f, 9), DateTime.UtcNow);
            var result = await held.ExecuteAsync(Executor(f), command);
            owner.Update(2); Assert.True(owner.TryAdvanceProgress(id, 3)); Runtime(owner, id).ShareTimer = 17;
            Assert.True(held.AdoptCommitted(result)); Assert.False(owner.RequiresAuthoritativeReload);
            Assert.Equal(1u, Runtime(owner, id).CurrentTier); Assert.Equal(3u, Runtime(owner, id).CurrentCount);
            Assert.Equal(298d, Runtime(owner, id).ActiveTimer);
            Assert.Equal(17d, Runtime(owner, id).ShareTimer);
        }
        using var save = f.Context(); owner.Save(save); await save.SaveChangesAsync();
        Assert.Equal(3u, save.CharacterChallenge.Single().CurrentCount); Assert.Equal(298d, save.CharacterChallenge.Single().ActiveTimerSeconds);
        Assert.Equal(125u, save.CharacterChallengeRewardTrack.Single().Points); Assert.Single(save.CharacterChallengeMedalReceipt);
    }

    [Fact]
    public async Task NewAttemptMarkerIsAdoptedBeforeTheNextOrdinarySave()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        string attempt = Guid.NewGuid().ToString("N");
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Activate(null, f.Activated(16), attempt, true));
        Assert.True(held.AdoptCommitted(result)); held.Dispose();
        Assert.Equal(attempt, Runtime(owner, id).ActiveAttemptId);
        owner.Update(5); Assert.True(owner.TryAdvanceProgress(id, 2));
        using var save = f.Context(); owner.Save(save); await save.SaveChangesAsync();
        Assert.Equal(attempt, save.CharacterChallenge.Single().ActiveAttemptId); Assert.Equal(2u, save.CharacterChallenge.Single().CurrentCount);
        Assert.Equal(295d, save.CharacterChallenge.Single().ActiveTimerSeconds); Assert.Single(save.CharacterChallengeAttempt);
    }

    [Fact]
    public async Task LaterAttemptIdentityQuarantinesTheOldResultWithoutOverwritingEitherOwner()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var before = held.Expected(id);
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1 }, 0, new(1, 1), DateTime.UtcNow));
        string later = Guid.NewGuid().ToString("N"); Runtime(owner, id).ActiveAttemptId = later;
        Assert.False(held.AdoptCommitted(result)); Assert.True(owner.RequiresAuthoritativeReload); Assert.False(owner.HasPendingRewardOperation);
        Assert.Equal(later, Runtime(owner, id).ActiveAttemptId);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(denied)); Assert.True(denied.IsSaveRejected);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BarePlayer(f, owner).SaveDirect());
        using var read = f.Context(); Assert.Equal(before.AttemptId, read.CharacterChallenge.Single().ActiveAttemptId); Assert.Equal(1u, read.CharacterChallenge.Single().CurrentTier);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UncertainOrdinarySavesRequireACommittedLoadBeforeBindingTheRewardBaseline(bool committed)
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16); owner.Update(1);
        await using (var old = committed ? f.Intercepted(new LostAck()) : f.Context())
        {
            await using var transaction = await old.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            owner.Save(old); await old.SaveChangesAsync();
            if (committed) await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
            else await transaction.RollbackAsync();
        }
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        Assert.Throws<InvalidOperationException>(() => held.Expected(id));
        var loaded = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Load(42)); Assert.True(held.CompleteLoad(loaded));
        var before = held.Expected(id); Assert.Equal(committed ? 299d : 300d, before.ActiveSeconds);
        var after = CharacterChallengeState.FromModel(Model(Runtime(owner, id))) with { Tier = 1, LastRewardTier = 0 };
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Credit(before, after, 0, new(1, 1), DateTime.UtcNow));
        Assert.True(held.AdoptCommitted(result)); Assert.False(owner.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(299d, read.CharacterChallenge.Single().ActiveTimerSeconds); Assert.Equal(100u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task AForeignBaselineDiscoveredByLoadCannotBeAdoptedAsLocalIntent()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using (var foreign = f.Context()) { foreign.CharacterChallenge.Single().CurrentCount = 7; foreign.SaveChanges(); }
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Load(42));
        Assert.False(held.CompleteLoad(result)); Assert.True(owner.RequiresAuthoritativeReload); Assert.Equal(0u, Runtime(owner, id).CurrentCount);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(denied));
    }

    [Fact]
    public async Task DisposingAnExecutingLeaseCannotReleaseSaveOwnershipOrAcceptAClonedResult()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true));
        held.Dispose(); Assert.True(owner.HasPendingRewardOperation); Assert.False(f.Inventory.TryAcquireSaveLease(out _));
        Assert.False(held.AdoptCommitted(result with { })); Assert.True(owner.HasPendingRewardOperation);
        Assert.True(held.AdoptCommitted(result)); Assert.False(owner.HasPendingRewardOperation);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidationCanFinishKnownAdoptionButFinalizedOwnersNeverResumeSaving(bool final)
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true));
        owner.InvalidateRewardOperations(final);
        Assert.False(owner.TryAcquireRewardPersistence(identity, [], out _));
        Assert.True(held.AdoptCommitted(result)); Assert.Equal(final, owner.RequiresAuthoritativeReload);
        using var save = f.Context();
        if (final) { Assert.Throws<InvalidOperationException>(() => owner.Save(save)); Assert.True(save.IsSaveRejected); }
        else { owner.Update(1); owner.Save(save); await save.SaveChangesAsync(); }
    }

    [Fact]
    public async Task ClaimLeasesUseTheirOwnInventoryReservationWithoutNestingAMutationLease()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var delivery = f.Reserve(input.Row, 0); var owner = Manager(f);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [], out var held, delivery));
        var command = CharacterChallengeRewardCommand.Claim(42, 16, input.Cycle, input.Revision, input.Row, 0, Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        var result = await held.ExecuteAsync(Executor(f), command);
        Assert.True(held.AdoptCommitted(result)); Assert.False(owner.HasPendingRewardOperation);
        Assert.False(f.Inventory.TryAcquireSaveLease(out _)); Assert.True(delivery.PublishCommitted());
        Assert.True(f.Inventory.TryAcquireSaveLease(out var save)); save.Dispose();
    }

    [Fact]
    public async Task ExistingSaveOwnershipAndForeignIntentsCannotLeakAnInventoryLease()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); var foreign = Manager(f);
        using var identity = f.Context();
        Assert.Throws<InvalidOperationException>(() => owner.TryAcquireRewardPersistence(identity, [foreign.CaptureRewardIntent(f.Challenge(16))], out _));
        Assert.True(f.Inventory.TryAcquireSaveLease(out var released)); released.Dispose();
        owner.Update(1);
        await using var save = f.Context(); await using var transaction = await save.Database.BeginTransactionAsync(IsolationLevel.Serializable); owner.Save(save);
        Assert.False(owner.TryAcquireRewardPersistence(identity, [], out _));
        Assert.True(f.Inventory.TryAcquireMutationLease(out var mutation)); mutation.Dispose();
        await transaction.RollbackAsync();
    }

    [Fact]
    public void MultipleQueuedRequestsRetainDeferredCallbacksUntilTheFinalQueueOwnerReleases()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var player = BarePlayer(f, owner);
        var first = owner.HoldRewardQueue(); var second = owner.HoldRewardQueue();
        int success = 0, failure = 0;
        MethodInfo save = typeof(Player).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(Action), typeof(Action)])!;
        save.Invoke(player, [new Action(() => ++success), new Action(() => ++failure)]);
        first.Dispose(); first.Dispose(); Assert.True(owner.HasPendingRewardOperation);
        save.Invoke(player, [new Action(() => ++success), new Action(() => ++failure)]);
        Assert.Equal(0, success); Assert.Equal(0, failure);
        second.Dispose(); Assert.False(owner.HasPendingRewardOperation);
        var error = Assert.Throws<TargetInvocationException>(() => save.Invoke(player, [new Action(() => ++success), new Action(() => ++failure)]));
        Assert.IsType<InvalidOperationException>(error.InnerException); Assert.Equal(3, failure); Assert.Equal(0, success);
        Assert.False(f.Inventory.IsSaveBlocked);
    }

    [Fact]
    public async Task AnUnavailableCommittedOutcomeKeepsOwnershipUntilItsBoundResumeIsAdopted()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var before = held.Expected(id); var command = CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1 }, 0, new(1, 1), DateTime.UtcNow);
        var fault = new LostAck(); int calls = 0;
        var broken = new CharacterChallengeRewardExecutor(() => Interlocked.Increment(ref calls) > 2
            ? throw new IOException("Owned probe unavailable.") : f.Intercepted(fault), f.Catalog, f.Tables);
        var failed = await held.ExecuteAsync(broken, command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Uncertain, failed.Outcome); Assert.True(failed.OriginalUnitEnded);
        held.Dispose(); Assert.True(owner.HasPendingRewardOperation); Assert.False(f.Inventory.TryAcquireSaveLease(out _));
        await Assert.ThrowsAsync<InvalidOperationException>(() => BarePlayer(f, owner).SaveDirect());
        var resolved = await held.ResumeAsync(Executor(f), failed); Assert.True(held.AdoptCommitted(resolved));
        Assert.False(owner.HasPendingRewardOperation); Assert.False(owner.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Single(read.CharacterChallengeMedalReceipt); Assert.Equal(100u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task AbandonmentAfterATerminalResultReleasesTheProcessLeaseButBlocksOldPlayerSaves()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var before = held.Expected(id);
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1 }, 0, new(1, 1), DateTime.UtcNow));
        Assert.True(held.AbandonAfterTerminal(result)); Assert.True(owner.RequiresAuthoritativeReload); Assert.False(owner.HasPendingRewardOperation);
        Assert.True(f.Inventory.TryAcquireSaveLease(out var inventory)); inventory.Dispose();
        await Assert.ThrowsAsync<InvalidOperationException>(() => BarePlayer(f, owner).SaveDirect());
        var fresh = Manager(f); Assert.True(fresh.TryAcquireRewardPersistence(identity, [], out var next)); next.Dispose();
    }

    [Fact]
    public async Task CleanupWaitsForQueuedAndExecutingWorkUntilAdoptionOrFinalInvalidation()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var player = BarePlayer(f, owner);
        bool CanCleanup() => (bool)typeof(Player).GetMethod("CanCleanup", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null)!;
        Assert.True(CanCleanup());
        using (owner.HoldRewardQueue()) Assert.False(CanCleanup());
        Assert.True(CanCleanup());
        using var identity = f.Context(); ushort id = f.Challenge(16);
        Assert.True(owner.TryAcquireRewardPersistence(identity, [owner.CaptureRewardIntent(id)], out var held));
        var result = await held.ExecuteAsync(Executor(f), CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true));
        owner.InvalidateRewardOperations(); Assert.False(CanCleanup());
        owner.InvalidateRewardOperations(true); Assert.True(CanCleanup());
        Assert.True(held.AdoptCommitted(result)); Assert.True(CanCleanup()); Assert.True(owner.RequiresAuthoritativeReload);
    }

    [Fact]
    public void PreparedInvalidationReleasesItsLeaseAndForbidsStaleCleanupPersistence()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f);
        using var identity = f.Context(); Assert.True(owner.TryAcquireRewardPersistence(identity, [], out var held));
        owner.InvalidateRewardOperations(); Assert.True(owner.RequiresAuthoritativeReload); Assert.False(owner.HasPendingRewardOperation);
        held.Dispose(); Assert.True(f.Inventory.TryAcquireSaveLease(out var inventory)); inventory.Dispose();
        Assert.Throws<InvalidOperationException>(() => owner.HoldRewardQueue());
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(denied)); Assert.True(denied.IsSaveRejected);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void IdentityContextsMustBeFreshBeforeAnyInventoryOrProcessLeaseIsTaken(bool transaction)
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); using var identity = f.Context();
        using var unit = transaction ? identity.Database.BeginTransaction(IsolationLevel.Serializable) : null;
        if (!transaction) identity.Character.Single();
        Assert.Throws<InvalidOperationException>(() => owner.TryAcquireRewardPersistence(identity, [], out _));
        Assert.False(owner.HasPendingRewardOperation); Assert.True(f.Inventory.TryAcquireSaveLease(out var inventory)); inventory.Dispose();
    }

    [Fact]
    public void AReservationWithTheSameCharacterIdStillMustBelongToThisInventory()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f);
        var foreign = RecordingDispatchProxy<IInventoryRewardReservation>.Create(out var proxy); proxy.SetProperty(nameof(IInventoryRewardReservation.CharacterId), 42ul);
        using var identity = f.Context(); Assert.False(owner.TryAcquireRewardPersistence(identity, [], out _, foreign));
        Assert.False(owner.HasPendingRewardOperation); Assert.True(f.Inventory.TryAcquireSaveLease(out var inventory)); inventory.Dispose();
    }

    internal static void PrepareInventory(Fixture f)
    {
        using var held = f.Reserve(f.Catalog.GetTrack(16).Slots[0].Candidates[0].Id, 0); Assert.True(held.Cancel());
    }
    internal static ChallengeManager Manager(Fixture f)
    {
        var proxy = (RecordingDispatchProxy<IGameTableManager>)(object)f.Tables;
        proxy.SetProperty(nameof(IGameTableManager.ChallengeTier), PlayerBindPointTests.Table([
            new ChallengeTierEntry { Id = 1, Count = 10 }, new ChallengeTierEntry { Id = 2, Count = 20 }, new ChallengeTierEntry { Id = 3, Count = 30 }]));
        using var read = f.Context(); var model = new CharacterModel { Id = 42 };
        foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
        var manager = new ChallengeManager(f.Player, model, f.Tables); f.PlayerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager); return manager;
    }
    private static CharacterChallengeRewardExecutor Executor(Fixture f) => new(f.Context, f.Catalog, f.Tables);
    internal static ChallengeRuntimeState Runtime(ChallengeManager manager, ushort id)
        => ((Dictionary<ushort, ChallengeRuntimeState>)typeof(ChallengeManager).GetField("activeChallenges", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!)[id];
    private static CharacterChallengeModel Model(ChallengeRuntimeState state) => new()
    {
        Id = 42, ChallengeId = (ushort)state.ChallengeId, ActiveAttemptId = state.ActiveAttemptId, Activated = state.Activated,
        OnCooldown = state.OnCooldown, LeftArea = state.LeftArea, CurrentCount = state.CurrentCount, CurrentTier = state.CurrentTier,
        LastRewardTier = state.LastRewardTier, CompletionCount = state.CompletionCount, ActiveTimerSeconds = state.ActiveTimer,
        CooldownTimerSeconds = state.CooldownTimer, AreaFailTimerSeconds = state.AreaFailTimer
    };
    private static Player BarePlayer(Fixture f, ChallengeManager manager)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Identity { RealmId = 1, Id = 42 });
        foreach (var pair in new Dictionary<string, object> { [nameof(Player.Inventory)] = f.Inventory, [nameof(Player.ChallengeManager)] = manager,
            [nameof(Player.Session)] = f.Player.Session, [nameof(Player.Account)] = f.Player.Account })
            typeof(Player).GetProperty(pair.Key)!.SetValue(player, pair.Value);
        typeof(Player).GetField("saveTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, new UpdateTimer(1000));
        return player;
    }
    private sealed class LostAck : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => throw new IOException("Owned ordinary-save commit acknowledgement fault.");
    }
}
