using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Script.Template;
using NexusForever.Shared.Game;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameQuest = NexusForever.Game.Quest.Quest;

namespace NexusForever.Game.Tests.Quest;

public sealed class QuestRewardCaptureTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public QuestRewardCaptureTests() { }
    internal QuestRewardCaptureTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task RetryEnabledPreparationOwnsTheWholeReadTransactionWithoutReplayingActorEffects(int faultPhase)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f);
        int effects = 0, contexts = 0;
        using var capture = Capture(f, manager, () => { effects++; Active(manager).Single().Progress = 2; });
        var fault = new RetryProbeCommit(faultPhase);
        CharacterContext Context()
        {
            Interlocked.Increment(ref contexts);
            return new(new DbContextOptionsBuilder<CharacterContext>(f.Source.Options)
                .ReplaceService<IExecutionStrategyFactory, ProbeRetryFactory>().AddInterceptors(fault).Options);
        }
        var preparation = await capture.PrepareAsync(Context);
        Assert.Equal(faultPhase == 0 ? 2 : 3, contexts);
        Assert.Equal(1, effects); Assert.Equal(1u, Read(f).Objectives[0].Progress);
        Assert.Equal(2u, Assert.Single(Assert.Single(preparation.Changes).After.Objectives).Progress);
        var result = await capture.ExecuteAsync(Executor(f), preparation, Gold(f)); AssertCommitted(result);
        Assert.True(capture.AdoptCommitted(result)); Assert.Equal(1, effects);
        Assert.Equal(2u, Read(f).Objectives[0].Progress);
        using var read = f.Context(); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count());
    }

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task CompletionCapturesRealObjectiveEventAndAdoptsOnlyItsCommittedRevision(uint track)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f, track);
        using var manager = Manager(f); var quest = Active(manager);
        Assert.True(manager.TryCaptureRewardCompletion(Database(f), () => manager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1), out var capture));
        using (capture)
        {
            var prepared = await capture.PrepareAsync(() => f.Context());
            Assert.Equal(2u, Assert.Single(Assert.Single(prepared.Changes).After.Objectives).Progress);
            var result = await capture.ExecuteAsync(Executor(f), prepared, Gold(f, track));
            AssertCommitted(result); Assert.Equal(2u, Read(f).Objectives[0].Progress);
            quest.Single().Progress = 3; quest.Single().Timer = 1234; quest.Timer = 700; quest.Flags = QuestStateFlags.Tracked;
            Assert.True(capture.AdoptCommitted(result)); Assert.False(manager.HasPendingRewardOperation); Assert.False(manager.RequiresAuthoritativeReload);
            Assert.Equal(3u, quest.Single().Progress); Assert.True(quest.NeedsPersistenceSave);
        }
        await Save(f, manager); Assert.Equal(3u, Read(f).Objectives[0].Progress); Assert.Equal(1234u, Read(f).Objectives[0].Timer);
        Assert.Equal(700u, Read(f).Quest.Timer); Assert.Equal((byte)QuestStateFlags.Tracked, Read(f).Quest.Flags);
        Assert.False(quest.NeedsPersistenceSave);
        using var read = f.Context(); Assert.Equal(300u, read.CharacterChallengeRewardTrack.Single().Points); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task LaterSameValueTouchesRemainDirtyUntilAnOrdinarySaveAcknowledgesThem()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        using var capture = Capture(f, manager, () => quest.Single().Progress = 2);
        var prepared = await capture.PrepareAsync(() => f.Context()); var result = await capture.ExecuteAsync(Executor(f), prepared, Gold(f));
        quest.Single().Progress = 2; quest.Reset = quest.Reset;
        Assert.True(capture.AdoptCommitted(result)); Assert.True(quest.NeedsPersistenceSave);
        await Save(f, manager); Assert.False(quest.NeedsPersistenceSave);
    }

    [Fact]
    public async Task UnchangedCapturedRowsAreCleanAfterCommitAndNoQuestChangesStillCompleteTheMedal()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        using var capture = Capture(f, manager, () => { }); var prepared = await capture.PrepareAsync(() => f.Context());
        Assert.Empty(prepared.Changes);
        var result = await capture.ExecuteAsync(Executor(f), prepared, Gold(f)); AssertCommitted(result);
        Assert.True(capture.AdoptCommitted(result)); Assert.False(quest.NeedsPersistenceSave); Assert.Empty(result.QuestStates);
    }

    [Fact]
    public async Task TwoCompletionsAreFifoAndTheSecondUsesTheFirstCommittedBaseline()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17); using var manager = Manager(f); var quest = Active(manager);
        using var first = Capture(f, manager, () => manager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1));
        using var second = Capture(f, manager, () => manager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => second.PrepareAsync(() => f.Context()));
        var one = await first.ExecuteAsync(Executor(f), await first.PrepareAsync(() => f.Context()), Gold(f)); AssertCommitted(one);
        Assert.True(first.AdoptCommitted(one)); Assert.Equal(3u, quest.Single().Progress); Assert.True(manager.HasPendingRewardOperation);
        var prepared = await second.PrepareAsync(() => f.Context()); Assert.Equal(2u, prepared.Changes[0].Before.Objectives[0].Progress);
        var two = await second.ExecuteAsync(Executor(f), prepared, Gold(f, 17)); AssertCommitted(two); Assert.True(second.AdoptCommitted(two));
        Assert.Equal(3u, Read(f).Objectives[0].Progress); Assert.False(quest.NeedsPersistenceSave); Assert.False(manager.HasPendingRewardOperation);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AnUnfinishedNormalRootOrDirectLeafSaveRefusesCaptureBeforeCallingTheEvent(bool leaf)
    {
        using var f = new Fixture(options); f.Seed(); using var manager = Manager(f); var quest = Active(manager); quest.Single().Progress = 2;
        await using var context = f.Context(); await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (leaf) quest.Single().Save(context); else quest.Save(context);
        bool called = false; Assert.False(manager.TryCaptureRewardCompletion(Database(f), () => called = true, out _)); Assert.False(called);
        await context.SaveChangesAsync(); await unit.CommitAsync();
        using var capture = Capture(f, manager, () => called = true); Assert.True(called);
    }

    [Fact]
    public void ARewardCaptureExcludesEveryLoadedManagerAndDirectQuestOrLeafSaveForItsCharacter()
    {
        using var f = new Fixture(options); f.Seed(); using var owner = Manager(f); using var other = f.Manager(); var quest = Active(other);
        using var capture = Capture(f, owner, () => Active(owner).Single().Progress = 2);
        bool called = false; Assert.False(other.TryCaptureRewardCompletion(Database(f), () => called = true, out _)); Assert.False(called);
        quest.Single().Progress = 4;
        using (var direct = f.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Single().Save(direct)); Assert.True(direct.IsSaveRejected); }
        using (var root = f.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Save(root)); Assert.True(root.IsSaveRejected); }
        using var aggregate = f.Context(); Assert.Throws<InvalidOperationException>(() => owner.Save(aggregate)); Assert.True(aggregate.IsSaveRejected);
    }

    [Fact]
    public async Task PreparationsAndExecutionResultsRequireTheExactCaptureProof()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        using var capture = Capture(f, manager, () => quest.Single().Progress = 2); var prep = await capture.PrepareAsync(() => f.Context());
        var forged = new QuestManager.QuestRewardCapture.Preparation(prep.Token, prep.Changes);
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.ExecuteAsync(Executor(f), forged, Gold(f)));
        var result = await capture.ExecuteAsync(Executor(f), prep, Gold(f)); AssertCommitted(result);
        Assert.False(capture.AdoptCommitted(result with { })); Assert.False(capture.AbandonAfterTerminal(result with { }));
        Assert.True(manager.HasPendingRewardOperation); Assert.True(capture.AdoptCommitted(result)); Assert.False(capture.AdoptCommitted(result));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ACommittedCascadeRetiresTheDeletedOwnerButALaterRestorationRecreatesEveryLeaf(bool restore)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        using var capture = Capture(f, manager, () => quest.EnqueueDelete(true)); var prep = await capture.PrepareAsync(() => f.Context());
        Assert.Null(prep.Changes[0].After); var result = await capture.ExecuteAsync(Executor(f), prep, Gold(f)); AssertCommitted(result);
        if (restore) { quest.EnqueueDelete(false); quest.Single().Progress = 3; }
        Assert.True(capture.AdoptCommitted(result)); Assert.Null(Read(f));
        if (restore)
        {
            Assert.Same(quest, Active(manager)); await Save(f, manager); Assert.Equal(3u, Read(f).Objectives[0].Progress); Assert.False(quest.NeedsPersistenceSave);
        }
        else
        {
            Assert.Empty(manager.GetActiveQuests());
            using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => quest.Single().Save(denied)); Assert.True(denied.IsSaveRejected);
        }
    }

    [Fact]
    public async Task AReplacementOwnerIsRebasedWithoutAcknowledgingItsNewIntentOrRevivingTheOldOwner()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var old = Active(manager);
        using var capture = Capture(f, manager, () => old.Single().Progress = 2);
        var prepared = await capture.PrepareAsync(() => f.Context()); var result = await capture.ExecuteAsync(Executor(f), prepared, Gold(f)); AssertCommitted(result);
        var replacement = f.New(); replacement.Single().Progress = 4;
        ActiveDictionary(manager)[Fixture.QuestId] = replacement;
        Assert.True(capture.AdoptCommitted(result)); Assert.Same(replacement, Active(manager)); Assert.True(replacement.NeedsPersistenceSave);
        using (var denied = f.Context()) { Assert.Throws<InvalidOperationException>(() => old.Save(denied)); Assert.True(denied.IsSaveRejected); }
        await Save(f, manager); Assert.Equal(4u, Read(f).Objectives[0].Progress); Assert.False(replacement.NeedsPersistenceSave);
    }

    [Fact]
    public async Task TheCensusIncludesNewQuestsCreatedByObjectiveScriptsAndTheirOtherScalarChanges()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); QuestManager manager = null;
        var extra = RecordingDispatchProxy<IQuestInfo>.Create(out var info);
        info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = 9002 });
        info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList<IQuestObjectiveInfo>.Empty);
        var script = RecordingDispatchProxy<IQuestScript>.Create(out var scriptProxy);
        int calls = 0;
        scriptProxy.SetMethodHandler(nameof(IQuestScript.OnObjectiveUpdate), _ => { calls++; manager.QuestMention(extra); Active(manager).Timer = 800; return null; });
        var collection = RecordingDispatchProxy<IScriptCollection>.Create(out var collectionProxy);
        collectionProxy.SetMethodHandler(nameof(IScriptCollection.Invoke), (method, args) =>
        {
            if (method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(IQuestScript)) ((Action<IQuestScript>)args[0])(script);
            return null;
        });
        ((RecordingDispatchProxy<IScriptManager>)(object)f.Scripts).SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        using (manager = Manager(f))
        using (var capture = Capture(f, manager, () => manager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1)))
        {
            Assert.Equal(1, calls); var prep = await capture.PrepareAsync(() => f.Context()); Assert.Equal(2, prep.Changes.Count);
            var result = await capture.ExecuteAsync(Executor(f), prep, Gold(f)); AssertCommitted(result); Assert.True(capture.AdoptCommitted(result));
            Assert.Equal(1, calls); Assert.Equal(800u, Read(f).Quest.Timer);
            using var read = f.Context(); Assert.Equal((byte)QuestState.Mentioned, read.CharacterQuest.Single(row => row.QuestId == 9002).State);
        }
    }

    [Fact]
    public void AThrowingEventRetiresBothExistingAndNewOwnersBeforeReleasingTheSqlScope()
    {
        using var f = new Fixture(options); f.Seed(); using var manager = Manager(f); var old = Active(manager); var added = f.New();
        Assert.Throws<IOException>(() => manager.TryCaptureRewardCompletion(Database(f), () =>
        {
            old.Single().Progress = 2; ActiveDictionary(manager)[Fixture.QuestId] = added; throw new IOException("Owned event fault.");
        }, out _));
        Assert.True(manager.RequiresAuthoritativeReload); Assert.False(manager.HasPendingRewardOperation);
        using (var denied = f.Context()) Assert.Throws<InvalidOperationException>(() => old.Save(denied));
        using var second = f.Context(); Assert.Throws<InvalidOperationException>(() => added.Save(second)); Assert.True(second.IsSaveRejected);
    }

    [Theory]
    [InlineData("root")] [InlineData("progress")] [InlineData("missing-root")] [InlineData("missing-leaf")] [InlineData("extra-leaf")]
    public async Task PreparationRejectsAnUnownedSqlGraphWithoutCreditingTheReward(string mutation)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f);
        using var capture = Capture(f, manager, () => Active(manager).Single().Progress = 2);
        using (var context = f.Context())
        {
            if (mutation == "root") context.CharacterQuest.ExecuteUpdate(s => s.SetProperty(row => row.Flags, (byte)99));
            if (mutation == "progress") context.CharacterQuestObjective.ExecuteUpdate(s => s.SetProperty(row => row.Progress, 99u));
            if (mutation == "missing-root") context.CharacterQuest.ExecuteDelete();
            if (mutation == "missing-leaf") context.CharacterQuestObjective.ExecuteDelete();
            if (mutation == "extra-leaf") context.Database.ExecuteSqlRaw("INSERT INTO character_quest_objective (id, questId, `index`, progress, timer) VALUES (42, 9001, 99, 0, NULL)");
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PrepareAsync(() => f.Context()));
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
        capture.Dispose(); Assert.True(manager.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task APreviousUncertainOrdinarySaveIsReconciledByItsActualStoredCandidate(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager); quest.Single().Progress = 2;
        await using (var context = f.Context(new CommitFault(after)))
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync(); await Assert.ThrowsAsync<IOException>(() => unit.CommitAsync());
        }
        using var capture = Capture(f, manager, () => quest.Single().Progress = 3);
        var prep = await capture.PrepareAsync(() => f.Context()); Assert.Equal(after ? 2u : 1u, prep.Changes[0].Before.Objectives[0].Progress);
        var result = await capture.ExecuteAsync(Executor(f), prep, Gold(f)); AssertCommitted(result); Assert.True(capture.AdoptCommitted(result));
        Assert.Equal(3u, Read(f).Objectives[0].Progress); Assert.False(quest.NeedsPersistenceSave);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RewardCommitFaultsKeepExactQuestOwnershipUntilTheTerminalResultIsHandled(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f);
        using var capture = Capture(f, manager, () => Active(manager).Single().Progress = 2); var prep = await capture.PrepareAsync(() => f.Context());
        var fault = new CommitFault(after);
        var result = await capture.ExecuteAsync(new(() => f.Context(fault), f.Source.Catalog, f.Source.Tables), prep, Gold(f));
        Assert.True(result.OriginalUnitEnded); Assert.True(manager.HasPendingRewardOperation);
        if (after) { AssertCommitted(result); Assert.True(capture.AdoptCommitted(result)); Assert.Equal(2u, Read(f).Objectives[0].Progress); }
        else { Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome); Assert.True(capture.AbandonAfterTerminal(result)); Assert.True(manager.RequiresAuthoritativeReload); }
        Assert.False(manager.HasPendingRewardOperation);
    }

    [Fact]
    public async Task FinalInvalidationDuringCommitWaitsForSqlAndNeverAdoptsLateActorState()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        using var capture = Capture(f, manager, () => quest.Single().Progress = 2); var prep = await capture.PrepareAsync(() => f.Context());
        var pause = new PauseCommit(); var pending = capture.ExecuteAsync(new(() => f.Context(pause), f.Source.Catalog, f.Source.Tables), prep, Gold(f));
        try
        {
            await pause.Reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); manager.InvalidateRewardOperations(true);
            Assert.True(manager.HasPendingRewardOperation); Assert.True(manager.RequiresAuthoritativeReload); quest.Single().Progress = 4;
        }
        finally { pause.Release.TrySetResult(); }
        var result = await pending; AssertCommitted(result);
        // A synchronous continuation may run just after the await resumes.
        await Task.Run(async () => { for (int i = 0; i < 100 && manager.HasPendingRewardOperation; i++) await Task.Delay(10); });
        Assert.False(manager.HasPendingRewardOperation); Assert.False(capture.AdoptCommitted(result)); Assert.Equal(4u, quest.Single().Progress);
        Assert.Equal(2u, Read(f).Objectives[0].Progress);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => quest.Save(denied)); Assert.True(denied.IsSaveRejected);
    }

    [Fact]
    public async Task ResetNormalizationUsesTheProviderValueWithoutChangingTheLiveCapturedValue()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var manager = Manager(f); var quest = Active(manager);
        DateTime raw = new DateTime(2026, 10, 30, 12, 34, 56, DateTimeKind.Utc).AddTicks(9876543);
        DateTime reference;
        using (var context = f.Context())
        {
            context.CharacterQuest.Add(new() { Id = 42, QuestId = 9002, State = 1, Reset = raw }); context.SaveChanges();
            context.ChangeTracker.Clear(); reference = context.CharacterQuest.AsNoTracking().Single(row => row.QuestId == 9002).Reset!.Value;
        }
        using var capture = Capture(f, manager, () => { quest.Reset = raw; quest.Single().Progress = 2; });
        var result = await capture.ExecuteAsync(Executor(f), await capture.PrepareAsync(() => f.Context()), Gold(f)); AssertCommitted(result);
        Assert.True(capture.AdoptCommitted(result)); Assert.Equal(raw, quest.Reset); Assert.Equal(reference, Read(f).Quest.Reset); Assert.False(quest.NeedsPersistenceSave);
        quest.Flags = QuestStateFlags.Tracked; await Save(f, manager); Assert.Equal(reference, Read(f).Quest.Reset);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingOrQuarantinedQuestWorkBlocksPlayerBeforeItsAuthPhase(bool quarantine)
    {
        using var f = new Fixture(options); f.Seed(); using var manager = Manager(f); using var capture = Capture(f, manager, () => Active(manager).Single().Progress = 2);
        if (quarantine) capture.Dispose(); var player = BarePlayer(f, manager);
        await Assert.ThrowsAsync<InvalidOperationException>(() => player.SaveDirect());
        using var auth = new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite("Data Source=:memory:").Options);
        Assert.Throws<InvalidOperationException>(() => player.Save(auth)); Assert.Empty(auth.ChangeTracker.Entries());
        using var context = f.Context(); Assert.Throws<InvalidOperationException>(() => player.Save(context)); Assert.True(context.IsSaveRejected);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AnAlreadyRunningFullPlayerSaveRefusesTheQuestEventBeforeAnyActorMutation(bool direct)
    {
        using var f = new Fixture(options); f.Seed(); var player = BarePlayer(f, null);
        using var context = f.Context(); var model = context.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single();
        using var manager = new QuestManager(player, model, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
        typeof(Player).GetProperty(nameof(Player.QuestManager))!.SetValue(player, manager);
        typeof(Player).GetField(direct ? "directSaveOperations" : "saveInProgress", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, direct ? (object)1 : true);
        bool invoked = false; Assert.False(manager.TryCaptureRewardCompletion(Database(f), () => invoked = true, out _)); Assert.False(invoked);
        Assert.False(manager.HasPendingRewardOperation); Assert.Equal(1u, Active(manager).Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AProbeMustUseAFreshUntrackedContextAndDisposalRequiresReload(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); using var manager = Manager(f); using var capture = Capture(f, manager, () => Active(manager).Single().Progress = 2);
        await using var context = f.Context(); await using var unit = tracked ? null : await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (tracked) await context.CharacterQuest.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PrepareAsync(() => context));
        capture.Dispose(); Assert.True(manager.RequiresAuthoritativeReload); Assert.False(manager.HasPendingRewardOperation);
    }

    [Fact]
    public async Task AQueuedReplacementCanFollowThePreviousOwnersCommitWithoutLosingItsNewGraph()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17); using var manager = Manager(f); var old = Active(manager);
        using var first = Capture(f, manager, () => old.Single().Progress = 2);
        var replacement = f.New(); replacement.Single().Progress = 4;
        using var second = Capture(f, manager, () => ActiveDictionary(manager)[Fixture.QuestId] = replacement);
        var one = await first.ExecuteAsync(Executor(f), await first.PrepareAsync(() => f.Context()), Gold(f)); AssertCommitted(one); Assert.True(first.AdoptCommitted(one));
        var prep = await second.PrepareAsync(() => f.Context()); Assert.Equal(2u, prep.Changes[0].Before.Objectives[0].Progress);
        var two = await second.ExecuteAsync(Executor(f), prep, Gold(f, 17)); AssertCommitted(two); Assert.True(second.AdoptCommitted(two));
        Assert.False(replacement.NeedsPersistenceSave); Assert.Equal(4u, Read(f).Objectives[0].Progress);
    }

    private static QuestManager Manager(Fixture f) { var manager = f.Manager(); f.Source.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), manager); return manager; }
    private static GameQuest Active(QuestManager manager) => Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests()));
    private static Dictionary<ushort, IQuest> ActiveDictionary(QuestManager manager) => (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
    private static string Database(Fixture f) { using var context = f.Context(); return Game.Entity.Item.GetRewardDatabaseKey(context); }
    private static QuestManager.QuestRewardCapture Capture(Fixture f, QuestManager manager, Action action)
        { Assert.True(manager.TryCaptureRewardCompletion(Database(f), action, out var capture)); return capture; }
    private static CharacterChallengeRewardExecutor Executor(Fixture f) => new(() => f.Context(), f.Source.Catalog, f.Source.Tables);
    private static async Task Ready(Fixture f, uint track = 16) { await f.Source.Begin(track); await f.Source.Medal(track, 0, new(1, 1)); await f.Source.Medal(track, 1, new(1, 2)); }
    private static CharacterChallengeRewardCommand Gold(Fixture f, uint track = 16)
    {
        var before = f.Source.State(track);
        return CharacterChallengeRewardCommand.Credit(before, before with { Activated = false, Tier = 2, LastRewardTier = 2, Completions = before.Completions + 1, ActiveSeconds = 0 }, 2, new(1, 3), DateTime.UtcNow);
    }
    private static CharacterChallengeQuestSnapshot Read(Fixture f)
    {
        using var context = f.Context(); var root = context.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.QuestId == Fixture.QuestId);
        return root == null ? null : new(CharacterChallengeQuestState.FromModel(root), context.CharacterQuestObjective.AsNoTracking().Where(row => row.QuestId == Fixture.QuestId).AsEnumerable().Select(CharacterChallengeQuestObjectiveState.FromModel));
    }
    private static async Task Save(Fixture f, QuestManager manager) { await using var context = f.Context(); manager.Save(context); await context.SaveChangesAsync(); }
    private static void AssertCommitted(CharacterChallengeRewardExecution result) => Assert.True(result.Outcome == CharacterChallengeRewardExecutionOutcome.Committed, result.Error?.ToString());
    private static Player BarePlayer(Fixture f, QuestManager manager)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Identity { RealmId = 1, Id = 42 });
        foreach (var pair in new Dictionary<string, object> { [nameof(Player.Inventory)] = f.Source.Inventory, [nameof(Player.QuestManager)] = manager,
            [nameof(Player.Session)] = f.Source.Player.Session, [nameof(Player.Account)] = f.Source.Player.Account }) typeof(Player).GetProperty(pair.Key)!.SetValue(player, pair.Value);
        typeof(Player).GetField("saveTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, new UpdateTimer(1000)); return player;
    }
    private sealed class ProbeTransientException : IOException;
    private sealed class ProbeRetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new ProbeRetryStrategy(dependencies);
    }
    private sealed class ProbeRetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception error) => error is ProbeTransientException;
    }
    private sealed class RetryProbeCommit(int phase) : DbTransactionInterceptor
    {
        private int remaining = 1;
        private void Fail(int current) { if (phase == current && Interlocked.Exchange(ref remaining, 0) == 1) throw new ProbeTransientException(); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            { Fail(1); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            { Fail(2); return Task.CompletedTask; }
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int remaining = 1;
        private void Fail() { if (Interlocked.Exchange(ref remaining, 0) == 1) throw new IOException("Owned Quest capture commit fault."); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            { if (!after) Fail(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            { if (after) Fail(); return Task.CompletedTask; }
    }
    private sealed class PauseCommit : DbTransactionInterceptor
    {
        internal TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            { Reached.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken); return result; }
    }
}
