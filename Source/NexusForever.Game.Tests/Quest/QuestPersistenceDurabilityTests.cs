using System.Data;
using System.Data.Common;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using SourceFixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;
using GameQuest = NexusForever.Game.Quest.Quest;

namespace NexusForever.Game.Tests.Quest;

public sealed class QuestPersistenceDurabilityTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public QuestPersistenceDurabilityTests() { }
    internal QuestPersistenceDurabilityTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task NewQuestAndObjectivesRemainOwnedUntilTheOuterTransactionCommits()
    {
        using var f = new Fixture(options); var quest = f.New(); var objective = Assert.IsType<QuestObjective>(quest.Single());
        objective.Progress = 2; quest.Flags = QuestStateFlags.Tracked;
        await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.True(quest.PendingCreate); quest.Save(context); Assert.False(quest.PendingCreate);
        await context.SaveChangesAsync(); Assert.True(quest.NeedsPersistenceSave); Assert.True(objective.NeedsPersistenceSave);
        await transaction.CommitAsync(); Assert.False(quest.NeedsPersistenceSave); Assert.False(objective.NeedsPersistenceSave);
        using var read = f.Context(); Assert.Single(read.CharacterQuest); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("failure")] [InlineData("suppressed")]
    public async Task UncommittedOrUnflushedCreatesRetainCompleteIntentForAFreshUnit(string outcome)
    {
        using var f = new Fixture(options); var quest = f.New(); var objective = Assert.IsType<QuestObjective>(quest.Single()); objective.Progress = 3; objective.Timer = 2500; quest.Timer = 900;
        await using (var context = outcome == "failure" ? f.Context(new FailFlush()) : outcome == "suppressed" ? f.Context(new SuppressFlush()) : f.Context())
        {
            quest.Save(context);
            if (outcome == "rollback")
            {
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await context.SaveChangesAsync(); await transaction.RollbackAsync();
            }
            else if (outcome == "failure") await Assert.ThrowsAsync<IOException>(() => context.SaveChangesAsync());
            else if (outcome == "suppressed") Assert.Equal(0, await context.SaveChangesAsync());
        }
        Assert.True(quest.NeedsPersistenceSave); Assert.True(objective.NeedsPersistenceSave);
        using (var read = f.Context()) { Assert.Empty(read.CharacterQuest); Assert.Empty(read.CharacterQuestObjective); }
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        Assert.False(quest.NeedsPersistenceSave); using var verify = f.Context();
        Assert.Equal(900u, verify.CharacterQuest.Single().Timer); var row = Assert.Single(verify.CharacterQuestObjective); Assert.Equal(3u, row.Progress); Assert.Equal(2500u, row.Timer);
    }

    [Fact]
    public async Task LaterQuestAndObjectiveMutationsSurviveAcknowledgementOfAnEarlierFlush()
    {
        using var f = new Fixture(options); var quest = f.New(); var objective = Assert.IsType<QuestObjective>(quest.Single()); objective.Progress = 1;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); objective.Progress = 4; objective.Timer = 1800; quest.Flags = QuestStateFlags.Tracked; quest.Timer = 700;
            await transaction.CommitAsync();
        }
        Assert.True(quest.NeedsPersistenceSave); Assert.True(objective.NeedsPersistenceSave);
        using (var read = f.Context()) { Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Null(read.CharacterQuest.Single().Timer); }
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = f.Context(); Assert.Equal(4u, verify.CharacterQuestObjective.Single().Progress); Assert.Equal(700u, verify.CharacterQuest.Single().Timer);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RestagingTracksTheLastActualFlushRatherThanTheLastCapture(bool flushAgain)
    {
        using var f = new Fixture(options); var quest = f.New(); var objective = quest.Single(); objective.Progress = 1;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); objective.Progress = 3; quest.Save(context);
            if (flushAgain) await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        using (var read = f.Context()) Assert.Equal(flushAgain ? 3u : 1u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(!flushAgain, quest.NeedsPersistenceSave);
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = f.Context(); Assert.Equal(3u, verify.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LostAcknowledgementsReconcileEitherOriginalOrCommittedRowsWithoutDuplicateCreates(bool afterCommit)
    {
        using var f = new Fixture(options); var quest = f.New(); quest.Single().Progress = 2;
        await using (var context = f.Context(new CommitFault(afterCommit)))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
        }
        Assert.True(quest.NeedsPersistenceSave);
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        Assert.False(quest.NeedsPersistenceSave); using var read = f.Context(); Assert.Single(read.CharacterQuest); Assert.Equal(2u, Assert.Single(read.CharacterQuestObjective).Progress);
    }

    [Fact]
    public async Task SavepointRollbackAndOuterCommitRetainLaterIntentForAFreshReconciliation()
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); quest.Single().Progress = 2;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); await transaction.CreateSavepointAsync("quest_checkpoint");
            quest.Single().Progress = 4; quest.Save(context); await context.SaveChangesAsync(); await transaction.RollbackToSavepointAsync("quest_checkpoint");
            Assert.True(context.HasSavepointRollback); await transaction.CommitAsync();
        }
        Assert.True(quest.NeedsPersistenceSave);
        using (var read = f.Context()) Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = f.Context(); Assert.Equal(4u, verify.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AnotherLoadedOwnerCannotJoinOrOverlapTheUnfinishedAggregate(bool sameContext)
    {
        using var f = new Fixture(options); f.Seed(); var first = f.Load(); var second = f.Load(); first.Single().Progress = 2; second.Single().Progress = 3;
        await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable); first.Save(context);
        if (sameContext)
        {
            Assert.Throws<InvalidOperationException>(() => second.Save(context)); Assert.True(context.IsSaveRejected); await transaction.RollbackAsync();
        }
        else
        {
            using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => second.Save(denied)); Assert.True(denied.IsSaveRejected);
            await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignRootOrObjectiveChangesAreRejectedWithoutOverwritingTheirState(bool root)
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); quest.Single().Progress = 2;
        using (var foreign = f.Context())
        {
            if (root) foreign.CharacterQuest.Single().Timer = 777; else foreign.CharacterQuestObjective.Single().Progress = 4;
            foreign.SaveChanges();
        }
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => quest.Save(denied)); Assert.True(denied.IsSaveRejected);
        await Assert.ThrowsAsync<InvalidOperationException>(() => denied.SaveChangesAsync());
        using var read = f.Context(); if (root) Assert.Equal(777u, read.CharacterQuest.Single().Timer); else Assert.Equal(4u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData("scalar")] [InlineData("original")] [InlineData("detached")] [InlineData("unmodified")] [InlineData("field-unmodified")]
    public async Task LateTrackedChangesPermanentlyRejectTheSharedContextBeforeSql(string mutation)
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); quest.Single().Progress = 2; quest.Single().Timer = 1900;
        using var context = f.Context(); quest.Save(context);
        context.SavingChanges += (_, _) =>
        {
            var entry = context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Single();
            switch (mutation)
            {
                case "scalar": entry.Entity.Progress = 999; break;
                case "original": entry.OriginalValues[nameof(CharacterQuestObjectiveModel.Progress)] = 999u; break;
                case "detached": entry.State = EntityState.Detached; break;
                case "unmodified": entry.State = EntityState.Unchanged; break;
                case "field-unmodified": entry.Property(row => row.Progress).IsModified = false; break;
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task ManagerRetainsDeletionThroughRollbackAndRetiresItOnlyAfterAcknowledgedCommit()
    {
        using var f = new Fixture(options); f.Seed(); var manager = f.Manager(); var quest = Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests())); quest.EnqueueDelete(true);
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync(); Assert.Single(manager.GetActiveQuests()); await transaction.RollbackAsync();
        }
        Assert.Single(manager.GetActiveQuests()); using (var read = f.Context()) { Assert.Single(read.CharacterQuest); Assert.Single(read.CharacterQuestObjective); }
        await using (var retry = f.Context()) { manager.Save(retry); await retry.SaveChangesAsync(); }
        Assert.Empty(manager.GetActiveQuests()); Assert.True(quest.IsPersistenceDeleted);
        using var verify = f.Context(); Assert.Empty(verify.CharacterQuest); Assert.Empty(verify.CharacterQuestObjective);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CanceledDeletionRestoresTheWholeQuestBeforeOrAfterTheCascadeWasFlushed(bool flushDelete)
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load();
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.EnqueueDelete(true); quest.Save(context); if (flushDelete) await context.SaveChangesAsync();
            quest.EnqueueDelete(false); quest.Single().Progress = 4; quest.Save(context); await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        Assert.False(quest.PendingDelete); Assert.False(quest.NeedsPersistenceSave);
        using var read = f.Context(); Assert.Single(read.CharacterQuest); Assert.Equal(4u, Assert.Single(read.CharacterQuestObjective).Progress);
    }

    [Fact]
    public async Task ADeletionCommittedAfterCancellationCanBeRestoredInAFreshUnit()
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load();
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.EnqueueDelete(true); quest.Save(context); await context.SaveChangesAsync(); quest.EnqueueDelete(false); quest.Single().Progress = 3; await transaction.CommitAsync();
        }
        Assert.True(quest.NeedsPersistenceSave); using (var read = f.Context()) Assert.Empty(read.CharacterQuest);
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = f.Context(); Assert.Single(verify.CharacterQuest); Assert.Equal(3u, verify.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task TouchingTheSameValueAfterFlushStillRetainsTheNewMutationRevision()
    {
        using var f = new Fixture(options); var quest = f.New(); var objective = Assert.IsType<QuestObjective>(quest.Single()); objective.Progress = 2;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); objective.Progress = 2; await transaction.CommitAsync();
        }
        Assert.True(objective.NeedsPersistenceSave);
        await using (var retry = f.Context()) { quest.Save(retry); Assert.Equal(0, await retry.SaveChangesAsync()); }
        Assert.False(objective.NeedsPersistenceSave);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExplicitUnitsSupportSaveChangesWithoutAcceptingTrackedOriginals(bool secondFlush)
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); quest.Single().Progress = 2;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
            if (secondFlush) { quest.Single().Progress = 3; quest.Save(context); await context.SaveChangesAsync(acceptAllChangesOnSuccess: false); }
            await transaction.CommitAsync();
        }
        Assert.False(quest.NeedsPersistenceSave); using var read = f.Context(); Assert.Equal(secondFlush ? 3u : 2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DeletingANewQuestAfterStagingCannotLoseTheDeletionWhenItsCreateCommits(bool restageDelete)
    {
        using var f = new Fixture(options); var quest = f.New(); quest.Single().Progress = 2;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync(); Assert.False(quest.PendingCreate);
            quest.EnqueueDelete(true); if (restageDelete) { quest.Save(context); await context.SaveChangesAsync(); }
            await transaction.CommitAsync();
        }
        if (!restageDelete) { await using var retry = f.Context(); quest.Save(retry); await retry.SaveChangesAsync(); }
        Assert.True(quest.IsPersistenceDeleted); using var read = f.Context(); Assert.Empty(read.CharacterQuest); Assert.Empty(read.CharacterQuestObjective);
    }

    [Fact]
    public async Task RestoringAFlushedCascadeAndRollingBackRetainsTheOriginalObjectiveCandidate()
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load();
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.EnqueueDelete(true); quest.Save(context); await context.SaveChangesAsync();
            quest.EnqueueDelete(false); quest.Single().Progress = 4; quest.Save(context); await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var read = f.Context()) Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress);
        await using (var retry = f.Context()) { quest.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = f.Context(); Assert.Equal(4u, verify.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task ADirectObjectiveSaveAlsoBlocksAnotherLoadedParentsDeletion()
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); var other = f.Load(); quest.Single().Progress = 2;
        await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        quest.Single().Save(context); other.EnqueueDelete(true);
        using (var denied = f.Context()) { Assert.Throws<InvalidOperationException>(() => other.Save(denied)); Assert.True(denied.IsSaveRejected); }
        await context.SaveChangesAsync(); await transaction.CommitAsync();
        using var read = f.Context(); Assert.Single(read.CharacterQuest); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task ManagerDoesNotRetireACanceledDeletionAcknowledgedByAnOlderSave()
    {
        using var f = new Fixture(options); f.Seed(); var manager = f.Manager(); var quest = Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests()));
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.EnqueueDelete(true); manager.Save(context); await context.SaveChangesAsync(); quest.EnqueueDelete(false); quest.Single().Progress = 3; await transaction.CommitAsync();
        }
        Assert.Same(quest, Assert.Single(manager.GetActiveQuests()));
        await using (var retry = f.Context()) { manager.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Single(read.CharacterQuest); Assert.Equal(3u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task AnOldDeletionAcknowledgementCannotRetireAReplacementQuestObject()
    {
        using var f = new Fixture(options); f.Seed(); var manager = f.Manager(); var old = Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests())); var replacement = f.New();
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            old.EnqueueDelete(true); manager.Save(context); await context.SaveChangesAsync();
            var active = (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
            active[Fixture.QuestId] = replacement; await transaction.CommitAsync();
        }
        Assert.Same(replacement, Assert.Single(manager.GetActiveQuests()));
    }

    [Fact]
    public async Task AForeignSqlMutationAfterFlushCannotCommitThroughTheOwnedUnit()
    {
        using var f = new Fixture(options); f.Seed(); var quest = f.Load(); quest.Single().Progress = 2;
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            quest.Save(context); await context.SaveChangesAsync();
            await context.CharacterQuestObjective.Where(row => row.Id == 42 && row.QuestId == Fixture.QuestId && row.Index == 0).ExecuteUpdateAsync(set => set.SetProperty(row => row.Progress, 9u));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync()); Assert.True(context.IsSaveRejected); await transaction.RollbackAsync();
        }
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public void PendingDeletionDoesNotContinueTickingItsQuestTimerOrEmitAFalseFailure()
    {
        using var f = new Fixture(options); f.Seed(); using (var context = f.Context()) { context.CharacterQuest.Single().Timer = 1; context.SaveChanges(); }
        var manager = f.Manager(); var quest = Assert.Single(manager.GetActiveQuests()); quest.EnqueueDelete(true); manager.Update(1);
        Assert.Equal(QuestState.Accepted, quest.State); Assert.Equal(1u, quest.Timer);
    }

    [Fact]
    public async Task ResetAndTimerPersistenceReconcileAtTheConfiguredProviderPrecision()
    {
        using var f = new Fixture(options); var quest = f.New(); DateTime reset = new DateTime(2026, 10, 30, 12, 34, 56, DateTimeKind.Utc).AddTicks(9876543); quest.Reset = reset; quest.Timer = 700; quest.Single().Timer = 1800;
        DateTime reference;
        using (var context = f.Context())
        {
            context.CharacterQuest.Add(new() { Id = 42, QuestId = 9002, State = (byte)QuestState.Accepted, Reset = reset }); context.SaveChanges();
            context.ChangeTracker.Clear(); reference = context.CharacterQuest.AsNoTracking().Single(row => row.QuestId == 9002).Reset!.Value;
        }
        await using (var context = f.Context()) { quest.Save(context); await context.SaveChangesAsync(); }
        Assert.False(quest.NeedsPersistenceSave); quest.Flags = QuestStateFlags.Tracked;
        await using (var context = f.Context()) { quest.Save(context); await context.SaveChangesAsync(); }
        using var read = f.Context(); var value = read.CharacterQuest.Single(row => row.QuestId == Fixture.QuestId);
        Assert.Equal(reference, value.Reset!.Value); Assert.Equal(700u, value.Timer); Assert.Equal(1800u, read.CharacterQuestObjective.Single().Timer);
    }

    internal sealed class Fixture : IDisposable
    {
        internal const ushort QuestId = 9001;
        internal readonly SourceFixture Source;
        internal readonly IQuestInfo Info;
        internal readonly IGlobalQuestManager Global;
        internal readonly IScriptManager Scripts;
        internal Fixture(DbContextOptions<CharacterContext> options) : this(new SourceFixture(options)) { }
        internal Fixture(DbContextOptions<CharacterContext> options, bool seedCharacter) : this(new SourceFixture(options, seedCharacter)) { }
        private Fixture(SourceFixture source)
        {
            Source = source; PrepareInventory(Source);
            Info = RecordingDispatchProxy<IQuestInfo>.Create(out var info);
            info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = QuestId });
            info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 })));
            Global = RecordingDispatchProxy<IGlobalQuestManager>.Create(out var global); global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), Info);
            global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestCommunicatorQuestStateTriggers), Array.Empty<ICommunicatorMessage>());
            Scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scripts); scripts.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
        }
        internal CharacterContext Context(params IInterceptor[] interceptors) => interceptors.Length == 0 ? Source.Context() : Source.Intercepted(interceptors);
        internal GameQuest New() => new(Source.Player, Info, globalQuestManager: Global, scriptManager: Scripts, gameTableManager: Source.Tables);
        internal GameQuest Load()
        {
            using var context = Context(); return new(Source.Player, Info, context.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single(), globalQuestManager: Global, scriptManager: Scripts, gameTableManager: Source.Tables);
        }
        internal QuestManager Manager()
        {
            using var context = Context(); var model = new CharacterModel { Id = 42 }; model.Quest.Add(context.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single());
            return new(Source.Player, model, globalQuestManager: Global, scriptManager: Scripts, gameTableManager: Source.Tables);
        }
        internal void Seed()
        {
            using var context = Context(); context.CharacterQuest.Add(new() { Id = 42, QuestId = QuestId, State = (byte)QuestState.Accepted,
                QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = QuestId, Index = 0, Progress = 1, Timer = 2000 } } }); context.SaveChanges();
        }
        public void Dispose() => Source.Dispose();
    }
    private sealed class FailFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new IOException("Owned Quest flush fault.");
    }
    private sealed class SuppressFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
    }
    private sealed class CommitFault(bool afterCommit) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => afterCommit ? ValueTask.FromResult(result) : throw new IOException("Owned Quest before-commit fault.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => afterCommit ? Task.FromException(new IOException("Owned Quest commit acknowledgement fault.")) : Task.CompletedTask;
    }
}
