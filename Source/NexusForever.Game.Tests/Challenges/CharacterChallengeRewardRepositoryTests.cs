using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeRewardRepositoryTests
{
    private readonly DbContextOptions<CharacterContext> providerOptions;
    public CharacterChallengeRewardRepositoryTests() { }
    internal CharacterChallengeRewardRepositoryTests(DbContextOptions<CharacterContext> options) => providerOptions = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task AllSixteenActualCatalogsPersistCanonicalOrdinalsWithoutResamplingOnReplay(uint track)
    {
        using var f = new Fixture(providerOptions);
        var attempt = await f.Begin(track);
        Assert.Equal(CharacterChallengeRewardMode.CharacterTrack, attempt.Mode);
        int samples = f.Samples;
        using (var context = f.Context())
        {
            var ledger = context.CharacterChallengeRewardTrack.Single();
            var slots = context.CharacterChallengeRewardSlot.OrderBy(row => row.Ordinal).ToArray();
            var definition = f.Catalog.GetTrack(track);
            Assert.Equal(definition.Slots.Count, slots.Length);
            Assert.Equal(definition.Slots.Select(slot => slot.Cost), slots.Select(slot => slot.RequiredPoints));
            Assert.Equal(1ul, ledger.Revision); Assert.True(ledger.Active); Assert.Equal(0u, ledger.Points);
            Assert.All(slots, slot => Assert.Contains(f.Catalog.GetTrack(track).Slots[slot.Ordinal].Candidates, row => row.Id == slot.RewardRowId));
        }
        var replay = await f.Execute(repo => repo.BeginAttempt(null, f.Activated(track), attempt.AttemptId, true));
        Assert.Equal(attempt, replay); Assert.Equal(samples, f.Samples);
    }

    [Theory]
    [InlineData(29u)] [InlineData(30u)] [InlineData(31u)] [InlineData(32u)] [InlineData(34u)]
    public async Task EmptyCatalogsKeepAnExplicitLegacyAttemptWithoutInventingTrackState(uint track)
    {
        using var f = new Fixture(providerOptions);
        Assert.Equal(CharacterChallengeRewardMode.LegacyDirect, (await f.Begin(track)).Mode);
        using var context = f.Context();
        Assert.Empty(context.CharacterChallengeRewardTrack); Assert.Empty(context.CharacterChallengeRewardSlot);
        Assert.Empty(context.CharacterChallengeMedalReceipt); Assert.Empty(context.Item);
    }

    [Fact]
    public async Task MedalsUseFrozenIndependentIncrementsAndCommittedReplayCannotRecredit()
    {
        using var f = new Fixture(providerOptions);
        var attempt = await f.Begin(16);
        var before = f.State(16);
        var bronze = await f.Medal(16, 0, new(1.239f, 17));
        await f.Medal(16, 1, new(1.5f, 18));
        var gold = await f.Medal(16, 2, new(2, 19));
        Assert.Equal(473u, gold.Track.Points); Assert.True(gold.Attempt.Terminal); Assert.Equal(7u, gold.Attempt.EarnedMedalMask);
        var replay = await f.Execute(repo => repo.CreditMedal(attempt.AttemptId, f.Challenge(16), 0,
            new(100, 900), before, before with { Tier = 1, LastRewardTier = 0 }, DateTime.UtcNow));
        Assert.False(replay.Applied); Assert.Equal(123u, replay.Points); Assert.Equal(17, replay.ModifierRevision);
        Assert.Equal(473u, replay.Track.Points);
        using var context = f.Context(); Assert.Equal(3, context.CharacterChallengeMedalReceipt.Count());
        Assert.Equal(1u, context.CharacterChallenge.Single().CompletionCount); Assert.Empty(context.Item);
        Assert.NotEqual(attempt.AttemptId, (await f.Begin(16)).AttemptId);
    }

    [Fact]
    public async Task PositiveTierTimeoutRetainsBronzeWithoutSynthesizingAnotherMedal()
    {
        using var f = new Fixture(providerOptions);
        var attempt = await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var before = f.State(16);
        var terminal = await f.Execute(repo => repo.FinishAttempt(attempt.AttemptId, f.Challenge(16), before,
            before with { Activated = false, ActiveSeconds = 0, Completions = 1 }));
        Assert.True(terminal.Terminal); Assert.Equal(1u, terminal.EarnedMedalMask);
        using var context = f.Context(); Assert.Single(context.CharacterChallengeMedalReceipt);
        Assert.Equal(100u, context.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task RolledBackActivationLeavesNoMarkerOrSelectionAndCanRetryFresh()
    {
        using var f = new Fixture(providerOptions);
        string id = Guid.NewGuid().ToString("N");
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            f.Repository(context).BeginAttempt(null, f.Activated(24), id, true);
            await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var read = f.Context()) { Assert.Empty(read.CharacterChallenge); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardSlot); }
        await f.Execute(repo => repo.BeginAttempt(null, f.Activated(24), id, true));
        using var result = f.Context(); Assert.Equal(id, result.CharacterChallenge.Single().ActiveAttemptId);
    }

    [Fact]
    public async Task SelectionDoesNotRedirectAnotherChallengesEarnedPoints()
    {
        using var f = new Fixture(providerOptions);
        await f.Begin(15); await f.Begin(16);
        Dictionary<uint, ulong> Versions() { using var read = f.Context(); return read.CharacterChallengeRewardTrack.ToDictionary(row => row.TrackId, row => row.Revision); }
        var versions = Versions();
        Assert.True((await f.Execute(repo => repo.ChooseTrack(16, versions))).Active);
        var earned = await f.Medal(15, 0, new(1, 1));
        Assert.Equal(15u, earned.Track.TrackId); Assert.Equal(100u, earned.Track.Points); Assert.False(earned.Track.Active);
        using var read = f.Context(); Assert.Equal(16u, read.CharacterChallengeRewardTrack.Single(row => row.Active).TrackId);
        Assert.Equal(0u, read.CharacterChallengeRewardTrack.Single(row => row.TrackId == 16).Points);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.ChooseTrack(15, versions)));
    }

    [Fact]
    public async Task NewSelectedTrackKeepsItsInsertWhileChangingTheActiveSelection()
    {
        using var f = new Fixture(providerOptions); await f.Begin(15);
        var selected = await f.Execute(repo => repo.ChooseTrack(16, new Dictionary<uint, ulong> { [15] = 1 }));
        Assert.True(selected.Active); Assert.Equal(3, selected.Slots.Count);
        using var read = f.Context(); Assert.Equal(2, read.CharacterChallengeRewardTrack.Count());
        Assert.Single(read.CharacterChallengeRewardTrack.Where(row => row.Active)); Assert.Equal(7, read.CharacterChallengeRewardSlot.Count());
    }

    [Fact]
    public async Task FinalFirstClaimIsAllowedAndLastClaimResetsOnceWithoutDeletingOldWitnesses()
    {
        using var f = new Fixture(providerOptions); await f.Begin(15); await f.Medal(15, 0, new(27, 1));
        var final = await f.Claim(15, 3);
        Assert.Equal(0ul, final.CycleId); Assert.True(final.Track.Slots[3].Claimed); Assert.Equal(2700u, final.Track.Points);
        await f.Claim(15, 0); await f.Claim(15, 1); var last = await f.Claim(15, 2);
        Assert.Equal(1ul, last.Track.CycleId); Assert.Equal(1500u, last.Track.Points);
        Assert.All(last.Track.Slots, slot => Assert.False(slot.Claimed));
        int samples = f.Samples;
        var duplicate = await f.Execute(repo => repo.Claim(15, 0, 1, final.RewardRowId, final.Choice,
            Guid.NewGuid().ToString("N"), DateTime.UtcNow, null));
        Assert.False(duplicate.Applied); Assert.Equal(final.ClaimId, duplicate.ClaimId); Assert.Equal(samples, f.Samples);
        using var read = f.Context(); Assert.Equal(8, read.CharacterChallengeRewardSlot.Count());
        Assert.Equal(4, read.CharacterChallengeRewardClaim.Count()); Assert.NotEmpty(read.Item);
    }

    [Fact]
    public async Task ReceiptAndItemsRollbackTogetherAndTheHeldDeliveryResolvesBeforeRetry()
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0); using var held = f.Reserve(input.Row, 0);
        string claim = Guid.NewGuid().ToString("N");
        await using (var context = f.Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            f.Repository(context).Claim(16, input.Cycle, input.Revision, input.Row, 0, claim, DateTime.UtcNow, held);
            await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.Uncertain, held.State);
        await using (var fresh = f.Context())
        {
            await using var transaction = await fresh.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Assert.Equal(InventoryRewardReservationState.ReadyToRetry, held.ResolveOutcome(fresh));
        }
        await f.Execute(repo => repo.Claim(16, input.Cycle, input.Revision, input.Row, 0, claim, DateTime.UtcNow, held));
        Assert.True(held.PublishCommitted());
        using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Theory]
    [InlineData("row")] [InlineData("revision")] [InlineData("cycle")] [InlineData("choice")]
    [InlineData("points")] [InlineData("prerequisite")] [InlineData("reservation")]
    public async Task InvalidClaimNeverAllocatesIdsOrStagesPartialItems(string error)
    {
        using var f = new Fixture(providerOptions); await f.Begin(16);
        if (error != "points") await f.Medal(16, 0, new(1, 1));
        var input = f.ClaimInput(16, 0);
        using var held = f.Reserve(input.Row, 0);
        if (error == "prerequisite") { f.Tracks.Single(row => row.Id == 16).PrerequisiteId = 99; f.RebuildCatalog(); f.Meets = false; }
        uint row = error == "row" ? f.Catalog.GetTrack(16).Slots[1].Candidates[0].Id : input.Row;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.Claim(16,
            error == "cycle" ? input.Cycle + 1 : input.Cycle, error == "revision" ? input.Revision + 1 : input.Revision,
            row, error == "choice" ? 99u : 0u, Guid.NewGuid().ToString("N"), DateTime.UtcNow,
            error == "reservation" ? null : held)));
        Assert.Empty(f.ManagerProxy.GetInvocations("get_NextItemId"));
        using var read = f.Context(); Assert.Empty(read.Item); Assert.Empty(read.CharacterChallengeRewardClaim);
        Assert.All(read.CharacterChallengeRewardSlot, slot => Assert.False(slot.Claimed));
        Assert.True(held.Cancel());
    }

    [Theory]
    [InlineData("current")] [InlineData("original")] [InlineData("detach")] [InlineData("foreign")]
    public async Task ChangedStagingPoisonsTheWholeCharacterUnit(string mutation)
    {
        using var f = new Fixture(providerOptions); await f.Begin(16);
        var before = f.State(16);
        await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        f.Repository(context).CreditMedal(before.AttemptId, before.ChallengeId, 0, new(1, 1), before,
            before with { Tier = 1, LastRewardTier = 0 }, DateTime.UtcNow);
        var track = context.ChangeTracker.Entries<CharacterChallengeRewardTrackModel>().Single();
        if (mutation == "current") track.Entity.Points++;
        if (mutation == "original") track.OriginalValues[nameof(CharacterChallengeRewardTrackModel.Revision)] = 999ul;
        if (mutation == "detach") track.State = EntityState.Detached;
        if (mutation == "foreign") context.Item.Add(new ItemModel { Id = 999, OwnerId = 42, ItemId = 10 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
        await transaction.RollbackAsync(); using var read = f.Context(); Assert.Empty(read.CharacterChallengeMedalReceipt);
        Assert.Equal(0u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task ConcurrentFreshUnitsCannotInitializeTheSameAttemptTwice()
    {
        using var f = new Fixture(providerOptions);
        string attempt = Guid.NewGuid().ToString("N");
        await using var first = f.Context(); await using var transaction = await first.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        f.Repository(first).BeginAttempt(null, f.Activated(16), attempt, true);
        using var entered = new ManualResetEventSlim();
        Task replay = Task.Run(async () => { entered.Set(); await f.Execute(repo => repo.BeginAttempt(null, f.Activated(16), attempt, true)); });
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.NotSame(replay, await Task.WhenAny(replay, Task.Delay(100)));
        await first.SaveChangesAsync(); await transaction.CommitAsync(); await replay.WaitAsync(TimeSpan.FromSeconds(10));
        using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt); Assert.Single(read.CharacterChallengeRewardTrack);
        Assert.Equal(3, read.CharacterChallengeRewardSlot.Count());
    }

    [Fact]
    public async Task ConcurrentCreditsDeduplicateAgainstTheCommittedMedalReceipt()
    {
        using var f = new Fixture(providerOptions); await f.Begin(16);
        var before = f.State(16); var after = before with { Tier = 1, LastRewardTier = 0 };
        await using var first = f.Context(); await using var transaction = await first.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        f.Repository(first).CreditMedal(before.AttemptId, before.ChallengeId, 0, new(1.25f, 1), before, after, DateTime.UtcNow);
        using var entered = new ManualResetEventSlim();
        Task<CharacterChallengeMedalState> replay = Task.Run(async () =>
        {
            entered.Set(); return await f.Execute(repo => repo.CreditMedal(before.AttemptId, before.ChallengeId, 0, new(7, 9), before, after, DateTime.UtcNow));
        });
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.NotSame(replay, await Task.WhenAny(replay, Task.Delay(100)));
        await first.SaveChangesAsync(); await transaction.CommitAsync();
        var duplicate = await replay.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(duplicate.Applied); Assert.Equal(125u, duplicate.Points); Assert.Equal(1, duplicate.ModifierRevision);
        using var read = f.Context(); Assert.Single(read.CharacterChallengeMedalReceipt);
        Assert.Equal(125u, read.CharacterChallengeRewardTrack.Single().Points);
    }

    [Fact]
    public async Task HistoricalNullMarkersLoadWithoutBackfillAndNewStateLoadsAsCopiedSnapshots()
    {
        using var f = new Fixture(providerOptions);
        using (var seed = f.Context())
        {
            seed.CharacterChallenge.Add(new CharacterChallengeModel { Id = 42, ChallengeId = f.Challenge(15), Activated = true,
                CurrentTier = 2, LastRewardTier = 1, CompletionCount = 43, ActiveTimerSeconds = 90 });
            seed.SaveChanges();
        }
        var empty = await f.Execute(repo => repo.LoadState());
        Assert.Empty(empty.Tracks); Assert.Empty(empty.Attempts);
        await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var state = await f.Execute(repo => repo.LoadState()); Assert.Single(state.Tracks); Assert.Single(state.Attempts);
        Assert.Equal(100u, state.Tracks[0].Points);
        using var read = f.Context(); var old = read.CharacterChallenge.Single(row => row.ChallengeId == f.Challenge(15));
        Assert.Null(old.ActiveAttemptId); Assert.Equal(43u, old.CompletionCount); Assert.True(old.Activated);
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeSlotState>)state.Tracks[0].Slots)[0] = new(0, 0, 0, true));
    }

    [Fact]
    public async Task DurableClaimReplayDoesNotResurrectConsumedItemRowsOrReplaceItsChoice()
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); await f.Medal(16, 0, new(2, 1));
        var claim = await f.Claim(16, 1);
        using (var consume = f.Context()) consume.Item.ExecuteDelete();
        int allocations = f.ManagerProxy.GetInvocations("get_NextItemId").Count;
        var duplicate = await f.Execute(repo => repo.Claim(16, claim.CycleId, 0, claim.RewardRowId, 0,
            Guid.NewGuid().ToString("N"), DateTime.UtcNow, null));
        Assert.False(duplicate.Applied); Assert.Equal(claim.ClaimId, duplicate.ClaimId);
        Assert.True(f.Catalog.GetReward(claim.RewardRowId).Reward.Choices.Count > 1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.Claim(16, claim.CycleId, 0, claim.RewardRowId, 1,
            Guid.NewGuid().ToString("N"), DateTime.UtcNow, null)));
        Assert.Equal(allocations, f.ManagerProxy.GetInvocations("get_NextItemId").Count);
        using var read = f.Context(); Assert.Empty(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Theory]
    [InlineData("medal")] [InlineData("nan")] [InlineData("negative")] [InlineData("overflow")]
    public async Task InvalidMedalsCannotCreateAReceiptOrChangeTheChallenge(string fault)
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); var before = f.State(16);
        if (fault == "overflow") { using var seed = f.Context(); seed.CharacterChallengeRewardTrack.Single().Points = uint.MaxValue; seed.SaveChanges(); }
        Task Operation() => f.Medal(16, fault == "medal" ? (byte)1 : (byte)0,
            new(fault == "nan" ? float.NaN : fault == "negative" ? -1 : 1, 1));
        if (fault == "overflow") await Assert.ThrowsAsync<OverflowException>(Operation);
        else await Assert.ThrowsAsync<InvalidOperationException>(Operation);
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeMedalReceipt); Assert.Equal(before, f.State(16));
        Assert.Equal(0u, read.CharacterChallengeAttempt.Single().EarnedMedalMask);
    }

    [Theory]
    [InlineData("mask")] [InlineData("marker")] [InlineData("terminal")]
    public async Task ReloadRejectsBrokenAttemptReceiptOrChallengeBindings(string fault)
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        using (var corrupt = f.Context())
        {
            if (fault == "mask") corrupt.CharacterChallengeAttempt.Single().EarnedMedalMask = 3;
            if (fault == "marker") corrupt.CharacterChallenge.Single().ActiveAttemptId = Guid.NewGuid().ToString("N");
            if (fault == "terminal") corrupt.CharacterChallengeAttempt.Single().Terminal = true;
            corrupt.SaveChanges();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.LoadState()));
    }

    [Theory]
    [InlineData("flush")] [InlineData("source")]
    public async Task IncompleteOrChangedSourceEnrollmentCannotCommit(string fault)
    {
        using var f = new Fixture(providerOptions);
        await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        f.Repository(context).BeginAttempt(null, f.Activated(16), Guid.NewGuid().ToString("N"), true);
        if (fault == "source")
        {
            f.Tables.Challenge.GetEntry(f.Challenge(16)).RewardTrackId = 15;
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        else await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        Assert.True(context.IsSaveRejected);
        await transaction.RollbackAsync(); using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt);
        Assert.Empty(read.CharacterChallengeRewardTrack); Assert.Empty(read.CharacterChallenge);
    }

    [Fact]
    public void NontransactionalOrAlreadyTrackedContextsArePermanentlyRejected()
    {
        using var f = new Fixture(providerOptions);
        using (var context = f.Context())
        {
            Assert.Throws<InvalidOperationException>(() => f.Repository(context)); Assert.True(context.IsSaveRejected);
        }
        using (var context = f.Context())
        {
            using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
            context.Character.Single();
            Assert.Throws<InvalidOperationException>(() => f.Repository(context)); Assert.True(context.IsSaveRejected);
        }
    }

    [Fact]
    public async Task AnEarnedAttemptCannotBeDowngradedToLegacyModeDuringCompletion()
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); await f.Medal(16, 0, new(1, 1));
        var before = f.State(16);
        using (var corrupt = f.Context()) { corrupt.CharacterChallengeAttempt.Single().RewardMode = 0; corrupt.SaveChanges(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.FinishAttempt(before.AttemptId, before.ChallengeId,
            before, before with { Activated = false, Completions = 1 })));
        using var read = f.Context(); Assert.False(read.CharacterChallengeAttempt.Single().Terminal);
        Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Theory]
    [InlineData("quantity")] [InlineData("owner")] [InlineData("duplicate")] [InlineData("existing")] [InlineData("guid")]
    public async Task ReloadValidatesTheWholeDeliveryWitnessWithoutResurrectingItsItems(string fault)
    {
        using var f = new Fixture(providerOptions); await f.Begin(16); await f.Medal(16, 0, new(1, 1)); await f.Claim(16, 0);
        using (var corrupt = f.Context())
        {
            var receipt = corrupt.CharacterChallengeRewardClaim.Single();
            JsonNode payload = JsonNode.Parse(receipt.DeliveryJson)!;
            var lines = payload["Lines"]!.AsArray(); var line = lines[0]!;
            if (fault == "quantity") line["AwardedCount"] = 0;
            if (fault == "owner") line["After"]!["CharacterId"] = 43;
            if (fault == "duplicate") lines.Add(line.DeepClone());
            if (fault == "existing") line["ExistingLiveItem"] = true;
            if (fault == "guid") line["After"]!["Guid"] = 0;
            receipt.DeliveryJson = payload.ToJsonString(); corrupt.SaveChanges();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Execute(repo => repo.LoadState()));
        using var read = f.Context(); Assert.Single(read.Item); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    internal sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-CharacterRewardRepo-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<CharacterContext> options;
        private FixtureConnectionDiagnostics connectionDiagnostics;
        internal void EnableConnectionDiagnostics() => connectionDiagnostics ??= new();
        internal string DescribeConnectionDiagnostics() => connectionDiagnostics?.Describe() ?? "disabled";
        public RewardTrackEntry[] Tracks { get; } = LoyaltyCatalogBuild16042Fixture.Tracks();
        public IGameTableManager Tables { get; }
        public CharacterChallengeRewardCatalog Catalog { get; private set; }
        public int Samples; public bool Meets = true;
        private Inventory inventory;
        public Inventory Inventory => inventory;
        public IPlayer Player { get; private set; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; private set; }
        public RecordingDispatchProxy<IGameSession> SessionProxy { get; private set; }
        public RecordingDispatchProxy<IItemManager> ManagerProxy { get; private set; }
        private readonly Dictionary<uint, IItemInfo> infos = [];
        public Fixture(DbContextOptions<CharacterContext> provider)
        {
            Directory.CreateDirectory(directory);
            options = provider ?? new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source={Path.Combine(directory, "repository.sqlite")};Pooling=False;Default Timeout=10").Options;
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks));
            proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
            proxy.SetProperty(nameof(IGameTableManager.Challenge), PlayerBindPointTests.Table(Tracks.Where(row => row.RewardTrackTypeEnum == 3)
                .Select(row => new ChallengeEntry { Id = Challenge(row.Id), RewardTrackId = row.Id,
                    ChallengeTierId00 = 1, ChallengeTierId01 = 2, ChallengeTierId02 = 3 }).ToArray()));
            RebuildCatalog();
            using var context = Context(); context.Database.EnsureCreated();
            context.Character.Add(new CharacterModel { Id = 42, Name = "Character reward repository" }); context.SaveChanges();
        }
        public void RebuildCatalog() => Catalog = new(Tables);
        public ushort Challenge(uint track) => checked((ushort)(1000 + track));
        public CharacterContext Context()
        {
            if (connectionDiagnostics == null) return new(options);
            var builder = new DbContextOptionsBuilder<CharacterContext>(options).AddInterceptors(connectionDiagnostics);
            builder.LogTo(FixtureConnectionDiagnostics.ShouldObserve, connectionDiagnostics.Observe);
            return new(builder.Options);
        }
        public CharacterContext Intercepted(params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<CharacterContext>(options).AddInterceptors(interceptors);
            if (connectionDiagnostics != null)
            {
                builder.AddInterceptors(connectionDiagnostics);
                builder.LogTo(FixtureConnectionDiagnostics.ShouldObserve, connectionDiagnostics.Observe);
            }
            return new(builder.Options);
        }
        public DbContextOptions<CharacterContext> Options => options;
        public CharacterChallengeRewardRepository Repository(CharacterContext context)
            => new(context, 42, Catalog, Tables, _ => Meets, count => { Samples++; return 0; });
        public CharacterChallengeState State(uint track) { using var read = Context(); return CharacterChallengeState.FromModel(read.CharacterChallenge.Single(row => row.ChallengeId == Challenge(track))); }
        public CharacterChallengeState Activated(uint track)
            => new(42, Challenge(track), null, true, false, false, 0, 0, 0, 0, 300, 0, 0);
        public async Task<T> Execute<T>(Func<CharacterChallengeRewardRepository, T> stage)
        {
            await using var context = Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            T result = stage(Repository(context)); await context.SaveChangesAsync(); await transaction.CommitAsync(); return result;
        }
        public async Task<CharacterChallengeAttemptState> Begin(uint track)
        {
            using var read = Context(); var previous = CharacterChallengeState.FromModel(read.CharacterChallenge.SingleOrDefault(row => row.ChallengeId == Challenge(track)));
            return await Execute(repo => repo.BeginAttempt(previous, Activated(track) with { Completions = previous?.Completions ?? 0 }, Guid.NewGuid().ToString("N"), true));
        }
        public Task<CharacterChallengeMedalState> Medal(uint track, byte ordinal, ChallengeRewardModifierSnapshot modifier)
        {
            var before = State(track); var after = before with { Tier = ordinal == 2 ? 2u : (uint)ordinal + 1,
                LastRewardTier = ordinal, Activated = ordinal != 2, Completions = before.Completions + (ordinal == 2 ? 1u : 0) };
            return Execute(repo => repo.CreditMedal(before.AttemptId, before.ChallengeId, ordinal, modifier, before, after, DateTime.UtcNow));
        }
        public (uint Row, ulong Cycle, ulong Revision) ClaimInput(uint track, byte ordinal)
        {
            using var read = Context(); var ledger = read.CharacterChallengeRewardTrack.Single(row => row.TrackId == track);
            uint rowId = read.CharacterChallengeRewardSlot.Single(row => row.TrackId == track && row.CycleId == ledger.CycleId && row.Ordinal == ordinal).RewardRowId;
            return (rowId, ledger.CycleId, ledger.Revision);
        }
        public async Task<CharacterChallengeClaimState> Claim(uint track, byte ordinal)
        {
            var input = ClaimInput(track, ordinal); using var held = Reserve(input.Row, 0);
            var result = await Execute(repo => repo.Claim(track, input.Cycle, input.Revision, input.Row, 0, Guid.NewGuid().ToString("N"), DateTime.UtcNow, held));
            Assert.True(held.PublishCommitted()); return result;
        }
        public IInventoryRewardReservation Reserve(uint row, uint choice)
        {
            var item = Catalog.GetReward(row).Reward.GetChoice(choice);
            if (inventory == null)
            {
                inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
                var session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy); SessionProxy = sessionProxy;
                var account = RecordingDispatchProxy<IAccount>.Create(out var ap); ap.SetProperty(nameof(IAccount.Session), session);
                var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
                Player = player; PlayerProxy = pp;
                pp.SetProperty(nameof(IPlayer.CharacterId), 42ul); pp.SetProperty(nameof(IPlayer.Session), session);
                pp.SetProperty(nameof(IPlayer.Account), account); pp.SetProperty(nameof(IPlayer.Inventory), inventory);
                var quests = RecordingDispatchProxy<IQuestManager>.Create(out var qp); qp.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());
                pp.SetProperty(nameof(IPlayer.QuestManager), quests);
                var manager = RecordingDispatchProxy<IItemManager>.Create(out var mp); ManagerProxy = mp;
                ulong next = 500; mp.SetMethodReturnFactory("get_NextItemId", () => next++);
                mp.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => infos[(uint)args[0]]);
                Set("characterId", 42ul); Set("player", player); Set("itemManager", manager);
                Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, 20, manager) });
                Set("deletedItems", new List<IItem>());
            }
            if (!infos.TryGetValue(item.ItemId, out var info))
            {
                info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
                ip.SetMethodReturnFactory("get_Id", () => item.ItemId);
                ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = item.ItemId, MaxStackCount = 10, MaxCharges = 1 });
                ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true); infos.Add(item.ItemId, info);
            }
            Assert.True(inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, info, item.Count, out var held, out _));
            Assert.Equal(42ul, held.CharacterId); Assert.Equal(item.ItemId, held.ItemId); Assert.Equal(item.Count, held.Count);
            return held;
        }
        private void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(inventory, value);
        public void Dispose()
        {
            string full = Path.GetFullPath(directory), parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("NexusForever-CharacterRewardRepo-", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture cleanup escaped its temp root.");
            if (connectionDiagnostics != null) Console.Error.WriteLine("Fixture diagnostic origin cost: " + connectionDiagnostics.OriginCost());
            try { if (Directory.Exists(full)) TemporaryDirectoryCleanup.Delete(full); }
            catch
            {
                if (connectionDiagnostics != null) Console.Error.WriteLine("Fixture connection lifetimes on cleanup failure: " + connectionDiagnostics.Describe());
                throw;
            }
        }
    }
}
