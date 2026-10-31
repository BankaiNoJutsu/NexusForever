using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using DatabaseFixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractCompletionRepositoryTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractCompletionRepositoryTests() { }
    internal ContractCompletionRepositoryTests(DbContextOptions<CharacterContext> options) => this.options = options;
    public static IEnumerable<object[]> ActualSources() => ContractBuild16042Fixture.Quests().Where(q => q.PeriodicQuestGroupId != 0).Select(q => new object[] { q.Id });

    [Fact]
    public async Task StandalonePointCompletionAndDuplicateObservationRetainAnAlreadySelectedCycle()
    {
        using var f = new Fixture(options); var definition = f.Catalog.GetTrack(f.Source.TrackId);
        var selection = ContractRewardSelection.Capture(f.Catalog, definition.Id, 7, f.Token,
            definition.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Append(definition.PrerequisiteId), _ => 0);
        using (var seed = f.Context())
        {
            seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = definition.Id, CycleId = 7, Revision = 3, Points = 899, SelectionInitialized = true });
            foreach (var slot in definition.Slots) seed.CharacterContractRewardSlot.Add(new()
            { Id = 42, TrackId = definition.Id, CycleId = 7, Ordinal = slot.Ordinal, RequiredPoints = slot.RequiredPoints, RewardRowId = selection.Rewards[slot.Ordinal], SelectionJson = selection.Json });
            seed.SaveChanges();
        }
        var command = f.Command(); var first = await f.Execute(command); var duplicate = await f.Execute(command);
        Assert.True(first.Track.SelectionInitialized); Assert.Equal(selection.Rewards, first.Track.Slots.Select(slot => slot.RewardRowId));
        Assert.True(ContractRewardSnapshot.Same(first.Track, duplicate.Track)); Assert.False(duplicate.Applied);
        using var read = f.Context(); Assert.Equal(definition.Slots.Count, read.CharacterContractRewardSlot.Count()); Assert.Single(read.CharacterContractCompletion);
    }

    [Theory]
    [MemberData(nameof(ActualSources))]
    public async Task EveryActualSourceCommitsOneReceiptPointIncrementAndTerminalQuestGraphTogether(uint questId)
    {
        using var f = new Fixture(options, questId); var command = f.Command();
        var first = await f.Execute(command); Assert.True(first.Applied);
        Assert.Equal(f.Source.BasePoints, first.Track.Points); Assert.Equal(1ul, first.Track.Revision);
        Assert.Equal(f.Source.TrackId, first.Receipt.TrackId); Assert.Equal(f.Token, first.Receipt.AcceptanceId);
        var replay = await f.Execute(command); Assert.False(replay.Applied); Assert.Equal(first.Track, replay.Track);
        Assert.True(CharacterChallengeQuestSnapshot.Same(command.Changes.Single().After, f.Read((ushort)questId)));
        using var read = f.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Single(read.CharacterContractRewardTrack);
        Assert.Empty(read.CharacterChallengeMedalReceipt); Assert.Empty(read.CharacterChallengeRewardTrack); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task IndependentLaterAcceptancesCreditSeparatelyAndAnOldReplayCannotRestoreTheOldQuestOrPoints()
    {
        using var f = new Fixture(options); var first = f.Command(); await f.Execute(first);
        string next = Guid.NewGuid().ToString("N");
        using (var context = f.Context())
        {
            var row = context.CharacterQuest.Single(); row.State = (byte)QuestState.Achieved; row.ContractAcceptanceId = next; await context.SaveChangesAsync();
        }
        var before = f.Read(f.Source.QuestId); var second = f.Command(before, next);
        await f.Execute(second); var replay = await f.Execute(first);
        Assert.False(replay.Applied); Assert.Equal(f.Source.BasePoints * 2, replay.Track.Points); Assert.Equal(2ul, replay.Track.Revision);
        Assert.Equal(next, f.Read(f.Source.QuestId).Quest.ContractAcceptanceId);
        Assert.Equal(next, replay.Quests[f.Source.QuestId].Quest.ContractAcceptanceId);
        using var read = f.Context(); Assert.Equal(2, read.CharacterContractCompletion.Count());
    }

    [Fact]
    public async Task CompleteObjectiveGraphsShareCreationDeletionAndOrdinalZeroWithTheReceipt()
    {
        using var f = new Fixture(options); f.Seed(Snapshot(9001)); f.Seed(Snapshot(9002));
        var added = Snapshot(9003); var ordinary = f.Read(9001);
        var changed = new CharacterChallengeQuestSnapshot(ordinary.Quest, [ordinary.Objectives[0] with { Progress = 9 }, new(42, 9001, 2, 4, 700)]);
        var command = f.Command(extra: [new(ordinary, changed), new(f.Read(9002), null), new(null, added)]);
        await f.Execute(command); Assert.True(CharacterChallengeQuestSnapshot.Same(changed, f.Read(9001)));
        Assert.Null(f.Read(9002)); Assert.True(CharacterChallengeQuestSnapshot.Same(added, f.Read(9003)));
        Assert.False((await f.Execute(command)).Applied);
    }

    [Theory]
    [InlineData("root")] [InlineData("objective")] [InlineData("extra-objective")]
    public async Task ForeignBaselineChangesCannotCommitAReceiptWithoutItsExactQuestGraph(string changed)
    {
        using var f = new Fixture(options); var command = f.Command();
        using (var foreign = f.Context())
        {
            if (changed == "root") foreign.CharacterQuest.Single().Timer = 999;
            else if (changed == "objective") foreign.CharacterQuestObjective.Single(row => row.Index == 0).Progress = 99;
            else foreign.CharacterQuestObjective.Add(new() { Id = 42, QuestId = f.Source.QuestId, Index = 3, Progress = 1 });
            await foreign.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(command)); f.AssertNoCredit();
    }

    [Theory]
    [InlineData("receipt")] [InlineData("track")] [InlineData("root")] [InlineData("objective")]
    [InlineData("detach")] [InlineData("original")]
    public async Task CapturedTargetsAndOwnershipCannotBeChangedBeforeFlush(string changed)
    {
        using var f = new Fixture(options);
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            new ContractCompletionRepository(context, 42, f.Catalog).Complete(f.Command());
            switch (changed)
            {
                case "receipt": context.ChangeTracker.Entries<CharacterContractCompletionModel>().Single().Entity.Points++; break;
                case "track": context.ChangeTracker.Entries<CharacterContractRewardTrackModel>().Single().Entity.Points++; break;
                case "root": context.ChangeTracker.Entries<CharacterQuestModel>().Single().Entity.ContractAcceptanceId = Guid.NewGuid().ToString("N"); break;
                case "objective": context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().First().Entity.Progress++; break;
                case "detach": context.ChangeTracker.Entries<CharacterContractCompletionModel>().Single().State = EntityState.Detached; break;
                case "original": context.ChangeTracker.Entries<CharacterQuestModel>().Single().Property(row => row.ContractAcceptanceId).OriginalValue = Guid.NewGuid().ToString("N"); break;
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        f.AssertNoCredit(); Assert.Equal((byte)QuestState.Achieved, f.Read(f.Source.QuestId).Quest.State);
    }

    [Theory]
    [InlineData("receipt")] [InlineData("track")] [InlineData("root")] [InlineData("objective")]
    public async Task RawSqlTamperingAfterFlushRollsBackTheWholeCompletionAtTheCommitBoundary(string changed)
    {
        using var f = new Fixture(options);
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            new ContractCompletionRepository(context, 42, f.Catalog).Complete(f.Command()); await context.SaveChangesAsync();
            string sql = changed switch
            {
                "receipt" => "UPDATE character_contract_completion SET points=points+1 WHERE id=42",
                "track" => "UPDATE character_contract_reward_track SET points=points+1 WHERE id=42",
                "root" => "UPDATE character_quest SET timer=999 WHERE id=42",
                _ => "UPDATE character_quest_objective SET progress=99 WHERE id=42"
            };
            await context.Database.ExecuteSqlRawAsync(sql);
            await Assert.ThrowsAsync<InvalidOperationException>(() => unit.CommitAsync());
        }
        f.AssertNoCredit();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RollbackAndFlushFailureLeaveNoPartialReceiptAndRetryUsesTheSameIdentity(bool failFlush)
    {
        using var f = new Fixture(options); var command = f.Command();
        await using (var context = failFlush ? f.Context(new FailFlush()) : f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            new ContractCompletionRepository(context, 42, f.Catalog).Complete(command);
            if (failFlush) await Assert.ThrowsAsync<IOException>(() => context.SaveChangesAsync());
            else { await context.SaveChangesAsync(); await unit.RollbackAsync(); }
        }
        f.AssertNoCredit(); Assert.True((await f.Execute(command)).Applied); Assert.False((await f.Execute(command)).Applied);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OverflowCannotLeaveATerminalQuestOrReceipt(bool revision)
    {
        using var f = new Fixture(options);
        using (var context = f.Context())
        {
            context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = f.Source.TrackId,
                Points = revision ? 0u : uint.MaxValue, Revision = revision ? ulong.MaxValue : 0ul }); await context.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<OverflowException>(() => f.Execute(f.Command()));
        using var read = f.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Equal((byte)QuestState.Achieved, read.CharacterQuest.Single().State);
    }

    [Theory]
    [InlineData("points")] [InlineData("evaluation")] [InlineData("choice")] [InlineData("graph")]
    public async Task TheSameAcceptanceCannotBeReusedForADifferentFrozenCompletion(string change)
    {
        using var f = new Fixture(options); var first = f.Command(); await f.Execute(first);
        var changes = first.Changes.ToArray();
        if (change == "graph") changes[0] = new(changes[0].Before, new(changes[0].After.Quest with { Timer = 555 }, changes[0].After.Objectives));
        var conflicting = new ContractCompletionCommand(42, f.Token, f.Source, change == "points" ? first.Points + 1 : first.Points,
            change == "evaluation" ? "different evaluation" : first.Evaluation, change == "choice" ? (ushort)2 : (ushort)0, DateTime.UtcNow, changes);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(conflicting));
        using var read = f.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Equal(first.Points, read.CharacterContractRewardTrack.Single().Points);
    }

    [Fact]
    public async Task NoTransactionForeignContextAndUnrelatedEnrollmentAreRejected()
    {
        using var f = new Fixture(options);
        using (var noUnit = f.Context()) { Assert.Throws<InvalidOperationException>(() => new ContractCompletionRepository(noUnit, 42, f.Catalog)); Assert.True(noUnit.IsSaveRejected); }
        await using (var context = f.Context())
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            new ContractCompletionRepository(context, 42, f.Catalog).Complete(f.Command());
            context.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 99 });
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        f.AssertNoCredit();
    }

    [Theory]
    [InlineData("identity")] [InlineData("terminal")] [InlineData("already-terminal")] [InlineData("foreign")]
    [InlineData("duplicate")] [InlineData("evaluation")] [InlineData("time")] [InlineData("prerequisite")]
    public void InvalidCompletionInputsNeverCreateReceiptAuthority(string invalid)
    {
        using var f = new Fixture(options); var before = f.Read(f.Source.QuestId);
        var after = new CharacterChallengeQuestSnapshot(before.Quest with { State = (byte)QuestState.Completed }, before.Objectives);
        var source = invalid == "prerequisite" ? f.Source with { TrackPrerequisiteId = 123 } : f.Source;
        if (invalid == "terminal") after = before;
        if (invalid == "already-terminal") before = after;
        var changes = new List<CharacterChallengeQuestChange> { new(before, after) }; if (invalid == "duplicate") changes.Add(changes[0]);
        Assert.Throws<ArgumentException>(() => new ContractCompletionCommand(invalid == "foreign" ? 43ul : 42ul,
            invalid == "identity" ? new string('0', 32) : f.Token, source, 50, invalid == "evaluation" ? "" : "fixture-evaluated-base", 0,
            invalid == "time" ? DateTime.Now : DateTime.UtcNow, changes)); f.AssertNoCredit();
    }

    private static CharacterChallengeQuestSnapshot Snapshot(ushort id, string token = null)
        => new(new(42, id, (byte)QuestState.Achieved, 1, 200, null, token), [new(42, id, 0, 5, 250), new(42, id, 1, 1, null)]);

    internal sealed class Fixture : IDisposable
    {
        private readonly DatabaseFixture database;
        internal readonly string Token = Guid.NewGuid().ToString("N");
        internal readonly ContractRewardSource Source;
        internal readonly ContractRewardCatalog Catalog;
        internal Fixture(DbContextOptions<CharacterContext> options, uint questId = 9880)
        {
            database = new(options);
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            void Set<T>(string property, params T[] rows) where T : class, new() => proxy.SetProperty(property, PlayerBindPointTests.Table(rows));
            var quests = ContractBuild16042Fixture.Quests(); var rewards = ContractBuild16042Fixture.Rewards(); var trackRewards = LoyaltyCatalogBuild16042Fixture.Rewards();
            Set(nameof(IGameTableManager.Quest2), quests); Set(nameof(IGameTableManager.QuestObjective), ContractBuild16042Fixture.Objectives());
            Set(nameof(IGameTableManager.PeriodicQuestGroup), ContractBuild16042Fixture.Groups()); Set(nameof(IGameTableManager.Quest2Reward), rewards);
            Set(nameof(IGameTableManager.GameFormula), ContractBuild16042Fixture.Formula()); Set(nameof(IGameTableManager.RewardTrack), LoyaltyCatalogBuild16042Fixture.Tracks());
            Set(nameof(IGameTableManager.RewardTrackRewards), trackRewards);
            uint[] ids = rewards.Where(r => r.Quest2RewardTypeId == 1).Select(r => r.ObjectId)
                .Concat(trackRewards.Where(r => r.RewardTrackId is 4 or 5).SelectMany(r => new[] {
                    (r.RewardTrackRewardTypeEnum00, r.RewardChoiceId00), (r.RewardTrackRewardTypeEnum01, r.RewardChoiceId01), (r.RewardTrackRewardTypeEnum02, r.RewardChoiceId02) })
                .Where(r => r.Item1 == 0 && r.Item2 != 0).Select(r => r.Item2)).Distinct().ToArray();
            Set(nameof(IGameTableManager.Item), ids.Select(id => new Item2Entry { Id = id }).ToArray());
            Set(nameof(IGameTableManager.AccountItem), new AccountItemEntry { Id = 2485 }, new AccountItemEntry { Id = 2486 });
            Set(nameof(IGameTableManager.CurrencyType), new CurrencyTypeEntry { Id = 1 });
            var info = RecordingDispatchProxy<IQuestInfo>.Create(out var ip); ip.SetProperty(nameof(IQuestInfo.Entry), quests.Single(q => q.Id == questId)); ip.SetMethodReturn(nameof(IQuestInfo.IsContract), true);
            Assert.True(new ContractManager(tables).TryGetRewardSource(info, out Source)); Catalog = new(tables); Seed(Snapshot((ushort)questId, Token));
        }
        internal CharacterContext Context(params IInterceptor[] interceptors) => interceptors.Length == 0 ? database.Context() : database.Intercepted(interceptors);
        internal CharacterChallengeQuestSnapshot Read(ushort id)
        {
            using var read = Context(); var row = read.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.Id == 42 && row.QuestId == id);
            return row == null ? null : new(CharacterChallengeQuestState.FromModel(row), read.CharacterQuestObjective.AsNoTracking().Where(row => row.Id == 42 && row.QuestId == id).AsEnumerable().Select(CharacterChallengeQuestObjectiveState.FromModel));
        }
        internal void Seed(CharacterChallengeQuestSnapshot snapshot)
        {
            using var context = Context(); context.CharacterQuest.Add(snapshot.Quest.ToModel()); foreach (var row in snapshot.Objectives) context.CharacterQuestObjective.Add(row.ToModel()); context.SaveChanges();
        }
        internal ContractCompletionCommand Command(CharacterChallengeQuestSnapshot before = null, string token = null, IEnumerable<CharacterChallengeQuestChange> extra = null)
        {
            before ??= Read(Source.QuestId); var after = new CharacterChallengeQuestSnapshot(before.Quest with { State = (byte)QuestState.Completed }, before.Objectives);
            return new(42, token ?? Token, Source, Source.BasePoints, "fixture-evaluated-base", 0, new DateTime(2026, 10, 31, 1, 2, 3, DateTimeKind.Utc),
                new[] { new CharacterChallengeQuestChange(before, after) }.Concat(extra ?? []));
        }
        internal async Task<ContractCompletionStage> Execute(ContractCompletionCommand command)
        {
            await using var context = Context(); await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = new ContractCompletionRepository(context, 42, Catalog).Complete(command); await context.SaveChangesAsync(); await unit.CommitAsync(); return result;
        }
        internal void AssertNoCredit() { using var read = Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterContractRewardTrack); }
        public void Dispose() => database.Dispose();
    }
    private sealed class FailFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new IOException("Contract flush fault.");
    }
}
