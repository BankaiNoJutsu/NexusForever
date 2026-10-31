using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeQuestTransactionTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CharacterChallengeQuestTransactionTests() { }
    internal CharacterChallengeQuestTransactionTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task TerminalMedalAndExactQuestGraphChangesCommitAndReplayTogether(uint track)
    {
        using var f = new Fixture(options); await GoldReady(f, track); Seed(f, 9001); Seed(f, 9002); Seed(f, 9003);
        var before = Read(f, 9001); var after = Advance(before);
        var added = Snapshot(9004);
        var changes = new[] { new CharacterChallengeQuestChange(before, after), new(Read(f, 9002), null),
            new(null, added), new(Read(f, 9003), Read(f, 9003)) };
        var command = Gold(f, track).WithQuestChanges(changes);
        var executor = Executor(f);
        var result = await executor.ExecuteAsync(command);
        Assert.True(result.Outcome == CharacterChallengeRewardExecutionOutcome.Committed, result.Error?.ToString());
        Assert.True(result.Changed); Assert.True(CharacterChallengeQuestSnapshot.Same(after, Read(f, 9001)));
        Assert.Null(Read(f, 9002)); Assert.True(CharacterChallengeQuestSnapshot.Same(added, Read(f, 9004)));
        var replay = await executor.ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, replay.Outcome); Assert.False(replay.Changed);
        Assert.False(Assert.IsType<CharacterChallengeMedalState>(replay.Operation).Applied);
        using var read = f.Context(); Assert.Equal(300u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(3, read.CharacterQuest.Count()); Assert.Equal(6, read.CharacterQuestObjective.Count()); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task ObjectiveCreationAndRemovalShareAnUnchangedParentAndPreserveOrdinalZero()
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001);
        var after = new CharacterChallengeQuestSnapshot(before.Quest,
            [before.Objectives[0], new(42, 9001, 2, 4, 700)]);
        var command = Gold(f, 16).WithQuestChanges([new(before, after)]);
        var result = await Executor(f).ExecuteAsync(command);
        Assert.True(result.Outcome == CharacterChallengeRewardExecutionOutcome.Committed, result.Error?.ToString());
        Assert.True(CharacterChallengeQuestSnapshot.Same(after, Read(f, 9001)));
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
    }

    [Fact]
    public async Task SuppressedObjectiveModificationCannotCommitOnlyTheMedalAndQuestRoot()
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            command.Stage(f.Repository(context), null);
            context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Single(row => row.Entity.Index == 0)
                .Property(row => row.Progress).IsModified = false;
            // DetectChanges may restore the modified flag. Disable it to model
            // an actual suppressed write while retaining the captured value.
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                { await context.SaveChangesAsync(); await unit.CommitAsync(); });
        }
        Assert.True(CharacterChallengeQuestSnapshot.Same(before, Read(f, 9001)));
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task PositiveTierCompletionSharesQuestProgressWithoutSynthesizingAnotherMedal()
    {
        using var f = new Fixture(options); await f.Begin(16); await f.Medal(16, 0, new(1, 1)); Seed(f, 9001);
        var before = f.State(16); var quest = Read(f, 9001);
        var command = CharacterChallengeRewardCommand.Finish(before, before with { Activated = false, Completions = 1, ActiveSeconds = 0 })
            .WithQuestChanges([new(quest, Advance(quest))]);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
        using var read = f.Context(); Assert.Equal(100u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Single(read.CharacterChallengeMedalReceipt); Assert.True(read.CharacterChallengeAttempt.Single().Terminal);
        Assert.Equal(2u, read.CharacterQuestObjective.Single(row => row.Index == 0).Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommitFaultsResolveTheRewardAndQuestGraphsAsOneUnit(bool after)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001); Seed(f, 9002);
        var before = Read(f, 9001); var finished = Advance(before);
        var command = Gold(f, 16).WithQuestChanges([new(before, finished), new(Read(f, 9002), null), new(null, Snapshot(9003))]);
        var fault = new CommitFault(after);
        var result = await new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables).ExecuteAsync(command);
        Assert.True(result.Outcome == (after ? CharacterChallengeRewardExecutionOutcome.Committed : CharacterChallengeRewardExecutionOutcome.Failed), result.Error?.ToString());
        Assert.True(result.Recovered); Assert.True(result.OriginalUnitEnded);
        Assert.True(CharacterChallengeQuestSnapshot.Same(after ? finished : before, Read(f, 9001)));
        Assert.Equal(after, Read(f, 9002) == null); Assert.Equal(after, Read(f, 9003) != null);
        using (var read = f.Context()) Assert.Equal(after ? 3 : 2, read.CharacterChallengeMedalReceipt.Count());
        if (!after) Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
        using var final = f.Context(); Assert.Equal(300u, final.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(1u, final.CharacterChallenge.Single().CompletionCount);
    }

    [Theory]
    [InlineData("state")] [InlineData("progress")] [InlineData("missing-root")]
    [InlineData("missing-leaf")] [InlineData("extra-leaf")]
    public async Task ForeignBaselineChangesRejectTheEntireRewardWithoutOverwritingQuests(string change)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        Mutate(f, change);
        var foreign = Read(f, 9001);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, (await Executor(f).ExecuteAsync(command)).Outcome);
        Assert.True(CharacterChallengeQuestSnapshot.Same(foreign, Read(f, 9001)));
        using var read = f.Context(); Assert.Equal(200u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Theory]
    [InlineData("state")] [InlineData("progress")] [InlineData("missing-root")]
    [InlineData("missing-leaf")] [InlineData("extra-leaf")]
    public async Task LateSqlChangesInvalidateTheCompleteGraphAndRollBackTheMedal(string change)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            command.Stage(f.Repository(context), null); await context.SaveChangesAsync();
            Mutate(context, change);
            await Assert.ThrowsAsync<InvalidOperationException>(() => unit.CommitAsync());
            Assert.True(context.IsSaveRejected);
        }
        Assert.True(CharacterChallengeQuestSnapshot.Same(before, Read(f, 9001)));
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Theory]
    [InlineData("root")] [InlineData("leaf")] [InlineData("original")] [InlineData("detached")]
    public async Task LateTrackerChangesRejectTheWholeSharedContext(string change)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            command.Stage(f.Repository(context), null);
            var root = context.ChangeTracker.Entries<CharacterQuestModel>().Single();
            var leaf = context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Single(row => row.Entity.Index == 0);
            if (change == "root") root.Entity.Flags++;
            if (change == "leaf") leaf.Entity.Progress++;
            if (change == "original") leaf.OriginalValues[nameof(CharacterQuestObjectiveModel.Progress)] = 999u;
            if (change == "detached") root.State = EntityState.Detached;
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
        }
        Assert.True(CharacterChallengeQuestSnapshot.Same(before, Read(f, 9001)));
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LostAcknowledgementFollowedByQuestChangeRequiresReloadAndCannotReapplyProgress(bool after)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        // A before-commit failure is followed by a foreign write only after the
        // old unit has ended, using the first recovery-context factory call.
        var fault = new CommitFault(after); int calls = 0;
        CharacterContext Context()
        {
            if (Interlocked.Increment(ref calls) == 3) Mutate(f, "progress");
            return f.Intercepted(fault);
        }
        var result = await new CharacterChallengeRewardExecutor(Context, f.Catalog, f.Tables).ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Conflict, result.Outcome);
        using var read = f.Context(); Assert.Equal(99u, read.CharacterQuestObjective.Single(row => row.Index == 0).Progress);
        Assert.Equal(after ? 3 : 2, read.CharacterChallengeMedalReceipt.Count());
        Assert.Equal(after ? 300u : 200u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task ReplayingAnOldCompletionCannotCreditAReacceptedQuest()
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16).WithQuestChanges([new(before, Advance(before))]);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
        using (var changed = f.Context()) { changed.CharacterQuest.ExecuteDelete(); changed.SaveChanges(); }
        Seed(f, 9001);
        var result = await Executor(f).ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome);
        Assert.True(CharacterChallengeQuestSnapshot.Same(before, Read(f, 9001)));
        using var read = f.Context(); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task ResetPrecisionUsesTheSameProviderConversionAsADirectEfWrite()
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001); Seed(f, 9002);
        var instant = new DateTime(2026, 10, 30, 12, 34, 56, DateTimeKind.Utc).AddTicks(9876543);
        using (var direct = f.Context()) { direct.CharacterQuest.Single(row => row.QuestId == 9002).Reset = instant; direct.SaveChanges(); }
        var before = Read(f, 9001); var after = new CharacterChallengeQuestSnapshot(before.Quest with { Reset = instant }, before.Objectives);
        var command = Gold(f, 16).WithQuestChanges([new(before, after)]);
        var fault = new CommitFault(true);
        var result = await new CharacterChallengeRewardExecutor(() => f.Intercepted(fault), f.Catalog, f.Tables).ExecuteAsync(command);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); Assert.True(result.Recovered);
        Assert.Equal(Read(f, 9002).Quest.Reset, Read(f, 9001).Quest.Reset);
    }

    [Fact]
    public async Task CommandFreezesScalarGraphsAndCollectionsBeforeTheWorkerRuns()
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var root = before.Quest.ToModel(); root.State = 3;
        var leaves = before.Objectives.ToArray(); leaves[0] = leaves[0] with { Progress = 2 };
        var after = new CharacterChallengeQuestSnapshot(CharacterChallengeQuestState.FromModel(root), leaves);
        var changes = new List<CharacterChallengeQuestChange> { new(before, after) };
        var command = Gold(f, 16).WithQuestChanges(changes);
        root.State = 99; leaves[0] = leaves[0] with { Progress = 99 }; changes.Clear();
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeQuestChange>)command.QuestChanges).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeQuestObjectiveState>)after.Objectives).Clear());
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await Executor(f).ExecuteAsync(command)).Outcome);
        Assert.True(CharacterChallengeQuestSnapshot.Same(after, Read(f, 9001)));
    }

    [Theory]
    [InlineData("foreign")] [InlineData("duplicate")] [InlineData("active")]
    [InlineData("no-completion")] [InlineData("wrong-operation")]
    public async Task InvalidParticipantsCannotAttachToARewardCommand(string invalid)
    {
        using var f = new Fixture(options); await GoldReady(f, 16); Seed(f, 9001);
        var before = Read(f, 9001); var command = Gold(f, 16);
        CharacterChallengeQuestChange[] changes = [new(before, Advance(before))];
        if (invalid == "foreign") changes = [new(null, new(before.Quest with { CharacterId = 43 }, []))];
        if (invalid == "duplicate") changes = [changes[0], changes[0]];
        if (invalid is "active" or "no-completion")
        {
            var state = f.State(16);
            command = CharacterChallengeRewardCommand.Finish(state, state with { Activated = invalid == "active", Completions = invalid == "active" ? 1u : 0u });
        }
        if (invalid == "wrong-operation") command = CharacterChallengeRewardCommand.Load(42);
        Assert.Throws<ArgumentException>(() => command.WithQuestChanges(changes));
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Theory]
    [InlineData("duplicate-leaf")] [InlineData("foreign-leaf")] [InlineData("identity")] [InlineData("empty")]
    public void InvalidSnapshotsRejectAmbiguousGraphs(string invalid)
    {
        using var f = new Fixture(options); Seed(f, 9001); var before = Read(f, 9001);
        if (invalid == "duplicate-leaf") Assert.Throws<ArgumentException>(() => new CharacterChallengeQuestSnapshot(before.Quest, [before.Objectives[0], before.Objectives[0]]));
        if (invalid == "foreign-leaf") Assert.Throws<ArgumentException>(() => new CharacterChallengeQuestSnapshot(before.Quest, [before.Objectives[0] with { QuestId = 9002 }]));
        if (invalid == "identity") Assert.Throws<ArgumentException>(() => new CharacterChallengeQuestChange(before, Snapshot(9002)));
        if (invalid == "empty") Assert.Throws<ArgumentException>(() => new CharacterChallengeQuestChange(null, null));
    }

    private static CharacterChallengeRewardExecutor Executor(Fixture f) => new(f.Context, f.Catalog, f.Tables);
    private static async Task GoldReady(Fixture f, uint track)
        { await f.Begin(track); await f.Medal(track, 0, new(1, 1)); await f.Medal(track, 1, new(1, 2)); }
    private static CharacterChallengeRewardCommand Gold(Fixture f, uint track)
    {
        var before = f.State(track);
        return CharacterChallengeRewardCommand.Credit(before, before with { Activated = false, Tier = 2, LastRewardTier = 2, Completions = 1, ActiveSeconds = 0 },
            2, new(1, 3), DateTime.UtcNow);
    }
    private static CharacterChallengeQuestSnapshot Snapshot(ushort id) => new(new(42, id, 1, 0, 5000, null),
        [new(42, id, 0, 1, 1000), new(42, id, 1, 0, null)]);
    private static CharacterChallengeQuestSnapshot Advance(CharacterChallengeQuestSnapshot before) => new(
        before.Quest with { State = 3, Flags = 3, Timer = 0 }, before.Objectives.Select(row => row.Index == 0 ? row with { Progress = 2, Timer = 0 } : row));
    private static void Seed(Fixture f, ushort id)
    {
        var snapshot = Snapshot(id); using var context = f.Context();
        context.CharacterQuest.Add(snapshot.Quest.ToModel());
        context.CharacterQuestObjective.AddRange(snapshot.Objectives.Select(row => row.ToModel())); context.SaveChanges();
    }
    private static CharacterChallengeQuestSnapshot Read(Fixture f, ushort id)
    {
        using var context = f.Context();
        var root = context.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.QuestId == id);
        return root == null ? null : new(CharacterChallengeQuestState.FromModel(root), context.CharacterQuestObjective.AsNoTracking()
            .Where(row => row.QuestId == id).AsEnumerable().Select(CharacterChallengeQuestObjectiveState.FromModel));
    }
    private static void Mutate(Fixture f, string change) { using var context = f.Context(); Mutate(context, change); }
    private static void Mutate(CharacterContext context, string change)
    {
        if (change == "state") context.CharacterQuest.Where(row => row.QuestId == 9001).ExecuteUpdate(set => set.SetProperty(row => row.Flags, (byte)99));
        if (change == "progress") context.CharacterQuestObjective.Where(row => row.QuestId == 9001 && row.Index == 0).ExecuteUpdate(set => set.SetProperty(row => row.Progress, 99u));
        if (change == "missing-root") context.CharacterQuest.Where(row => row.QuestId == 9001).ExecuteDelete();
        if (change == "missing-leaf") context.CharacterQuestObjective.Where(row => row.QuestId == 9001 && row.Index == 0).ExecuteDelete();
        if (change == "extra-leaf") context.Database.ExecuteSqlRaw("INSERT INTO character_quest_objective (id, questId, `index`, progress, timer) VALUES (42, 9001, 99, 0, NULL)");
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int remaining = 1;
        private void Fail() { if (Interlocked.Exchange(ref remaining, 0) == 1) throw new IOException("Owned shared Quest/reward commit acknowledgement fault."); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
            { if (!after) Fail(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            { if (after) Fail(); return Task.CompletedTask; }
    }
}
