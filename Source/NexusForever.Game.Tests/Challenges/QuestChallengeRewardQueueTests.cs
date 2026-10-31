using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared.Game.Events;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameQuest = NexusForever.Game.Quest.Quest;

namespace NexusForever.Game.Tests.Challenges;

public sealed class QuestChallengeRewardQueueTests
{
    private readonly DbContextOptions<CharacterContext> options;
    [ThreadStatic] private static bool pumping;
    public QuestChallengeRewardQueueTests() { }
    internal QuestChallengeRewardQueueTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u, false)] [InlineData(16u, false)] [InlineData(17u, false)] [InlineData(18u, false)]
    [InlineData(19u, false)] [InlineData(20u, false)] [InlineData(21u, false)] [InlineData(22u, false)]
    [InlineData(23u, false)] [InlineData(24u, false)] [InlineData(25u, false)] [InlineData(26u, false)]
    [InlineData(27u, false)] [InlineData(28u, false)] [InlineData(33u, false)] [InlineData(36u, false)]
    [InlineData(15u, true)] [InlineData(16u, true)] [InlineData(17u, true)] [InlineData(18u, true)]
    [InlineData(19u, true)] [InlineData(20u, true)] [InlineData(21u, true)] [InlineData(22u, true)]
    [InlineData(23u, true)] [InlineData(24u, true)] [InlineData(25u, true)] [InlineData(26u, true)]
    [InlineData(27u, true)] [InlineData(28u, true)] [InlineData(33u, true)] [InlineData(36u, true)]
    public async Task TerminalMedalOrFinishAdoptsBothAggregatesBeforePublishingOnce(uint track, bool finish)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f, track); var owner = Manager(f.Source); using var quests = Quests(f);
        var events = new EventQueue(); int callbacks = 0;
        var queue = Queue(f, owner, events, result =>
        {
            Assert.True(pumping); ++callbacks; Assert.True(owner.HasPendingRewardOperation); Assert.False(quests.HasPendingRewardOperation);
            Assert.False(Runtime(owner, f.Source.Challenge(track)).Activated); Assert.Equal(1u, Runtime(owner, f.Source.Challenge(track)).CompletionCount);
            Assert.False(Active(quests).NeedsPersistenceSave); Assert.Equal(2u, Active(quests).Single().Progress);
            Assert.Equal(2u, Assert.Single(Assert.Single(result.QuestStates).Value.Objectives).Progress);
        });
        using var capture = Capture(f, quests); Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(track))], held => Terminal(held, f, track, finish), quests: capture));
        await Until(() => !owner.HasPendingRewardOperation, events); Assert.Equal(1, callbacks);
        Assert.False(owner.RequiresAuthoritativeReload); Assert.False(quests.RequiresAuthoritativeReload);
        await Save(f, owner, quests);
        using var read = f.Context(); Assert.Equal(finish ? 200u : 300u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(finish ? 2 : 3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task LaterChallengeAndQuestIntentSurvivesTheSharedCommitAndOrdinarySave()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var pause = new PauseWrite();
        var queue = Queue(f, owner, events, contexts: () => f.Context(pause)); using var capture = Capture(f, quests);
        ushort id = f.Source.Challenge(16); Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], held => Terminal(held, f), quests: capture));
        try
        {
            await Until(() => pause.Reached.Task.IsCompleted, events);
            owner.Update(2); Assert.True(owner.TryAdvanceProgress(id, 3)); Runtime(owner, id).ShareTimer = 17;
            Active(quests).Single().Progress = 4; Active(quests).Timer = 700;
            using var blocked = f.Context(); Assert.Throws<InvalidOperationException>(() => quests.Save(blocked)); Assert.True(blocked.IsSaveRejected);
        }
        finally { pause.Release.TrySetResult(); }
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(3u, Runtime(owner, id).CurrentCount); Assert.Equal(298d, Runtime(owner, id).ActiveTimer); Assert.Equal(17d, Runtime(owner, id).ShareTimer);
        Assert.Equal(4u, Active(quests).Single().Progress); Assert.True(Active(quests).NeedsPersistenceSave);
        await Save(f, owner, quests); Assert.False(Active(quests).NeedsPersistenceSave);
        using var read = f.Context(); Assert.Equal(3u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(4u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(700u, read.CharacterQuest.Single().Timer);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task QueuedCompletionsFollowBothCommittedBaselinesIncludingAReplacement(bool replace)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int completed = 0;
        var queue = Queue(f, owner, events, _ => ++completed); using var first = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: first));
        GameQuest replacement = null;
        using var second = Capture(f, quests, () =>
        {
            if (replace) { replacement = f.New(); replacement.Single().Progress = 4; ActiveDictionary(quests)[Fixture.QuestId] = replacement; }
            else quests.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1);
        });
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(17))], held => Terminal(held, f, 17), quests: second));
        await Until(() => !owner.HasPendingRewardOperation, events); Assert.Equal(2, completed);
        Assert.False(quests.RequiresAuthoritativeReload); Assert.False(owner.RequiresAuthoritativeReload); Assert.False(Active(quests).NeedsPersistenceSave);
        using var read = f.Context(); Assert.Equal(replace ? 4u : 3u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallenge.Count(row => row.CompletionCount == 1));
        if (replace) Assert.Same(replacement, Active(quests));
    }

    [Fact]
    public async Task LostAcknowledgementAndUnavailableRecoveryResumeOneExactCommandWithoutReplayingTheEvent()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue();
        var fault = new LostWriteAck(); int rejected = 0, built = 0, called = 0, committed = 0;
        var queue = Queue(f, owner, events, _ => ++committed, contexts: () =>
        {
            if (fault.Fired && Interlocked.Exchange(ref rejected, 1) == 0) throw new IOException("Shared outcome probe unavailable.");
            return f.Context(fault);
        });
        using var capture = Capture(f, quests, () => { ++called; quests.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1); });
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => { ++built; return Terminal(held, f); }, quests: capture));
        await Until(() => !owner.HasPendingRewardOperation, events);
        Assert.Equal(1, called); Assert.Equal(1, built); Assert.Equal(1, committed); Assert.Equal(1, rejected);
        Assert.False(owner.RequiresAuthoritativeReload); Assert.False(quests.RequiresAuthoritativeReload); Assert.False(Active(quests).NeedsPersistenceSave);
        using var read = f.Context(); Assert.Equal(300u, read.CharacterChallengeRewardTrack.Single().Points); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UncertainNormalChallengeSaveIsLoadedBeforeTheQuestRewardCommandIsBuilt(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); owner.Update(1); using var quests = Quests(f);
        await using (var context = after ? f.Context(new LostAck()) : f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable); owner.Save(context); await context.SaveChangesAsync();
            if (after) await Assert.ThrowsAsync<IOException>(() => unit.CommitAsync()); else await unit.RollbackAsync();
        }
        var events = new EventQueue(); double? before = null; var queue = Queue(f, owner, events); using var capture = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held =>
        {
            before = held.Expected(f.Source.Challenge(16)).ActiveSeconds; return Terminal(held, f);
        }, quests: capture));
        await Until(() => !owner.HasPendingRewardOperation, events); Assert.Equal(after ? 299d : 300d, before);
        Assert.False(owner.RequiresAuthoritativeReload); Assert.False(quests.RequiresAuthoritativeReload); Assert.False(Active(quests).NeedsPersistenceSave);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidationDuringIdentityOrQuestProbeCancelsBothOwnersWithoutWriting(bool questProbe)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue();
        using var reached = new ManualResetEventSlim(); using var release = new ManualResetEventSlim(); int contexts = 0, callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () =>
        {
            if (Interlocked.Increment(ref contexts) == (questProbe ? 2 : 1)) { reached.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(15))); }
            return f.Context();
        });
        using var capture = Capture(f, quests); Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
        try { await Until(() => reached.IsSet, events); owner.InvalidateRewardOperations(true); }
        finally { release.Set(); }
        await Until(() => !events.PendingEvents, events); Assert.Equal(0, callbacks); Assert.False(owner.HasPendingRewardOperation); Assert.False(quests.HasPendingRewardOperation);
        Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidationDuringWriteRetainsBothUntilTerminalProofAndPublishesNothing(bool final)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var pause = new PauseWrite(); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Context(pause)); using var capture = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
        try
        {
            await Until(() => pause.Reached.Task.IsCompleted, events); owner.InvalidateRewardOperations(final);
            Assert.True(owner.HasPendingRewardOperation); Assert.True(quests.HasPendingRewardOperation); Active(quests).Single().Progress = 4;
        }
        finally { pause.Release.TrySetResult(); }
        await Until(() => !owner.HasPendingRewardOperation && !quests.HasPendingRewardOperation, final ? null : events);
        Assert.Equal(0, callbacks); Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload);
        Assert.True(Runtime(owner, f.Source.Challenge(16)).Activated); Assert.Equal(4u, Active(quests).Single().Progress);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Fact]
    public async Task CancelingALaterCaptureQuarantinesBothAndDoesNotPublishTheEarlierCommit()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var pause = new PauseWrite(); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Context(pause)); using var first = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: first));
        using var second = Capture(f, quests); Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(17))], held => Terminal(held, f, 17), quests: second));
        try { await Until(() => pause.Reached.Task.IsCompleted, events); owner.InvalidateRewardOperations(true); }
        finally { pause.Release.TrySetResult(); }
        await Until(() => !owner.HasPendingRewardOperation && !quests.HasPendingRewardOperation);
        Assert.Equal(0, callbacks); Assert.Equal(3u, Active(quests).Single().Progress);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(1, read.CharacterChallenge.Count(row => row.CompletionCount == 1));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EitherAggregateRejectingAdoptionQuarantinesBothBeforeAcknowledgingTheOther(bool questFailure)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var pause = new PauseWrite(); int completed = 0, failed = 0;
        var queue = Queue(f, owner, events, _ => ++completed, () => ++failed, contexts: () => f.Context(pause)); using var capture = Capture(f, quests);
        ushort id = f.Source.Challenge(16); Assert.True(queue.Enqueue([owner.CaptureRewardIntent(id)], held => Terminal(held, f), quests: capture));
        try
        {
            await Until(() => pause.Reached.Task.IsCompleted, events);
            if (questFailure) quests.InvalidateRewardOperations(); else Runtime(owner, id).ActiveAttemptId = Guid.NewGuid().ToString("N");
        }
        finally { pause.Release.TrySetResult(); }
        await Until(() => !owner.HasPendingRewardOperation && !quests.HasPendingRewardOperation, events);
        Assert.Equal(0, completed); Assert.Equal(1, failed); Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload);
        if (questFailure) Assert.True(Runtime(owner, id).Activated); // Quest rejection happened before Challenge Apply.
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => Active(quests).Save(denied)); Assert.True(denied.IsSaveRejected);
    }

    [Fact]
    public async Task AThrowingPublicationCannotReopenEitherCommittedAggregate()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int failed = 0;
        var queue = Queue(f, owner, events, _ => throw new IOException("Shared publication fault."), () => ++failed); using var capture = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
        await Until(() => !owner.HasPendingRewardOperation, events); Assert.Equal(1, failed); Assert.False(quests.HasPendingRewardOperation); Assert.False(Active(quests).NeedsPersistenceSave);
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42))); using var read = f.Context(); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task DuplicateCaptureSubmissionCannotTakeAnotherQueueTicket()
    {
        using var f = new Fixture(options); f.Seed(); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var queue = Queue(f, owner, events);
        using var capture = Capture(f, quests); Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: capture));
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: capture));
        owner.InvalidateRewardOperations(true); Assert.False(quests.HasPendingRewardOperation);
        await Until(() => !events.PendingEvents, events);
    }

    [Fact]
    public void AForeignPlayerCaptureWithTheSameCharacterIdIsRejectedBeforeIdentityWork()
    {
        using var f = new Fixture(options); f.Seed(); var owner = Manager(f.Source); var events = new EventQueue(); int contexts = 0;
        var foreign = RecordingDispatchProxy<IPlayer>.Create(out var proxy); proxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        using var read = f.Context(); var model = new CharacterModel { Id = 42 }; model.Quest.Add(read.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single());
        using var quests = new QuestManager(foreign, model, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
        var queue = Queue(f, owner, events, contexts: () => { ++contexts; return f.Context(); }); using var capture = Capture(f, quests, () => { });
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: capture)); Assert.Equal(0, contexts);
        Assert.False(owner.HasPendingRewardOperation); Assert.False(events.PendingEvents);
    }

    [Fact]
    public void ACompletionCaptureCannotBeCombinedWithAnItemClaimReservation()
    {
        using var f = new Fixture(options); f.Seed(); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var queue = Queue(f, owner, events);
        using var capture = Capture(f, quests); using var delivery = f.Source.Reserve(f.Source.Catalog.GetTrack(16).Slots[0].Candidates[0].Id, 0);
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), delivery, capture));
        Assert.False(owner.HasPendingRewardOperation); Assert.False(events.PendingEvents); Assert.True(delivery.Cancel());
    }

    [Fact]
    public async Task OutOfOrderSubmissionIsRejectedWithoutTakingOwnershipAndCanBeSubmittedInOrder()
    {
        using var f = new Fixture(options); f.Seed(); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int calls = 0;
        var queue = Queue(f, owner, events, contexts: () => { ++calls; return f.Context(); }); using var first = Capture(f, quests); using var second = Capture(f, quests);
        Assert.False(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: second)); Assert.Equal(0, calls); Assert.False(owner.HasPendingRewardOperation);
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: first));
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: second)); owner.InvalidateRewardOperations(true);
        await Until(() => !events.PendingEvents, events); Assert.False(owner.HasPendingRewardOperation); Assert.False(quests.HasPendingRewardOperation);
    }

    [Fact]
    public async Task OneQuestManagersCapturedOrderCannotBeSplitAcrossTwoLoadedChallengeQueues()
    {
        using var f = new Fixture(options); f.Seed(); var owner = Manager(f.Source); var other = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var otherEvents = new EventQueue();
        var queue = Queue(f, owner, events); var foreignQueue = Queue(f, other, otherEvents); using var first = Capture(f, quests); using var second = Capture(f, quests);
        Assert.True(queue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: first));
        Assert.False(foreignQueue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(42), quests: second)); Assert.False(other.HasPendingRewardOperation); Assert.False(otherEvents.PendingEvents);
        owner.InvalidateRewardOperations(true); second.Dispose(); await Until(() => !events.PendingEvents, events); Assert.False(quests.HasPendingRewardOperation);
    }

    [Fact]
    public async Task AChangedRetainedDatabaseQuarantinesBothBeforeStartingASqlProbe()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int calls = 0;
        using var identity = f.Context(); Assert.True(quests.TryCaptureRewardCompletion(Game.Entity.Item.GetRewardDatabaseKey(identity) + "-foreign", () => Active(quests).Single().Progress = 2, out var capture));
        using (capture)
        {
            var queue = Queue(f, owner, events, contexts: () => { ++calls; return f.Context(); });
            Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
            await Until(() => !owner.HasPendingRewardOperation, events); Assert.Equal(1, calls);
            Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload); Assert.False(quests.HasPendingRewardOperation);
            using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AWriteCommitFaultResolvesBothAsOneUnitBeforeEitherOwnerCanSave(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); var fault = new WriteFault(after); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Context(fault)); using var capture = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
        await Until(() => !owner.HasPendingRewardOperation && !quests.HasPendingRewardOperation, events);
        Assert.Equal(after ? 1 : 0, callbacks); Assert.Equal(!after, owner.RequiresAuthoritativeReload); Assert.Equal(!after, quests.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(after ? 2u : 1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(after ? 1u : 0u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(after ? 3 : 2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task CancelingWhileANormalInventorySaveOwnsTheQueueReleasesBothWithoutCrediting()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int identities = 0;
        Assert.True(f.Source.Inventory.TryAcquireSaveLease(out var save));
        try
        {
            var queue = Queue(f, owner, events, contexts: () => { Interlocked.Increment(ref identities); return f.Context(); }); using var capture = Capture(f, quests);
            Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
            await Until(() => Volatile.Read(ref identities) == 1, events); owner.InvalidateRewardOperations(true);
            await Until(() => !events.PendingEvents, events); Assert.False(owner.HasPendingRewardOperation); Assert.False(quests.HasPendingRewardOperation);
            Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload);
        }
        finally { save.Dispose(); }
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task ASqlMutationAfterFlushRollsBackTheMedalAndTheCompleteQuestGraph()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); var owner = Manager(f.Source); using var quests = Quests(f); var events = new EventQueue(); int callbacks = 0;
        var queue = Queue(f, owner, events, _ => ++callbacks, contexts: () => f.Context(new TamperFlush())); using var capture = Capture(f, quests);
        Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture));
        await Until(() => !owner.HasPendingRewardOperation && !quests.HasPendingRewardOperation, events);
        Assert.Equal(0, callbacks); Assert.True(owner.RequiresAuthoritativeReload); Assert.True(quests.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(200u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task ScriptCreatedQuestsJoinTheSameQueueUnitWithoutReinvokingTheScript()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); QuestManager quests = null;
        var extra = RecordingDispatchProxy<IQuestInfo>.Create(out var info); info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = 9002 }); info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList<IQuestObjectiveInfo>.Empty);
        var script = RecordingDispatchProxy<IQuestScript>.Create(out var sp); int called = 0;
        sp.SetMethodHandler(nameof(IQuestScript.OnObjectiveUpdate), _ => { ++called; quests.QuestMention(extra); return null; });
        var scripts = RecordingDispatchProxy<IScriptCollection>.Create(out var cp);
        cp.SetMethodHandler(nameof(IScriptCollection.Invoke), (method, args) => { if (method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(IQuestScript)) ((Action<IQuestScript>)args[0])(script); return null; });
        ((RecordingDispatchProxy<IScriptManager>)(object)f.Scripts).SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scripts);
        using (quests = Quests(f))
        {
            var owner = Manager(f.Source); var events = new EventQueue(); var queue = Queue(f, owner, events); using var capture = Capture(f, quests);
            Assert.True(queue.Enqueue([owner.CaptureRewardIntent(f.Source.Challenge(16))], held => Terminal(held, f), quests: capture)); await Until(() => !owner.HasPendingRewardOperation, events);
            Assert.Equal(1, called); using var read = f.Context(); Assert.Equal(2, read.CharacterQuest.Count()); Assert.False(quests.RequiresAuthoritativeReload);
        }
    }

    private static QuestManager Quests(Fixture f) { var manager = f.Manager(); f.Source.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), manager); return manager; }
    private static GameQuest Active(QuestManager manager) => Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests()));
    private static Dictionary<ushort, IQuest> ActiveDictionary(QuestManager manager) => (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
    private static QuestManager.QuestRewardCapture Capture(Fixture f, QuestManager quests, Action action = null)
    {
        using var identity = f.Context(); Assert.True(quests.TryCaptureRewardCompletion(Game.Entity.Item.GetRewardDatabaseKey(identity), action ?? (() => quests.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1)), out var capture)); return capture;
    }
    private static async Task Ready(Fixture f, uint track = 16) { await f.Source.Begin(track); await f.Source.Medal(track, 0, new(1, 1)); await f.Source.Medal(track, 1, new(1, 2)); }
    private static CharacterChallengeRewardCommand Terminal(ChallengeManager.RewardPersistenceLease held, Fixture f, uint track = 16, bool finish = false)
    {
        var before = held.Expected(f.Source.Challenge(track)); var after = before with { Activated = false, ActiveSeconds = 0, Completions = before.Completions + 1 };
        return finish ? CharacterChallengeRewardCommand.Finish(before, after) : CharacterChallengeRewardCommand.Credit(before, after with { Tier = 2, LastRewardTier = 2 }, 2, new(1, 3), DateTime.UtcNow);
    }
    private static ChallengeManager.CharacterRewardQueue Queue(Fixture f, ChallengeManager owner, EventQueue events, Action<CharacterChallengeRewardExecution> complete = null, Action fail = null, Func<CharacterContext> contexts = null)
    {
        contexts ??= () => f.Context(); var factory = contexts;
        return new(owner, () => { Assert.False(pumping); return factory(); }, new(() => { Assert.False(pumping); return factory(); }, f.Source.Catalog, f.Source.Tables), events, complete, fail, TimeSpan.Zero);
    }
    private static async Task Until(Func<bool> ready, EventQueue events = null)
    {
        var timeout = Stopwatch.StartNew();
        while (!ready())
        {
            if (events != null) { pumping = true; try { events.Update(.01); } finally { pumping = false; } }
            Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(20), "Shared Quest/Challenge queue did not settle."); await Task.Delay(5);
        }
    }
    private static async Task Save(Fixture f, ChallengeManager owner, QuestManager quests) { await using var context = f.Context(); owner.Save(context); quests.Save(context); await context.SaveChangesAsync(); }
    private sealed class PauseWrite : DbTransactionInterceptor
    {
        internal TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Any()) { Reached.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(30)); } return result;
        }
    }
    private sealed class LostWriteAck : DbTransactionInterceptor
    {
        private int fired;
        internal bool Fired => Volatile.Read(ref fired) != 0;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Any() && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Shared commit acknowledgement lost."); return Task.CompletedTask;
        }
    }
    private sealed class LostAck : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) => throw new IOException("Normal Challenge acknowledgement lost.");
    }
    private sealed class WriteFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        private void Fail(DbContext context) { if (context!.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Any() && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Shared write commit fault."); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            { if (!after) Fail(eventData.Context); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            { if (after) Fail(eventData.Context); return Task.CompletedTask; }
    }
    private sealed class TamperFlush : SaveChangesInterceptor
    {
        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            var context = (CharacterContext)eventData.Context!;
            if (context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Any())
                await context.Database.ExecuteSqlRawAsync("UPDATE character_quest_objective SET progress=99 WHERE id=42 AND questId=9001", cancellationToken);
            return result;
        }
    }
}
