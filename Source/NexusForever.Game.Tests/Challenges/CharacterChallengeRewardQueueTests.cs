using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Shared.Game.Events;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeRewardQueueTests
{
    private readonly DbContextOptions<CharacterContext> options;
    [ThreadStatic] private static bool pumping;
    public CharacterChallengeRewardQueueTests() { }
    internal CharacterChallengeRewardQueueTests(DbContextOptions<CharacterContext> options) => this.options = options;

    private static ChallengeManager.CharacterRewardQueue Queue(Fixture f, ChallengeManager owner, EventQueue events,
        Action<CharacterChallengeRewardExecution> complete = null, Action fail = null,
        Func<CharacterContext> contexts = null, int recoveries = 3)
        => new(owner, contexts ?? f.Context, new(contexts ?? f.Context, f.Catalog, f.Tables), events,
            complete, fail, TimeSpan.Zero, recoveries);
    private static void Pump(EventQueue events)
    {
        pumping = true;
        try { events.Update(.01); } finally { pumping = false; }
    }
    private static async Task Until(Func<bool> ready, EventQueue events = null)
    {
        var timeout = Stopwatch.StartNew();
        while (!ready())
        {
            if (events != null) Pump(events);
            Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(15), "Queued reward work did not settle.");
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task OrderedMedalsUseTheAdoptedBaselineAndHoldSavesAcrossEveryCallback()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        var events = new EventQueue(); var points = new List<uint>(); var baselines = new List<uint>();
        var queue = Queue(f, owner, events, result =>
        {
            Assert.True(pumping); Assert.True(owner.HasPendingRewardOperation);
            Assert.Throws<InvalidOperationException>(() => { using var denied = f.Context(); owner.Save(denied); });
            points.Add(result.State.Tracks.Single().Points);
        }, contexts: () => { Assert.False(pumping); return f.Context(); });
        using var read = f.Context(); var before = CharacterChallengeState.FromModel(read.CharacterChallenge.AsNoTracking().Single());
        Runtime(owner, id).CurrentTier = 1;
        var intents = new[] { owner.CaptureRewardIntent(id) };
        var bronze = before with { Tier = 1, LastRewardTier = 0 };
        Assert.True(queue.Enqueue(intents, lease =>
        {
            Assert.True(pumping); var expected = lease.Expected(id); baselines.Add(expected.Tier);
            return CharacterChallengeRewardCommand.Credit(expected, bronze, 0, new(1, 1), DateTime.UtcNow);
        }));
        intents[0] = null; // collection mutation cannot rewrite the queued copy
        Runtime(owner, id).CurrentTier = 2; Runtime(owner, id).LastRewardTier = 1;
        var silver = before with { Tier = 2, LastRewardTier = 1 };
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], lease =>
        {
            var expected = lease.Expected(id); baselines.Add(expected.Tier);
            return CharacterChallengeRewardCommand.Credit(expected, silver, 1, new(1.25f, 2), DateTime.UtcNow);
        }));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(new uint[] { 0, 1 }, baselines); Assert.Equal(new uint[] { 100, 225 }, points);
        Assert.False(owner.RequiresAuthoritativeReload); Assert.Equal(2u, Runtime(owner, id).CurrentTier);
        using var save = f.Context(); owner.Save(save); await save.SaveChangesAsync(); Assert.Equal(225u, save.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task APreexistingNormalSaveDelaysTheQueueWithoutRunningSqlOnTheActor()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var events = new EventQueue(); int calls = 0, commits = 0;
        Assert.True(f.Inventory.TryAcquireSaveLease(out var save));
        var queue = Queue(f, owner, events, _ => ++commits, contexts: () => { Assert.False(pumping); Interlocked.Increment(ref calls); return f.Context(); });
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42)));
        await Until(() => Volatile.Read(ref calls) == 1); Pump(events);
        await Task.Delay(20); Pump(events); Assert.Equal(0, commits); Assert.True(owner.HasPendingRewardOperation);
        save.Dispose(); await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, commits); Assert.False(owner.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UncertainOrdinarySaveCandidatesAreLoadedBeforeTheCommandIsBuilt(bool committed)
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16); owner.Update(1);
        await using (var context = committed ? f.Intercepted(new LostAck()) : f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            owner.Save(context); await context.SaveChangesAsync();
            if (committed) await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync()); else await transaction.RollbackAsync();
        }
        var events = new EventQueue(); double? baseline = null; int built = 0;
        var queue = Queue(f, owner, events);
        using var read = f.Context(); var after = CharacterChallengeState.FromModel(read.CharacterChallenge.AsNoTracking().Single()) with { Tier = 1, LastRewardTier = 0, ActiveSeconds = 299 };
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], lease =>
        {
            ++built; var before = lease.Expected(id); baseline = before.ActiveSeconds;
            return CharacterChallengeRewardCommand.Credit(before, after, 0, new(1, 1), DateTime.UtcNow);
        }));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, built); Assert.Equal(committed ? 299d : 300d, baseline); Assert.False(owner.RequiresAuthoritativeReload);
        using var verify = f.Context(); Assert.Equal(100u, verify.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task LostCommitAcknowledgementResumesTheExactCommandWithoutRebuildingIntent()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        var events = new EventQueue(); var ack = new LostAck(); int contexts = 0, built = 0, complete = 0;
        var queue = Queue(f, owner, events, _ => ++complete, contexts: () =>
        {
            // identity1, strategy2, original unit3, unavailable probe4
            if (Interlocked.Increment(ref contexts) == 4) throw new IOException("Queue recovery probe unavailable.");
            return f.Intercepted(ack);
        });
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], lease =>
        {
            ++built; var before = lease.Expected(id);
            return CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1 }, 0, new(1.25f, 4), DateTime.UtcNow);
        }));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, complete); Assert.Equal(1, built); Assert.False(owner.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(125u, read.CharacterChallengeRewardTrack.Single().Points); Assert.Single(read.CharacterChallengeMedalReceipt);
    }

    [Fact]
    public async Task ExhaustedRecoveryQuarantinesTheOldOwnerAndReleasesOnlyTheEndedUnit()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16);
        var events = new EventQueue(); int contexts = 0, failed = 0;
        var queue = Queue(f, owner, events, fail: () => ++failed, recoveries: 1, contexts: () =>
            Interlocked.Increment(ref contexts) >= 4 ? throw new IOException("Queue probes unavailable.") : f.Intercepted(new LostAck()));
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], _ => CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true)));
        await Until(() => owner.RequiresAuthoritativeReload && !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, failed); Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42)));
        var fresh = Manager(f); using var identity = f.Context(); Assert.True(fresh.TryAcquireRewardPersistence(identity, [], out var held)); held.Dispose();
        using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidationDuringIdentityPreparationCancelsPendingWorkWithoutActorCallbacks(bool final)
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var events = new EventQueue();
        using var started = new ManualResetEventSlim(); using var release = new ManualResetEventSlim(); int committed = 0;
        var queue = Queue(f, owner, events, _ => ++committed, contexts: () => { started.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); return f.Context(); });
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42)));
        await Until(() => started.IsSet); owner.InvalidateRewardOperations(final); release.Set();
        await Until(() => !events.PendingEvents, events);
        Assert.True(owner.RequiresAuthoritativeReload); Assert.False(owner.HasPendingRewardOperation); Assert.Equal(0, committed);
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExecutingInvalidationSettlesWithoutPublicationAndFinalDisposalNeedsNoEventPump(bool final)
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16); var events = new EventQueue();
        var pause = new PausedCommit(); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Intercepted(pause));
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], _ => CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true)));
        await Until(() => pause.Started.Task.IsCompleted, events);
        owner.InvalidateRewardOperations(final); Assert.True(owner.HasPendingRewardOperation); pause.Release.TrySetResult();
        await Until(() => !owner.HasPendingRewardOperation, final ? null : events);
        Assert.Equal(0, callbacks); Assert.Equal(final, owner.RequiresAuthoritativeReload);
        Assert.Equal(!final, owner.IsChallengeActivated(id));
        using var identity = f.Context(); var fresh = Manager(f); Assert.True(fresh.TryAcquireRewardPersistence(identity, [], out var held)); held.Dispose();
    }

    [Fact]
    public async Task CancelingLaterQueuedIntentRequiresReloadEvenWhenTheCurrentUnitCanCommit()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16); var events = new EventQueue(); var pause = new PausedCommit();
        var queue = Queue(f, owner, events, contexts: () => f.Intercepted(pause));
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], _ => CharacterChallengeRewardCommand.Activate(null, f.Activated(16), Guid.NewGuid().ToString("N"), true)));
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42)));
        await Until(() => pause.Started.Task.IsCompleted, events); owner.InvalidateRewardOperations(); Assert.True(owner.RequiresAuthoritativeReload);
        pause.Release.TrySetResult(); await Until(() => !owner.HasPendingRewardOperation, events);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(denied));
    }

    [Fact]
    public async Task AThrowingPublicationCallbackCannotReopenTheCommittedOperationOrLeakItsTicket()
    {
        using var f = new Fixture(options); await f.Begin(16); PrepareInventory(f); var owner = Manager(f); ushort id = f.Challenge(16); var events = new EventQueue(); int failures = 0;
        var queue = Queue(f, owner, events, _ => throw new IOException("Queue notification failed."), () => ++failures);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], lease =>
        {
            var before = lease.Expected(id); return CharacterChallengeRewardCommand.Credit(before, before with { Tier = 1 }, 0, new(1, 1), DateTime.UtcNow);
        }));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.True(owner.RequiresAuthoritativeReload); Assert.Equal(1, failures); Assert.Equal(1u, Runtime(owner, id).CurrentTier);
        using var read = f.Context(); Assert.Single(read.CharacterChallengeMedalReceipt); Assert.Equal(100u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task InventoryPublicationRunsOnTheActorAfterTheClaimWasCommitted()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var delivery = f.Reserve(input.Row, 0); var owner = Manager(f); var events = new EventQueue(); int callbacks = 0;
        var pause = new PausedCommit();
        var queue = Queue(f, owner, events, _ => { Assert.True(pumping); Assert.Equal(InventoryRewardReservationState.Published, delivery.State); ++callbacks; }, contexts: () => f.Intercepted(pause));
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Claim(42, 16, input.Cycle, input.Revision, input.Row, 0, Guid.NewGuid().ToString("N"), DateTime.UtcNow), delivery));
        await Until(() => pause.Started.Task.IsCompleted, events); pause.Release.TrySetResult();
        await Until(() => delivery.State == InventoryRewardReservationState.Committed);
        // The completed SQL task is not itself permission to publish on a worker.
        Assert.Equal(0, callbacks); Assert.False(f.Inventory.TryAcquireSaveLease(out _));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, callbacks); Assert.True(f.Inventory.TryAcquireSaveLease(out var save)); save.Dispose();
        using var read = f.Context(); Assert.Single(read.CharacterChallengeRewardClaim); Assert.NotEmpty(read.Item);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InitialReloadComposesPersistedCharacterTracksWithAccountRows(bool persisted)
    {
        using var f = new Fixture(options); if (persisted) { await f.Begin(16); await f.Medal(16, 0, new(1, 1)); }
        PrepareInventory(f); _ = Manager(f); var events = new EventQueue(); f.SessionProxy.SetProperty("Events", events);
        var presentation = new RewardTrackPresentation(f.Player); f.PlayerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
        using var read = f.Context(); var model = new CharacterModel { Id = 42 };
        foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
        var owner = new ChallengeManager(f.Player, model, f.Tables, f.Context); owner.SendInitialPackets();
        Assert.True(presentation.PublishAccountTracks([new RewardTrack { RewardTrackId = 1 }]));
        Assert.DoesNotContain(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"), call => call.Arguments[0] is ServerRewardTracksReload);
        await Until(() => !owner.HasPendingRewardOperation, events);
        var packet = Assert.IsType<ServerRewardTracksReload>(Assert.Single(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"), call => call.Arguments[0] is ServerRewardTracksReload).Arguments[0]);
        Assert.Equal(persisted ? new uint[] { 1, 16 } : new uint[] { 1 }, packet.RewardsTracks.Select(row => row.RewardTrackId));
        if (persisted) { var track = packet.RewardsTracks.Single(row => row.RewardTrackId == 16); Assert.Equal(100u, track.RewardPointsEarned); Assert.Equal(new uint[] { 320, 321, 324 }, track.RewardTrackRewardsIds); }
        owner.SendInitialPackets(); Pump(events);
        Assert.Single(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"), call => call.Arguments[0] is ServerRewardTracksReload);
        Assert.NotNull(owner.CommittedCharacterRewards); Assert.False(owner.RequiresAuthoritativeReload);
        Assert.Equal(persisted ? 1 : 0, read.CharacterChallengeRewardTrack.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AnInvalidatedClaimSettlesItsInventoryWithoutMessagesOrFurtherEventPolling(bool final)
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var delivery = f.Reserve(input.Row, 0); var owner = Manager(f); var events = new EventQueue(); var pause = new PausedCommit(); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Intercepted(pause));
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Claim(42, 16, input.Cycle, input.Revision, input.Row, 0, Guid.NewGuid().ToString("N"), DateTime.UtcNow), delivery));
        await Until(() => pause.Started.Task.IsCompleted, events);
        owner.InvalidateRewardOperations(final); pause.Release.TrySetResult();
        await Until(() => !owner.HasPendingRewardOperation, final ? null : events);
        Assert.Equal(0, callbacks); Assert.Equal(InventoryRewardReservationState.Published, delivery.State);
        Assert.Empty(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"));
        Assert.False(f.Inventory.HasPendingRewardDelivery); Assert.True(f.Inventory.TryAcquireSaveLease(out var held)); held.Dispose();
        using var read = f.Context(); Assert.Single(read.CharacterChallengeRewardClaim); Assert.NotEmpty(read.Item);
    }

    [Fact]
    public void ForeignReservationsAreRejectedBeforeTakingAQueueTicketOrStartingIdentityWork()
    {
        using var f = new Fixture(options); PrepareInventory(f); var owner = Manager(f); var events = new EventQueue(); int contexts = 0;
        var queue = Queue(f, owner, events, contexts: () => { ++contexts; return f.Context(); });
        var foreign = RecordingDispatchProxy<IInventoryRewardReservation>.Create(out _);
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), foreign));
        Assert.False(owner.HasPendingRewardOperation); Assert.False(events.PendingEvents); Assert.Equal(0, contexts);
    }

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)] [InlineData(19u)] [InlineData(20u)]
    [InlineData(21u)] [InlineData(22u)] [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task EverySupportedCatalogReloadsItsDurableOrdinalSelectionWithoutResampling(uint track)
    {
        using var f = new Fixture(options); await f.Begin(track); PrepareInventory(f); _ = Manager(f);
        var events = new EventQueue(); f.SessionProxy.SetProperty("Events", events);
        var presentation = new RewardTrackPresentation(f.Player); f.PlayerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
        using var read = f.Context(); var model = new CharacterModel { Id = 42 };
        foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
        uint[] selected = read.CharacterChallengeRewardSlot.AsNoTracking().OrderBy(row => row.Ordinal).Select(row => row.RewardRowId).ToArray();
        var owner = new ChallengeManager(f.Player, model, f.Tables, f.Context); owner.SendInitialPackets();
        Assert.True(presentation.PublishAccountTracks([new RewardTrack { RewardTrackId = 1 }]));
        await Until(() => !owner.HasPendingRewardOperation, events);
        var packet = Assert.IsType<ServerRewardTracksReload>(Assert.Single(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"), call => call.Arguments[0] is ServerRewardTracksReload).Arguments[0]);
        var published = Assert.Single(packet.RewardsTracks, row => row.RewardTrackId == track);
        Assert.Equal(selected, published.RewardTrackRewardsIds); Assert.Equal(f.Catalog.GetTrack(track).Slots.Count, selected.Length);
        Assert.False(owner.RequiresAuthoritativeReload); Assert.Single(read.CharacterChallengeAttempt);
    }

    private sealed class LostAck : DbTransactionInterceptor
    {
        private int commits;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Interlocked.Increment(ref commits) == 1 ? Task.FromException(new IOException("Queue commit acknowledgement fault.")) : Task.CompletedTask;
    }
    private sealed class PausedCommit : DbTransactionInterceptor
    {
        public readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(15)); return result;
        }
    }
}
