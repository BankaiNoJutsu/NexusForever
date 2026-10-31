using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;
using NexusForever.GameTable;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared.Game;
using NexusForever.Shared.Game.Events;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Game.Static.Entity;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameQuest = NexusForever.Game.Quest.Quest;

namespace NexusForever.Game.Tests.Challenges;

public sealed class RewardNormalSaveSnapshotTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public RewardNormalSaveSnapshotTests() { }
    internal RewardNormalSaveSnapshotTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task RealDirectSaveFreezesBeforeAuthAndTheCompletionQueueFollowsItsStoredBaseline(uint track)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f, track); using var actor = new Actor(f); await actor.Load();
        int calls = 0; actor.OnAuth = () =>
        {
            ++calls; Assert.True(actor.Player.IsNormalSaveInProgress); var capture = actor.Complete(); Assert.True(actor.QueueTerminal(capture, track));
        };
        await actor.Player.SaveDirect(); Assert.Equal(1, calls); Assert.False(actor.Player.IsNormalSaveInProgress);
        using (var read = f.Context()) { Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); }
        await actor.Drain();
        Assert.False(actor.Quests.RequiresAuthoritativeReload); Assert.False(actor.Challenges.RequiresAuthoritativeReload); Assert.False(actor.Quest.NeedsPersistenceSave);
        Assert.False(Runtime(actor.Challenges, f.Source.Challenge(track)).Activated);
        using var final = f.Context(); Assert.Equal(2u, final.CharacterQuestObjective.Single().Progress); Assert.Equal(1u, final.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(3, final.CharacterChallengeMedalReceipt.Count()); Assert.Equal(300u, final.CharacterChallengeRewardTrack.Single().Points); Assert.Empty(final.Item);
    }

    [Fact]
    public async Task RealDeferredSaveKeepsItsFrozenTargetsAcrossTheAuthCharacterEventGap()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); int auth = 0;
        actor.OnAuth = () => { if (++auth == 1) Assert.True(actor.QueueTerminal(actor.Complete())); };
        actor.StartSave(); await actor.Drain();
        Assert.False(actor.Player.IsNormalSaveInProgress); Assert.False(actor.Challenges.RequiresAuthoritativeReload); Assert.False(actor.Quests.RequiresAuthoritativeReload);
        // The Quest event requested another save; its deferred dirty intent is
        // allowed to run once reward ownership has ended.
        actor.StartSave(); await actor.Drain(); Assert.Equal(2, auth);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Fact]
    public async Task ScriptCreatedQuestIsExcludedFromTheOlderSaveAndCommittedOnlyWithItsCompletion()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); Actor actor = null; int calls = 0;
        var extra = ExtraInfo(9002, false);
        var script = RecordingDispatchProxy<IQuestScript>.Create(out var sp); sp.SetMethodHandler(nameof(IQuestScript.OnObjectiveUpdate), _ => { ++calls; actor.Quests.QuestMention(extra); return null; });
        var collection = RecordingDispatchProxy<IScriptCollection>.Create(out var cp); cp.SetMethodHandler(nameof(IScriptCollection.Invoke), (method, args) =>
        { if (method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(IQuestScript)) ((Action<IQuestScript>)args[0])(script); return null; });
        ((RecordingDispatchProxy<IScriptManager>)(object)f.Scripts).SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        using (actor = new Actor(f))
        {
            await actor.Load(); actor.OnAuth = () => Assert.True(actor.QueueTerminal(actor.Complete()));
            await actor.Player.SaveDirect(); using (var read = f.Context()) Assert.Single(read.CharacterQuest);
            await actor.Drain(); Assert.Equal(1, calls);
            using var final = f.Context(); Assert.Equal(2, final.CharacterQuest.Count()); Assert.Equal((byte)QuestState.Mentioned, final.CharacterQuest.Single(row => row.QuestId == 9002).State);
        }
    }

    [Fact]
    public async Task AQuestAcceptedAfterTheImmediateCompletionDoesNotReceiveTheOlderEvent()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); GameQuest later = null;
        actor.OnAuth = () =>
        {
            var capture = actor.Complete();
            later = new(actor.Player, ExtraInfo(9002, true), globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
            later.State = QuestState.Accepted; Active(actor.Quests).Add(9002, later); Assert.True(actor.QueueTerminal(capture));
        };
        await actor.Player.SaveDirect(); await actor.Drain(); Assert.Equal(0u, later.Single().Progress); Assert.True(later.NeedsPersistenceSave);
        using (var read = f.Context()) Assert.Single(read.CharacterQuest);
        actor.OnAuth = null; await actor.Player.SaveDirect(); using var final = f.Context(); Assert.Equal(0u, final.CharacterQuestObjective.Single(row => row.QuestId == 9002).Progress);
    }

    [Fact]
    public async Task SequentialObjectivesKeepTheirOriginalEventOrderAcrossTheAuthPhaseAndFifoCommits()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17);
        ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 2 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90011, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5,
                Flags = (uint)(QuestObjectiveFlags.RequiresPreviousObjectives | QuestObjectiveFlags.Sequential) })));
        using (var context = f.Context()) { context.CharacterQuestObjective.Add(new() { Id = 42, QuestId = Fixture.QuestId, Index = 1 }); context.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load();
        actor.OnAuth = () =>
        {
            Assert.True(actor.QueueTerminal(actor.Complete())); Assert.Equal(new uint[] { 2, 0 }, actor.Quest.Select(row => row.Progress));
            Assert.True(actor.QueueTerminal(actor.Complete(), 17)); Assert.Equal(new uint[] { 2, 1 }, actor.Quest.Select(row => row.Progress));
        };
        await actor.Player.SaveDirect();
        using (var read = f.Context()) Assert.Equal(new uint[] { 1, 0 }, read.CharacterQuestObjective.OrderBy(row => row.Index).Select(row => row.Progress));
        await actor.Drain(); using var final = f.Context(); Assert.Equal(new uint[] { 2, 1 }, final.CharacterQuestObjective.OrderBy(row => row.Index).Select(row => row.Progress));
        Assert.Equal(2, final.CharacterChallenge.Count(row => row.CompletionCount == 1));
    }

    [Fact]
    public void LoadedObjectiveCollectionOrderCannotChangeSequentialProgression()
    {
        using var f = new Fixture(options);
        ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 2 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90011, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5, Flags = (uint)QuestObjectiveFlags.Sequential })));
        var model = new CharacterQuestModel { Id = 42, QuestId = Fixture.QuestId, State = (byte)QuestState.Accepted,
            QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = Fixture.QuestId, Index = 1, Progress = 0 }, new() { Id = 42, QuestId = Fixture.QuestId, Index = 0, Progress = 1 } } };
        var quest = new GameQuest(f.Source.Player, f.Info, model, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
        Assert.Equal(new byte[] { 0, 1 }, quest.Select(row => row.Index));
        quest.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1); Assert.Equal(new uint[] { 2, 0 }, quest.Select(row => row.Progress));
        quest.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1); Assert.Equal(new uint[] { 2, 1 }, quest.Select(row => row.Progress));
    }

    [Fact]
    public async Task EventAfterCharacterStagingCannotChangeTheFrozenFlushOrAcknowledgeLaterIntent()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        actor.Quest.Single().Progress = 2; actor.Challenges.Update(1);
        Assert.True(f.Source.Inventory.TryAcquireSaveLease(out var lease));
        var snapshot = actor.Player.CaptureNormalRewardSave(); Assert.NotNull(snapshot);
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context);
            Assert.True(actor.QueueTerminal(actor.Complete())); actor.Quest.Single().Progress = 4;
            Assert.True(actor.Challenges.TryAdvanceProgress(f.Source.Challenge(16), 3));
            await context.SaveChangesAsync();
        }
        using (var read = f.Context()) { Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(0u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(299d, read.CharacterChallenge.Single().ActiveTimerSeconds); }
        Assert.True(snapshot.Finish(true)); lease.Dispose(); await actor.Drain();
        Assert.Equal(4u, actor.Quest.Single().Progress); Assert.True(actor.Quest.NeedsPersistenceSave);
        await actor.Player.SaveDirect();
        using var final = f.Context(); Assert.Equal(4u, final.CharacterQuestObjective.Single().Progress); Assert.Equal(3u, final.CharacterChallenge.Single().CurrentCount);
    }

    [Fact]
    public async Task AProbeCannotRunUntilTheFrozenNormalSaveReleasesItsOlderTargets()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        var snapshot = actor.Player.CaptureNormalRewardSave(); using var capture = actor.Complete();
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PrepareAsync(() => f.Context()));
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); var prepared = await capture.PrepareAsync(() => f.Context());
        Assert.Equal(1u, prepared.Changes[0].Before.Objectives[0].Progress); Assert.Equal(2u, prepared.Changes[0].After.Objectives[0].Progress);
    }

    [Fact]
    public async Task SameValueTouchesAfterCaptureStayDirtyUntilANewerSaveAcknowledgesThem()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var snapshot = actor.Player.CaptureNormalRewardSave(); actor.Quest.Single().Progress = 2;
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); Assert.True(actor.Quest.NeedsPersistenceSave);
        await actor.Player.SaveDirect(); Assert.False(actor.Quest.NeedsPersistenceSave);
    }

    [Fact]
    public async Task AReplacementAfterCaptureIsNotWrittenOrAcknowledgedByTheOlderNormalSave()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var old = actor.Quest; var snapshot = actor.Player.CaptureNormalRewardSave();
        var replacement = f.New(); replacement.Single().Progress = 4;
        using var capture = actor.Complete(() => Active(actor.Quests)[Fixture.QuestId] = replacement);
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); using (var read = f.Context()) Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.True(actor.QueueTerminal(capture)); await actor.Drain(); Assert.Same(replacement, actor.Quest); Assert.False(replacement.NeedsPersistenceSave);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => old.Save(denied));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ADeletionSnapshotPreservesLaterRestorationAndRetiresOnlyTheCommittedDeletedOwner(bool restore)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var quest = actor.Quest; quest.EnqueueDelete(true);
        var snapshot = actor.Player.CaptureNormalRewardSave(); if (restore) { quest.EnqueueDelete(false); quest.Single().Progress = 3; }
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); using (var read = f.Context()) { Assert.Empty(read.CharacterQuest); Assert.Empty(read.CharacterQuestObjective); }
        if (restore) { Assert.Same(quest, actor.Quest); await actor.Player.SaveDirect(); using var read = f.Context(); Assert.Equal(3u, read.CharacterQuestObjective.Single().Progress); }
        else Assert.Empty(actor.Quests.GetActiveQuests());
    }

    [Fact]
    public async Task ForeignManagersAndDirectLeafSavesCannotEnterTheAuthPhaseReservation()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var snapshot = actor.Player.CaptureNormalRewardSave();
        using var foreign = f.Manager(); var quest = Assert.IsType<GameQuest>(Assert.Single(foreign.GetActiveQuests())); quest.Single().Progress = 4;
        using (var context = f.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Single().Save(context)); Assert.True(context.IsSaveRejected); }
        using (var identity = f.Context()) Assert.False(foreign.TryCaptureRewardCompletion(Game.Entity.Item.GetRewardDatabaseKey(identity), () => throw new Exception("must not run"), out _));
        Assert.Throws<InvalidOperationException>(() => foreign.CaptureNormalSave(actor.Identity.ExecutionDatabase));
        Assert.True(snapshot.Finish(true));
    }

    [Fact]
    public async Task SnapshotDatabaseAndContextBindingRejectsForeignOrRepeatedEnrollment()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var snapshot = actor.Player.CaptureNormalRewardSave();
        using (var wrong = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:").Options))
        { Assert.Throws<InvalidOperationException>(() => snapshot.Quests.Save(wrong)); Assert.True(wrong.IsSaveRejected); }
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context);
            using var other = f.Context(); Assert.Throws<InvalidOperationException>(() => snapshot.Challenges.Save(other)); Assert.True(other.IsSaveRejected);
            await context.SaveChangesAsync();
        }
        Assert.True(snapshot.Finish(true)); using var ended = f.Context(); Assert.Throws<InvalidOperationException>(() => snapshot.Quests.Save(ended)); Assert.True(ended.IsSaveRejected);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailedNormalCommitCannotLeakProvisionalCompletionThroughAnOrdinaryRetry(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        Assert.True(f.Source.Inventory.TryAcquireSaveLease(out var lease)); var snapshot = actor.Player.CaptureNormalRewardSave(); using var capture = actor.Complete(); Assert.True(actor.QueueTerminal(capture));
        await using (var context = f.Context(new CommitFault(after)))
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); await Assert.ThrowsAsync<IOException>(() => unit.CommitAsync());
        }
        Assert.False(snapshot.Finish(false)); lease.Dispose(); await actor.Drain();
        Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
        Assert.False(actor.Quests.HasPendingRewardOperation); using var read = f.Context(); Assert.Equal(after ? 2u : 1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task AuthFailureAfterCaptureCancelsTheQueueBeforeReleasingTheRealDirectSave()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        actor.OnAuth = () => { Assert.True(actor.QueueTerminal(actor.Complete())); throw new IOException("Owned Auth failure."); };
        await Assert.ThrowsAsync<IOException>(() => actor.Player.SaveDirect()); await actor.Drain();
        Assert.False(actor.Player.IsNormalSaveInProgress); Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task QuiescentRewardInvalidationStillAllowsAnOrdinaryLogoutSaveWithoutReopeningRewards()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        f.Source.Inventory.InvalidateRewardDeliveries(); actor.Challenges.InvalidateRewardOperations(); actor.Quests.InvalidateRewardOperations();
        await actor.Player.SaveDirect(); Assert.False(actor.Player.IsNormalSaveInProgress);
        bool called = false; Assert.False(actor.Quests.TryCaptureRewardCompletion(actor.Identity.ExecutionDatabase, () => called = true, out _)); Assert.False(called);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData("none")] [InlineData("auth")] [InlineData("snapshot")]
    public async Task RealCleanupFinalizesOnceAcrossNormalSaveFailureAndSynchronousSnapshotFailure(string fault)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2; int auth = 0;
        actor.OnAuth = () => { ++auth; if (fault == "auth") throw new IOException("Owned cleanup Auth failure."); };
        if (fault == "snapshot") Assert.NotNull(actor.Player.CaptureNormalRewardSave());
        actor.Cleanup(); await actor.Drain(allowErrors: fault == "snapshot");
        Assert.Single(actor.CleanupProxy.GetInvocations(nameof(ICleanupManager.RemovePlayer)));
        Assert.Single(actor.PublisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)), call => call.Arguments[0] is PlayerLoggedOutMessage);
        Assert.Equal(LogoutState.Finished, actor.Player.LogoutManager.State); Assert.Equal(fault == "snapshot" ? 0 : 1, auth);
        if (fault == "snapshot") Assert.Single(actor.Errors); else Assert.Empty(actor.Errors);
        using var read = f.Context(); Assert.Equal(fault == "none" ? 2u : 1u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task FinishingBeforeTheRealCharacterUnitEndsCannotReleaseItsPersistenceOwnership()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var snapshot = actor.Player.CaptureNormalRewardSave();
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context); Assert.False(snapshot.Finish(true));
            using var foreign = f.Manager(); Assert.False(foreign.TryCaptureRewardCompletion(actor.Identity.ExecutionDatabase, () => { }, out _));
        }
        Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ObjectiveOrdinalsAreExplicitEvenWhenZeroIsAddedAfterAnAlreadyTrackedOne(bool frozen)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load();
        var info = RecordingDispatchProxy<IQuestInfo>.Create(out var qp); qp.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = 9002 });
        qp.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90020, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90021, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 })));
        var quest = new GameQuest(actor.Player, info, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables); Active(actor.Quests).Add(9002, quest);
        if (frozen)
        {
            var snapshot = actor.Player.CaptureNormalRewardSave();
            await using var context = f.Context(); snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); Assert.True(snapshot.Finish(true));
        }
        else
        {
            await using var context = f.Context(); quest.ElementAt(1).Save(context); quest.Save(context);
            var zero = context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Single(row => row.Entity.QuestId == 9002 && row.Entity.Index == 0);
            Assert.False(zero.Property(row => row.Index).IsTemporary); await context.SaveChangesAsync();
        }
        using var read = f.Context(); var property = read.Model.FindEntityType(typeof(CharacterQuestObjectiveModel))!.FindProperty(nameof(CharacterQuestObjectiveModel.Index))!;
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, property.ValueGenerated); Assert.Equal((byte)0, property.GetDefaultValue());
        Assert.Equal(new byte[] { 0, 1 }, read.CharacterQuestObjective.Where(row => row.QuestId == 9002).OrderBy(row => row.Index).Select(row => row.Index));
    }

    private static async Task Ready(Fixture f, uint track = 16) { await f.Source.Begin(track); await f.Source.Medal(track, 0, new(1, 1)); await f.Source.Medal(track, 1, new(1, 2)); }
    private static Dictionary<ushort, IQuest> Active(QuestManager manager) => (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
    private static IQuestInfo ExtraInfo(uint id, bool objective)
    {
        var info = RecordingDispatchProxy<IQuestInfo>.Create(out var proxy); proxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = id });
        proxy.SetProperty(nameof(IQuestInfo.Objectives), objective ? ImmutableList.Create<IQuestObjectiveInfo>(new QuestObjectiveInfo(new QuestObjectiveEntry { Id = id * 10, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 })) : ImmutableList<IQuestObjectiveInfo>.Empty); return info;
    }
    internal sealed class Actor : IDisposable
    {
        private readonly Fixture f;
        internal readonly Player Player;
        internal readonly QuestManager Quests;
        internal readonly ChallengeManager Challenges;
        internal readonly EventQueue Events = new();
        internal readonly List<Exception> Errors = [];
        internal readonly RecordingDispatchProxy<ICleanupManager> CleanupProxy;
        internal readonly RecordingDispatchProxy<IInternalMessagePublisher> PublisherProxy;
        internal readonly ChallengeManager.RewardPersistenceIdentity Identity;
        internal Action OnAuth;
        internal GameQuest Quest => Assert.IsType<GameQuest>(Assert.Single(Quests.GetActiveQuests()));
        internal Actor(Fixture fixture, IPrerequisiteManager prerequisites = null, Func<CharacterContext> rewardContexts = null,
            IGameTableManager questTables = null, IContractManager contracts = null, CharacterDatabase characterDatabase = null,
            IGameTableManager rewardTables = null, Func<AuthContext> authContexts = null, AuthDatabase authDatabase = null)
        {
            f = fixture; _ = Manager(f.Source); // installs reviewed tier goals
            Events.UnhandledExceptionHandler = Errors.Add;
            Player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
            foreach (var property in typeof(Player).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType.IsInterface && p.Name.EndsWith("Manager", StringComparison.Ordinal)))
            {
                var args = new object[] { null }; var value = typeof(RecordingDispatchProxy<>).MakeGenericType(property.PropertyType).GetMethod("Create")!.Invoke(null, args); Set(property.Name, value);
            }
            Set(nameof(Player.Identity), new Identity { Id = 42, RealmId = 1 }); Set(nameof(Player.Inventory), f.Source.Inventory); Set(nameof(Player.Session), f.Source.Player.Session); Set(nameof(Player.Account), f.Source.Player.Account);
            f.Source.SessionProxy.SetProperty("Events", Events);
            foreach (string name in new[] { "tradeskills", "schematics", "taxiNodes", "unsavedTaxiNodes" })
            { var field = typeof(Player).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!; field.SetValue(Player, Activator.CreateInstance(field.FieldType)); }
            foreach (string name in new[] { "taxiStateLock", "bindStateLock" }) Field(name, new object());
            Field("saveTimer", new UpdateTimer(1000));
            var pending = typeof(UnitEntity).GetField("pendingSpells", BindingFlags.NonPublic | BindingFlags.Instance)!; pending.SetValue(Player, Activator.CreateInstance(pending.FieldType));
            var cleanup = RecordingDispatchProxy<ICleanupManager>.Create(out var cleanupProxy); CleanupProxy = cleanupProxy; Field("cleanupManager", cleanup);
            var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var publisherProxy); PublisherProxy = publisherProxy; publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask); Field("messagePublisher", publisher);
            var stats = typeof(WorldEntity).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance)!; stats.SetValue(Player, Activator.CreateInstance(stats.FieldType));
            using var read = f.Context();
            // PersistSecurityInfo=false strips MySQL credentials after opening.
            // Retain the original configuration before the model queries below.
            string configuredConnection = read.Database.GetDbConnection().ConnectionString;
            Identity = ChallengeManager.CaptureRewardPersistenceIdentity(read);
            var model = new CharacterModel { Id = 42 };
            foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
            foreach (var row in read.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective)) model.Quest.Add(row);
            if (rewardTables != null) ((RecordingDispatchProxy<IGameTableManager>)(object)rewardTables).SetProperty(nameof(IGameTableManager.Challenge), f.Source.Tables.Challenge);
            Challenges = new(Player, model, rewardTables ?? f.Source.Tables, rewardContexts ?? (() => f.Context()), prerequisites,
                (IItemManager)(object)f.Source.ManagerProxy, authContexts); Set(nameof(Player.ChallengeManager), Challenges);
            Quests = new(Player, model, prerequisiteManager: prerequisites, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: questTables ?? f.Source.Tables,
                contractManager: contracts); Set(nameof(Player.QuestManager), Quests);
            var config = RecordingDispatchProxy<IConnectionString>.Create(out var cp); cp.SetProperty(nameof(IConnectionString.Provider), read.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql); cp.SetProperty(nameof(IConnectionString.ConnectionString), configuredConnection);
            var auth = authDatabase ?? new AuthDatabase(); if (authDatabase == null) auth.Initialise(config);
            var character = characterDatabase ?? new CharacterDatabase(); character.Initialise(config);
            var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) => method.GetGenericArguments()[0] == typeof(AuthDatabase) ? auth : character); Field("databaseManager", databases);
            ((RecordingDispatchProxy<IAccount>)(object)Player.Account).SetMethodHandler(nameof(IAccount.Save), _ => { OnAuth?.Invoke(); return null; });
        }
        internal void Set(string property, object value)
        {
            var info = typeof(Player).GetProperty(property)!;
            if (info.SetMethod != null) info.SetValue(Player, value);
            else
            {
                var backing = info.DeclaringType!.GetField("<" + property + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (backing != null) backing.SetValue(Player, value);
            }
        }
        private void Field(string name, object value) => typeof(Player).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Player, value);
        internal async Task Load() { Challenges.SendInitialPackets(); await Drain(); Assert.NotNull(Challenges.NormalSaveIdentity); }
        internal void StartSave() => typeof(Player).GetMethod("StartSave", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Player, [null, null]);
        internal void Cleanup() => typeof(Player).GetMethod("Cleanup", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Player, null);
        internal QuestManager.QuestRewardCapture Complete(Action action = null)
        {
            Assert.True(Quests.TryCaptureRewardCompletion(Identity.ExecutionDatabase, action ?? (() => Quests.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1)), out var capture)); return capture;
        }
        internal bool QueueTerminal(QuestManager.QuestRewardCapture capture, uint track = 16)
        {
            var queue = (ChallengeManager.CharacterRewardQueue)typeof(ChallengeManager).GetField("rewardQueue", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Challenges)!;
            return queue.Enqueue([Challenges.CaptureRewardIntent(f.Source.Challenge(track))], held =>
            {
                var before = held.Expected(f.Source.Challenge(track)); return CharacterChallengeRewardCommand.Credit(before,
                    before with { Activated = false, ActiveSeconds = 0, Tier = 2, LastRewardTier = 2, Completions = before.Completions + 1 }, 2, new(1, 3), DateTime.UtcNow);
            }, quests: capture);
        }
        internal async Task Drain(bool allowErrors = false)
        {
            var timeout = Stopwatch.StartNew();
            do { Events.Update(.01); Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(20), "Normal-save/reward work did not settle."); await Task.Delay(5); }
            while (Challenges.HasPendingRewardOperation || Player.IsNormalSaveInProgress || Events.PendingEvents);
            if (!allowErrors) Assert.Empty(Errors);
        }
        public void Dispose()
        {
            if (typeof(Player).GetField("normalRewardSave", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Player) is Player.NormalRewardSave save) save.Finish(false);
            Challenges.InvalidateRewardOperations(true); Quests.Dispose();
        }
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => after ? new(result) : throw new IOException("Frozen normal save before commit.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => after ? Task.FromException(new IOException("Frozen normal save acknowledgement lost.")) : Task.CompletedTask;
    }
}
