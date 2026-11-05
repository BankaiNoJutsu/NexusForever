using System.Collections.Immutable;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Quest.Reward;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using PersistenceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed partial class ContractLiveCompletionTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractLiveCompletionTests() { }
    internal ContractLiveCompletionTests(DbContextOptions<CharacterContext> options) => this.options = options;
    public static IEnumerable<object[]> ActualSources() => ContractCompletionRepositoryTests.ActualSources();

    private static ServerRewardTracksReload[] Reloads(Fixture f) => f.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted")
        .Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>().ToArray();
    private static IEnumerable<ServerChallengeResult> MedalResults(Fixture f) => f.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted")
        .Select(call => call.Arguments[0]).OfType<ServerChallengeResult>()
        .Where(result => result.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);

    [Theory]
    [InlineData("reward")] [InlineData("json")] [InlineData("marker")] [InlineData("drop")]
    public async Task SelectedRowsAndTheirInitializedMarkerCannotChangeAfterFinalEnrollment(string fault)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        ((RecordingDispatchProxy<IAppearanceManager>)(object)f.Actor.Player.AppearanceManager).SetMethodHandler(nameof(IAppearanceManager.Save), args =>
        {
            var context = (CharacterContext)args[0]; context.SavingChanges += (_, _) =>
            {
                var slot = context.CharacterContractRewardSlot.Local.First();
                if (fault == "reward") slot.RewardRowId = 35;
                if (fault == "json") slot.SelectionJson = "{}";
                if (fault == "marker") context.CharacterContractRewardTrack.Local.Single().SelectionInitialized = false;
                if (fault == "drop") context.CharacterContractRewardSlot.Remove(slot);
            }; return null;
        });
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractRewardSlot); Assert.Empty(read.CharacterContractCompletion);
        Assert.Empty(read.CharacterContractRewardTrack); Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task CapturedEligibilityAndSubsequentCompletionsCannotRerollAnInitializedCycle()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.OnXp = () => f.Eligibility = id => id == f.Source.TrackPrerequisiteId;
        f.Complete(); await f.Actor.Drain(); var before = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        Assert.True(before.SelectionInitialized); Assert.NotEmpty(before.Slots);
        f.AddQuest(9881); f.Actor.Quests.QuestComplete(9881, 0, true); await f.Actor.Drain();
        var after = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        Assert.Equal(before.Slots, after.Slots); Assert.Equal(before.CycleId, after.CycleId);
        Assert.Equal(before.Points * 2, after.Points); Assert.Equal(before.Revision + 1, after.Revision);
        using var read = f.Inner.Context(); Assert.Equal(before.Slots.Count, read.CharacterContractRewardSlot.Count()); Assert.Equal(2, read.CharacterContractCompletion.Count());
        var catalog = new ContractRewardCatalog(f.Tables);
        Assert.All(after.Slots, slot => Assert.Equal(before.Slots[slot.Ordinal].SelectionJson, ContractRewardSelection.Restore(catalog, slot.SelectionJson).Json));
    }

    [Fact]
    public async Task MissingRewardEligibilityPreservesOrdinaryCompletionAndWaitsForALaterEligibleGeneration()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.Eligibility = id => id == f.Source.TrackPrerequisiteId;
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        var before = Assert.Single(f.Actor.Quests.CommittedContractTracks); Assert.False(before.SelectionInitialized); Assert.Empty(before.Slots);
        f.Eligibility = _ => true; f.AddQuest(9881); f.Actor.Quests.QuestComplete(9881, 0, true); await f.Actor.Drain();
        var after = Assert.Single(f.Actor.Quests.CommittedContractTracks); Assert.True(after.SelectionInitialized); Assert.NotEmpty(after.Slots);
        Assert.Equal(before.Points * 2, after.Points); Assert.Equal(before.CycleId, after.CycleId);
    }

    [Fact]
    public async Task ACustomOwnerWithoutRewardEligibilityKeepsOrdinaryPrizesAndPointsWithoutInventingSelections()
    {
        using var f = new Fixture(options); await f.Actor.Load(); Assert.Equal(0u, f.Source.TrackPrerequisiteId);
        typeof(QuestManager).GetField("prerequisiteManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(f.Actor.Quests, null);
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        Assert.False(Assert.Single(f.Actor.Quests.CommittedContractTracks).SelectionInitialized);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractRewardSlot);
    }

    [Fact]
    public async Task SelectionInitializationRetainsExistingUnselectedPointsCycleAndActiveState()
    {
        using var f = new Fixture(options);
        using (var seed = f.Inner.Context())
        { seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = f.Source.TrackId, CycleId = 7, Revision = 3, Points = 899, Active = true }); seed.SaveChanges(); }
        await f.Actor.Load(); f.Complete(); await f.Actor.Drain();
        var after = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        Assert.True(after.SelectionInitialized); Assert.Equal(7ul, after.CycleId); Assert.Equal(899 + f.Source.BasePoints, after.Points);
        Assert.Equal(4ul, after.Revision); Assert.True(after.Active);
        using var read = f.Inner.Context(); Assert.All(read.CharacterContractRewardSlot, row => Assert.Equal(7ul, row.CycleId));
    }

    [Fact]
    public async Task ASelectionCannotSilentlyRebaseToATrackCreatedDuringTheAuthGap()
    {
        using var f = new Fixture(options); await f.Actor.Load();
        f.Actor.OnAuth = () =>
        {
            f.AuthCalls++; using var seed = f.Inner.Context(); seed.CharacterContractRewardTrack.Add(new()
            { Id = 42, TrackId = f.Source.TrackId, CycleId = 7, Revision = 3, Points = 899 }); seed.SaveChanges();
        };
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractRewardSlot); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item);
        Assert.Equal(899u, read.CharacterContractRewardTrack.Single().Points); Assert.Equal(3ul, read.CharacterContractRewardTrack.Single().Revision);
    }

    [Theory]
    [InlineData("missing")] [InlineData("ordinal")] [InlineData("json")] [InlineData("marker")]
    public async Task CorruptSelectedCyclesCannotRelogOrResampleAReplacement(string fault)
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.Complete(); await f.Actor.Drain(); f.Actor.Dispose();
        using (var edit = f.Inner.Context())
        {
            var slot = edit.CharacterContractRewardSlot.First();
            if (fault == "missing") edit.CharacterContractRewardSlot.Remove(slot);
            if (fault == "ordinal") slot.RewardRowId = 69;
            if (fault == "json") slot.SelectionJson = "{}";
            if (fault == "marker") edit.CharacterContractRewardTrack.Single().SelectionInitialized = false;
            edit.SaveChanges();
        }
        using var relog = new Actor(f.Inner, questTables: f.Tables, contracts: new ContractManager(f.Tables));
        relog.Challenges.SendInitialPackets(); await relog.Drain(allowErrors: true);
        Assert.True(relog.Challenges.RequiresAuthoritativeReload); Assert.Null(relog.Quests.CommittedContractTracks);
        using var read = f.Inner.Context(); Assert.Single(read.CharacterContractCompletion);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task OnlyTheSuccessfulCompositeCommitPublishesContractPointsAlongsideTheFinalChallengeAndLoyaltyRows(bool challenge, bool retry)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([new() { RewardTrackId = 9, RewardPointsEarned = 800 }]));
        var before = f.Actor.Quests.CommittedContractTracks; Assert.NotNull(before); Assert.Empty(before);
        int count = Reloads(f).Length;
        int reloadAttempts = 0;
        if (retry) f.Inner.Source.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", args =>
            args[0] is ServerRewardTracksReload && ++reloadAttempts == 1 ? throw new IOException("Composite reload failed before enqueue.") : null);
        f.OnXp = () =>
        {
            Assert.Same(before, f.Actor.Quests.CommittedContractTracks); Assert.Equal(count, Reloads(f).Length);
            if (challenge) { f.Actor.Challenges.HandleChoice(f.Inner.Source.Challenge(16), ChallengeChoice.Activate); Assert.True(f.Actor.Challenges.TryAdvanceProgress(f.Inner.Source.Challenge(16), 10)); }
        };
        f.Actor.OnAuth = () => { f.AuthCalls++; Assert.Same(before, f.Actor.Quests.CommittedContractTracks); Assert.Equal(count, Reloads(f).Length); };
        f.Complete(); Assert.Equal(count, Reloads(f).Length); await f.Actor.Drain(); f.AssertCommitted();
        Assert.Equal(count + (retry ? 2 : 1), Reloads(f).Length); var packet = Reloads(f).Last();
        var contract = Assert.Single(packet.RewardsTracks, row => row.RewardTrackId == f.Source.TrackId);
        Assert.Equal(f.Source.BasePoints, contract.RewardPointsEarned);
        Assert.Equal(Assert.Single(f.Actor.Quests.CommittedContractTracks).Slots.Select(slot => slot.RewardRowId), contract.RewardTrackRewardsIds);
        Assert.Equal(0u, contract.Unlocked); Assert.All(contract.ClaimedRewards.GetBuffer(), value => Assert.Equal(0, value));
        Assert.Equal(800u, Assert.Single(packet.RewardsTracks, row => row.RewardTrackId == 9).RewardPointsEarned);
        if (challenge)
        {
            Assert.Equal(100u, Assert.Single(packet.RewardsTracks, row => row.RewardTrackId == 16).RewardPointsEarned);
            Assert.Equal(ChallengeResult.TierAchieved, Assert.Single(MedalResults(f)).Result);
        }
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
    }

    [Theory]
    [InlineData("auth")] [InlineData("flush")] [InlineData("before-commit")] [InlineData("lost-ack")]
    public async Task FailedOrUncertainCompletionCannotPublishItsProvisionalContractPoints(string fault)
    {
        using var f = new Fixture(options, fault: fault); await f.Actor.Load();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        var before = f.Actor.Quests.CommittedContractTracks; int count = Reloads(f).Length;
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        Assert.Same(before, f.Actor.Quests.CommittedContractTracks); Assert.Equal(count, Reloads(f).Length);
        Assert.False(f.Actor.Quests.PublishContractRewards());
        using var read = f.Inner.Context(); Assert.Equal(fault == "lost-ack" ? 1 : 0, read.CharacterContractCompletion.Count());
    }

    [Fact]
    public async Task RelogLoadsBothDurableContractTypesWithoutBackfillingOrLosingLoyalty()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.Complete(); await f.Actor.Drain();
        using (var seed = f.Inner.Context())
        {
            uint other = f.Source.TrackId == 4 ? 5u : 4u;
            seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = other, Points = 900, Revision = 3, CycleId = 7, Active = true }); seed.SaveChanges();
        }
        f.Actor.Dispose(); using var relog = new Actor(f.Inner, questTables: f.Tables, contracts: new ContractManager(f.Tables));
        await relog.Load(); Assert.True(relog.Player.RewardTrackPresentation.PublishAccountTracks([new() { RewardTrackId = 9, RewardPointsEarned = 800 }]));
        var tracks = Assert.IsType<ServerRewardTracksReload>(f.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Last().Arguments[0]).RewardsTracks;
        Assert.Equal(new uint[] { 4, 5, 9 }, tracks.Select(row => row.RewardTrackId).Order());
        Assert.Equal(f.Source.BasePoints, Assert.Single(tracks, row => row.RewardTrackId == f.Source.TrackId).RewardPointsEarned);
        Assert.Equal(900u, Assert.Single(tracks, row => row.RewardTrackId is 4 or 5 && row.RewardTrackId != f.Source.TrackId).RewardPointsEarned);
        Assert.Equal(7ul, Assert.Single(relog.Quests.CommittedContractTracks, row => row.TrackId != f.Source.TrackId).CycleId);
        Assert.True(relog.Player.RewardTrackPresentation.IsPublishedContractSnapshot(relog.Quests.CommittedContractTracks));
        using var read = f.Inner.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Equal(2, read.CharacterContractRewardTrack.Count());
    }

    [Theory]
    [InlineData("unknown-owner")] [InlineData("zero-revision")]
    public async Task InvalidDurableContractOwnersQuarantineTheLoadBeforeAnyReplacingPublication(string corruption)
    {
        using var f = new Fixture(options);
        using (var seed = f.Inner.Context())
        { seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = corruption == "unknown-owner" ? 35u : f.Source.TrackId, Revision = corruption == "zero-revision" ? 0ul : 1ul }); seed.SaveChanges(); }
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([])); int count = Reloads(f).Length;
        f.Actor.Challenges.SendInitialPackets(); await f.Actor.Drain(allowErrors: true);
        Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload); Assert.Null(f.Actor.Quests.CommittedContractTracks);
        Assert.Equal(count, Reloads(f).Length);
    }

    [Theory]
    [InlineData("stale")] [InlineData("conflict")] [InlineData("missing")]
    public async Task StaleOrConflictingSnapshotsCannotReplaceTheCommittedContractOwner(string mode)
    {
        using var f = new Fixture(options);
        using (var seed = f.Inner.Context()) { seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = f.Source.TrackId, Revision = 3, Points = 200 }); seed.SaveChanges(); }
        await f.Actor.Load(); f.Complete(); await f.Actor.Drain();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        var before = f.Actor.Quests.CommittedContractTracks; var row = Assert.Single(before);
        IReadOnlyList<ContractTrackState> replacement = mode == "missing" ? []
            : [row with { Revision = mode == "stale" ? row.Revision - 1 : row.Revision, Points = row.Points + 1 }];
        Assert.Throws<InvalidOperationException>(() => f.Actor.Quests.AdoptContractRewards(replacement)); Assert.Same(before, f.Actor.Quests.CommittedContractTracks);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(before));
    }

    [Fact]
    public async Task AccountReadinessCanArriveAfterTheCompletionAndPublishOnlyTheNewestCommittedContractSnapshot()
    {
        using var f = new Fixture(options); await f.Actor.Load(); Assert.Empty(Reloads(f));
        var empty = f.Actor.Quests.CommittedContractTracks; f.Complete(); await f.Actor.Drain(); Assert.Empty(Reloads(f));
        Assert.False(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(empty));
        Assert.False(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        Assert.Equal(f.Source.BasePoints, Assert.Single(Assert.Single(Reloads(f)).RewardsTracks).RewardPointsEarned);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
    }

    [Fact]
    public async Task ASubsequentIndependentChallengeCommandRetainsThePublishedContractSnapshotAndPoints()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.Complete(); await f.Actor.Drain();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([new() { RewardTrackId = 9, RewardPointsEarned = 800 }]));
        var contract = f.Actor.Quests.CommittedContractTracks;
        f.Actor.Challenges.HandleChoice(f.Inner.Source.Challenge(16), ChallengeChoice.Activate); await f.Actor.Drain();
        Assert.True(f.Actor.Challenges.TryAdvanceProgress(f.Inner.Source.Challenge(16), 10)); await f.Actor.Drain();
        Assert.Same(contract, f.Actor.Quests.CommittedContractTracks);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(contract));
        var rows = Reloads(f).Last().RewardsTracks;
        Assert.Equal(f.Source.BasePoints, Assert.Single(rows, row => row.RewardTrackId == f.Source.TrackId).RewardPointsEarned);
        Assert.Equal(100u, Assert.Single(rows, row => row.RewardTrackId == 16).RewardPointsEarned);
        Assert.Equal(800u, Assert.Single(rows, row => row.RewardTrackId == 9).RewardPointsEarned);
    }

    [Fact]
    public async Task AReloadEnqueueFailureQuarantinesTheActorWithoutReplayingTheCommittedOrdinaryRewards()
    {
        using var f = new Fixture(options); await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        f.Inner.Source.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", args => args[0] is ServerRewardTracksReload
            ? throw new IOException("Contract reload enqueue fault.") : null);
        f.Complete(); await f.Actor.Drain(allowErrors: true); f.AssertCommitted();
        Assert.True(f.Actor.Quests.RequiresAuthoritativeReload); Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        Assert.Single(f.Inner.Source.SessionProxy.GetInvocations("ForceDisconnect"));
        Assert.Throws<InvalidOperationException>(f.Complete); Assert.Equal(1, f.XpCalls);
    }

    [Fact]
    public async Task ADeferredReloadRecoversCommittedOrdinaryRewardsWithoutQuarantiningTheActor()
    {
        using var f = new Fixture(options); await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        int attempts = 0;
        f.Inner.Source.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", args => args[0] is ServerRewardTracksReload && ++attempts <= 2
            ? throw new IOException("Contract reload enqueue fault.") : null);
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        Assert.Equal(3, attempts); Assert.Equal(1, f.XpCalls);
        Assert.False(f.Actor.Quests.RequiresAuthoritativeReload); Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
        Assert.Empty(f.Inner.Source.SessionProxy.GetInvocations("ForceDisconnect"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidLateContractRowsCannotCommitWithTheOrdinaryRewardGraph(bool unknown)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        var appearance = (RecordingDispatchProxy<IAppearanceManager>)(object)f.Actor.Player.AppearanceManager;
        appearance.SetMethodHandler(nameof(IAppearanceManager.Save), args =>
        {
            var context = (CharacterContext)args[0]; context.CharacterContractRewardTrack.Add(new()
            { Id = 42, TrackId = unknown ? 35u : f.Source.TrackId == 4 ? 5u : 4u, Revision = unknown ? 1ul : 0ul }); return null;
        });
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterContractRewardTrack);
        Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [MemberData(nameof(ActualSources))]
    public async Task RealPublicCompletionPersistsTheActualOrdinaryPrizeMoneyAndFrozenContractPointsTogether(uint id)
    {
        using var f = new Fixture(options, id); await f.Actor.Load();
        f.Complete(); Assert.True(f.Actor.Player.IsNormalSaveInProgress); await f.Actor.Drain();
        f.AssertCommitted(); Assert.False(f.Actor.Quests.HasContractCompletionWork);
        Assert.False(f.Actor.Quests.RequiresAuthoritativeReload); Assert.True(f.Actor.Player.Session.CanProcessIncomingPackets);
        Assert.Equal(1, f.AuthCalls);
        var snapshot = f.Actor.Quests.CommittedContractTracks; Assert.Equal(f.Source.BasePoints, Assert.Single(snapshot).Points);
        var selected = Assert.Single(snapshot); Assert.True(selected.SelectionInitialized);
        var catalog = new ContractRewardCatalog(f.Tables); Assert.Equal(catalog.GetTrack(f.Source.TrackId).Slots.Count, selected.Slots.Count);
        using (var read = f.Inner.Context()) Assert.Equal(selected.Slots.Count, read.CharacterContractRewardSlot.Count());
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        Assert.Equal(f.Source.BasePoints, Assert.Single(Reloads(f).Last().RewardsTracks).RewardPointsEarned);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(snapshot));
    }

    [Theory]
    [InlineData("auth")] [InlineData("flush")] [InlineData("before-commit")] [InlineData("lost-ack")]
    public async Task CompletionFaultsRequireReloadAndNeverRunOrdinaryRewardsAgain(string fault)
    {
        using var f = new Fixture(options, fault: fault); await f.Actor.Load();
        f.Complete(); await f.Actor.Drain(allowErrors: true);
        Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        Assert.True(f.Actor.Quests.HasContractCompletionWork); Assert.Equal(1, f.XpCalls);
        Assert.Throws<InvalidOperationException>(() => f.Complete());
        using var read = f.Inner.Context();
        if (fault == "lost-ack") f.AssertCommitted();
        else
        {
            Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterContractRewardTrack); Assert.Empty(read.Item);
            Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Equal((byte)QuestState.Achieved, read.CharacterQuest.Single().State);
        }
        Assert.Equal(1, f.XpCalls);
    }

    [Fact]
    public async Task NestedNormalSaveWaitsForAllOrdinaryRewardsAndInvokesItsCallbackAfterTheAtomicCommit()
    {
        using var f = new Fixture(options); await f.Actor.Load(); int callbacks = 0;
        f.OnXp = () =>
        {
            f.Actor.Player.Save(() => { callbacks++; f.AssertCommitted(); }); Assert.Equal(0, f.AuthCalls);
            using var direct = f.Inner.Context(); Assert.Throws<InvalidOperationException>(() => f.Actor.Player.Save(direct));
        };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted(); Assert.Equal(1, callbacks); Assert.Equal(1, f.AuthCalls);
    }

    [Theory]
    [InlineData("normal")] [InlineData("inventory")]
    public async Task BusyRequestsWaitBeforeOrdinarySideEffectsAndDuplicateRequestsCannotReplayThem(string busy)
    {
        using var f = new Fixture(options); await f.Actor.Load(); IDisposable inventory = null;
        if (busy == "normal") f.Actor.StartSave();
        else Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out inventory));
        f.Complete(); f.Complete(); Assert.Equal(0, f.XpCalls);
        inventory?.Dispose(); await f.Actor.Drain(); f.AssertCommitted(); Assert.Equal(1, f.XpCalls);
    }

    [Fact]
    public async Task AReentrantCompletionRequestCannotReplayTheSameAcceptedGeneration()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.OnXp = f.Complete;
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted(); Assert.Equal(1, f.XpCalls);
    }

    [Theory]
    [InlineData("legacy-null")] [InlineData("modifier-unavailable")] [InlineData("custom-manager")]
    public async Task LegacyAndUnsupportedOwnersKeepTheirOrdinaryRewardsWithoutInventedPointHistory(string mode)
    {
        using var f = new Fixture(options, legacy: mode == "legacy-null", customManager: mode == "custom-manager"); await f.Actor.Load();
        if (mode == "modifier-unavailable") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account)
            .SetProperty(nameof(IAccount.RewardPropertyManager), RecordingDispatchProxy<IRewardPropertyManager>.Create(out _));
        f.Complete(); Assert.False(f.Actor.Quests.HasContractCompletionWork); await f.Actor.Player.SaveDirect();
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterContractRewardTrack);
        Assert.Single(read.Item); Assert.Equal(125ul, read.CharacterCurrency.Single().Amount); Assert.Equal(1, f.XpCalls);
    }

    [Fact]
    public async Task AuthGapModifierChangesAndLaterQuestIntentCannotRewriteTheFrozenCompletion()
    {
        using var f = new Fixture(options); await f.Actor.Load();
        var effect = ContractRewardPropertyTests.Effects().First(row => row.DataBits01 == f.Source.TrackType);
        f.Properties.UpdateContractSpellModifier(f.Actor.Player, 700, effect.SpellId, effect.Id, f.Source.TrackType, .5f, false);
        var quest = f.Actor.Quest;
        f.Actor.OnAuth = () =>
        {
            f.AuthCalls++; quest.Timer = 901;
            f.Properties.UpdateContractSpellModifier(f.Actor.Player, 700, effect.SpellId, effect.Id, f.Source.TrackType, .5f, true);
        };
        f.Complete(); await f.Actor.Drain();
        using (var read = f.Inner.Context())
        {
            Assert.Equal(f.Source.BasePoints * 3 / 2, read.CharacterContractRewardTrack.Single().Points);
            Assert.Null(read.CharacterQuest.Single().Timer);
        }
        Assert.True(quest.NeedsPersistenceSave); f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect();
        using var final = f.Inner.Context(); Assert.Equal(901u, final.CharacterQuest.Single().Timer); Assert.Single(final.CharacterContractCompletion);
    }

    [Fact]
    public async Task GameplayCallbacksCannotSaveAQuestOrObjectiveOutsideTheContractUnit()
    {
        using var f = new Fixture(options); await f.Actor.Load();
        f.OnXp = () =>
        {
            var quest = f.Actor.Quest; quest.Single().Progress = 6;
            using (var root = f.Inner.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Save(root)); Assert.True(root.IsSaveRejected); }
            using (var leaf = f.Inner.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Single().Save(leaf)); Assert.True(leaf.IsSaveRejected); }
        };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted(); using var read = f.Inner.Context(); Assert.Equal(6u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task AnOrdinaryRewardCallbackFailureReleasesItsPendingScopeAndRequiresReloadBeforeAuth()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.OnXp = () => throw new IOException("Ordinary callback failed.");
        Assert.Throws<IOException>(() => f.Complete()); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload); Assert.Equal(0, f.AuthCalls);
        using var nextOwner = QuestSaveScope.AcquireFrozen(new object(), new object(), f.Actor.Identity.ExecutionDatabase, 42);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task ADirectSaveCanFinishAReadyJournalAfterInventoryTemporarilyDelayedTheScheduledSave()
    {
        using var f = new Fixture(options); await f.Actor.Load(); IDisposable inventory = null;
        f.OnXp = () => Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out inventory));
        f.Complete(); Assert.False(f.Actor.Player.IsNormalSaveInProgress); Assert.True(f.Actor.Quests.HasContractCompletionWork);
        Assert.False(f.Actor.Player.Session.CanProcessIncomingPackets); inventory.Dispose();
        await f.Actor.Player.SaveDirect(); f.AssertCommitted(); Assert.False(f.Actor.Quests.HasContractCompletionWork); Assert.True(f.Actor.Player.Session.CanProcessIncomingPackets);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlDuringAuthCannotBecomeAnInventedLoadedBaseline(bool acceptance)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        f.Actor.OnAuth = () =>
        {
            f.AuthCalls++; using var foreign = f.Inner.Context(); var row = foreign.CharacterQuest.Single();
            if (acceptance) row.ContractAcceptanceId = Guid.NewGuid().ToString("N"); else row.Timer = 999;
            foreign.SaveChanges();
        };
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal((byte)QuestState.Achieved, read.CharacterQuest.Single().State); if (!acceptance) Assert.Equal(999u, read.CharacterQuest.Single().Timer);
    }

    [Fact]
    public async Task ADeferredOldGenerationCannotCompleteItsReplacementOrHideTheReplacementsOwnRequest()
    {
        using var f = new Fixture(options); await f.Actor.Load(); Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out var inventory));
        var quest = f.Actor.Quest; string old = quest.ContractAcceptanceId; f.Complete();
        quest.BeginContractAcceptance(retainActive: false, supported: true); string next = quest.ContractAcceptanceId;
        f.Complete(); inventory.Dispose(); await f.Actor.Drain();
        using var read = f.Inner.Context(); Assert.Equal(next, read.CharacterContractCompletion.Single().AcceptanceId); Assert.NotEqual(old, next);
        Assert.Equal(f.Source.BasePoints, read.CharacterContractRewardTrack.Single().Points); Assert.Equal(125ul, read.CharacterCurrency.Single().Amount); Assert.Equal(1, f.XpCalls);
    }

    [Fact]
    public async Task DeferredRequestsAndACompletionCallbackRunInArrivalOrderAcrossIndependentContractGenerations()
    {
        using var f = new Fixture(options); f.AddQuest(9881); f.AddQuest(9883); await f.Actor.Load();
        var order = new List<ushort>();
        foreach (var pair in f.Infos)
        {
            ushort id = pair.Key;
            ((RecordingDispatchProxy<IQuestInfo>)(object)pair.Value).SetMethodHandler(nameof(IQuestInfo.GetRewardExperience), _ => { order.Add(id); return 17u; });
        }
        f.OnXp = () => { if (f.XpCalls == 1) f.Actor.Player.Save(f.Complete); };
        Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out var inventory));
        f.Actor.Quests.QuestComplete(9881, 0, true); f.Actor.Quests.QuestComplete(9883, 0, true); inventory.Dispose(); await f.Actor.Drain();
        Assert.Equal(new ushort[] { 9881, 9883, 9880 }, order);
        using var read = f.Inner.Context(); Assert.Equal(3, read.CharacterContractCompletion.Count()); Assert.Equal(f.Source.BasePoints * 3, read.CharacterContractRewardTrack.Single().Points);
        Assert.Equal(175ul, read.CharacterCurrency.Single().Amount); Assert.Equal(51u, read.Character.Single().TotalXp);
        Assert.Equal(3u, read.Item.Single().StackCount); Assert.All(read.CharacterQuest, row => Assert.Equal((byte)QuestState.Completed, row.State));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task ReentrantChallengeMedalsShareTheContractTransactionAndOrdinaryRewards(int previousMedals)
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        uint[] goals = [10,20,30];
        for (int i = 0; i < previousMedals; i++) { Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, goals[i])); await f.Actor.Drain(); }
        f.OnXp = () =>
        {
            for (int i = previousMedals; i < 3; i++) Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, goals[i]));
            Assert.Equal(1u, f.Actor.Challenges.GetCompletionCount(id));
            Assert.False(f.Actor.Challenges.HasPendingRewardOperation);
            Assert.Equal(previousMedals, MedalResults(f).Count());
        };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count());
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(new[] { (ChallengeResult.TierAchieved, 0), (ChallengeResult.TierAchieved, 1),
            (ChallengeResult.TierAchieved, 2), (ChallengeResult.Completed, 2) }, MedalResults(f).Select(row => (row.Result, row.Data)));
        Assert.Equal((uint)read.CharacterChallengeMedalReceipt.AsEnumerable().Sum(row => row.Points), read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Single(read.Item);
    }

    [Theory]
    [InlineData("auth")] [InlineData("flush")] [InlineData("before-commit")] [InlineData("lost-ack")]
    public async Task ReentrantChallengeCreditCannotBeLostOrPartiallyCommittedWithContractAcknowledgementFailure(string fault)
    {
        using var f = new Fixture(options, fault: fault); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => { foreach (uint count in new uint[] { 10,20,30 }) Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, count)); };
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        Assert.Empty(MedalResults(f));
        using var read = f.Inner.Context();
        if (fault == "lost-ack")
        {
            f.AssertCommitted(); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        }
        else
        {
            Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterChallengeMedalReceipt); Assert.Empty(read.Item);
            Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); Assert.True(read.CharacterChallenge.Single().Activated);
            Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        }
    }

    [Fact]
    public async Task ReentrantActivationAndAllMedalsCreateOneDurableAttemptInsideTheContractUnit()
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.OnXp = () =>
        {
            f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate);
            foreach (uint count in new uint[] { 10,20,30 }) Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, count));
        };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        using var read = f.Inner.Context(); Assert.Single(read.CharacterChallengeAttempt); Assert.True(read.CharacterChallengeAttempt.Single().Terminal);
        Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Fact]
    public async Task ReentrantNonterminalProgressPersistsWithoutInventingAMedalOrAnAttempt()
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, 5));
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterChallengeMedalReceipt); Assert.Single(read.CharacterChallengeAttempt);
        Assert.Equal(5u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReentrantTimerExpiryAndAbandonShareTheirTerminalAttemptWithTheContract(bool timer)
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => { if (timer) f.Actor.Challenges.Update(400); else f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Abandon); };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        using var read = f.Inner.Context(); Assert.False(read.CharacterChallenge.Single().Activated); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount);
        Assert.True(read.CharacterChallengeAttempt.Single().Terminal); Assert.Empty(read.CharacterChallengeMedalReceipt);
    }

    [Theory]
    [InlineData("attempt")] [InlineData("track")] [InlineData("medal")] [InlineData("slot")]
    public async Task OrdinaryEnrollmentCannotRewriteCommandOwnedChallengeTargets(string target)
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => { foreach (uint count in new uint[] { 10,20,30 }) Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, count)); };
        ((RecordingDispatchProxy<IAppearanceManager>)(object)f.Actor.Player.AppearanceManager).SetMethodHandler("Save", args =>
        {
            var context = (CharacterContext)args[0];
            if (target == "attempt") context.CharacterChallengeAttempt.Single().Terminal = false;
            if (target == "track") context.CharacterChallengeRewardTrack.Single().Points++;
            if (target == "medal") context.CharacterChallengeMedalReceipt.First().Points++;
            if (target == "slot") context.CharacterChallengeRewardSlot.First().RewardRowId++;
            return null;
        });
        f.Complete(); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.CharacterChallengeMedalReceipt);
        Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task ChallengeProgressAfterTheAuthSnapshotRetainsItsLaterDirtyIntent()
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, 10));
        f.Actor.OnAuth = () => { f.AuthCalls++; Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, 7)); };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        using (var read = f.Inner.Context()) { Assert.Equal(0u, read.CharacterChallenge.Single().CurrentCount); Assert.Single(read.CharacterChallengeMedalReceipt); }
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect();
        using var final = f.Inner.Context(); Assert.Equal(7u, final.CharacterChallenge.Single().CurrentCount); Assert.Single(final.CharacterChallengeMedalReceipt);
    }

    [Fact]
    public async Task AContractRequestedInsideAChallengeQuestCaptureWaitsBeforeAcquiringItsOwnScope()
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, 10)); await f.Actor.Drain();
        Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, 20)); await f.Actor.Drain();
        int callbacks = 0;
        var capture = f.Actor.Complete(() =>
        {
            f.Complete(); Assert.False(f.Actor.Quests.HasContractCompletionWork);
            f.Actor.Player.Save(() => { callbacks++; f.AssertCommitted(); });
            Assert.Equal(0, f.AuthCalls);
        });
        Assert.True(f.Actor.QueueTerminal(capture)); await f.Actor.Drain(); f.AssertCommitted();
        Assert.Equal(1, callbacks);
        using var read = f.Inner.Context(); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Single(read.CharacterContractCompletion);
    }

    [Fact]
    public async Task ReentrantLegacyDirectChallengePrizesRemainInTheOrdinaryContractSave()
    {
        using var f = new Fixture(options); await f.Actor.Load(); ushort id = f.Inner.Source.Challenge(16);
        var properties = RecordingDispatchProxy<IRewardPropertyManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IRewardPropertyManager.CaptureContractModifier), args =>
        {
            bool accepted = f.Properties.CaptureContractModifier((IPlayer)args[0], (uint)args[1], (uint)args[2], out var modifier); args[3] = modifier; return accepted;
        });
        ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.RewardPropertyManager), properties);
        f.Actor.Challenges.HandleChoice(id, ChallengeChoice.Activate); await f.Actor.Drain();
        f.OnXp = () => { foreach (uint count in new uint[] { 10,20,30 }) Assert.True(f.Actor.Challenges.TryAdvanceProgress(id, count)); };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted();
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterChallengeMedalReceipt);
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.True(read.Item.AsEnumerable().Sum(item => item.StackCount) > 1);
        Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task TheCompanionOwnsChallengePersistenceBeforeOrdinaryRewardCallbacksRun()
    {
        using var f = new Fixture(options); await f.Actor.Load();
        f.OnXp = () =>
        {
            Assert.False(f.Actor.Challenges.TryAcquireRewardPersistence(f.Actor.Identity, [], out var competing)); Assert.Null(competing);
            using var direct = f.Inner.Context(); Assert.Throws<InvalidOperationException>(() => f.Actor.Challenges.Save(direct)); Assert.True(direct.IsSaveRejected);
        };
        f.Complete(); await f.Actor.Drain(); f.AssertCommitted(); Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
    }

    internal sealed class Fixture : IDisposable
    {
        internal readonly PersistenceFixture Inner;
        internal readonly Actor Actor;
        internal readonly RewardPropertyManager Properties;
        internal readonly ContractRewardSource Source;
        internal readonly Quest2Entry Entry;
        internal readonly IGameTableManager Tables;
        internal readonly Dictionary<ushort, IQuestInfo> Infos = [];
        internal int XpCalls, AuthCalls;
        internal Action OnXp;
        internal Func<uint, bool> Eligibility = _ => true;
        private readonly string token;
        private readonly Quest2RewardEntry[] rewards;
        internal Fixture(DbContextOptions<CharacterContext> options, uint id = 9880, string fault = null, bool legacy = false, bool customManager = false,
            Func<PersistenceFixture, CharacterContext> rewardContexts = null, bool claimQueue = false,
            Func<NexusForever.Database.Auth.AuthContext> authContexts = null, NexusForever.Database.Auth.AuthDatabase authDatabase = null,
            NexusForever.Network.Session.IGameSession worldSession = null)
        {
            Inner = new(options); Entry = ContractBuild16042Fixture.Quests().Single(row => row.Id == id);
            Entry.PushedItemIds = []; Entry.PushedItemCounts = []; // fields absent from the source-only scalar fixture
            rewards = ContractBuild16042Fixture.Rewards().Where(row => row.Quest2Id == id).ToArray();
            var modifier = new ContractRewardPropertyTests.Fixture(); var tables = modifier.Tables; Tables = tables;
            var tp = modifier.TableProxy;
            void Table<T>(string property, params T[] rows) where T : class, new() => tp.SetProperty(property, PlayerBindPointTests.Table(rows));
            Table(nameof(IGameTableManager.Quest2), ContractBuild16042Fixture.Quests().Where(row => row.Id != id).Append(Entry).ToArray());
            Table(nameof(IGameTableManager.QuestObjective), ContractBuild16042Fixture.Objectives()); Table(nameof(IGameTableManager.Quest2Reward), ContractBuild16042Fixture.Rewards());
            Table(nameof(IGameTableManager.PeriodicQuestGroup), ContractBuild16042Fixture.Groups()); Table(nameof(IGameTableManager.GameFormula), ContractBuild16042Fixture.Formula());
            Table(nameof(IGameTableManager.RewardTrack), LoyaltyCatalogBuild16042Fixture.Tracks());
            Table(nameof(IGameTableManager.RewardTrackRewards), LoyaltyCatalogBuild16042Fixture.Rewards());
            var trackRows = LoyaltyCatalogBuild16042Fixture.Rewards().Where(row => row.RewardTrackId is 4 or 5).ToArray();
            uint[] itemIds = ContractBuild16042Fixture.Rewards().Where(row => row.Quest2RewardTypeId == 1).Select(row => row.ObjectId)
                .Concat(trackRows.SelectMany(row => new[] { (row.RewardTrackRewardTypeEnum00,row.RewardChoiceId00), (row.RewardTrackRewardTypeEnum01,row.RewardChoiceId01), (row.RewardTrackRewardTypeEnum02,row.RewardChoiceId02) })
                    .Where(choice => choice.Item1 == 0 && choice.Item2 != 0).Select(choice => choice.Item2))
                .Concat(Inner.Source.Tables.RewardTrackRewards.Entries.SelectMany(row => new[] { (row.RewardTrackRewardTypeEnum00,row.RewardChoiceId00), (row.RewardTrackRewardTypeEnum01,row.RewardChoiceId01), (row.RewardTrackRewardTypeEnum02,row.RewardChoiceId02) })
                    .Where(choice => choice.Item1 == 0 && choice.Item2 != 0).Select(choice => choice.Item2)).Distinct().ToArray();
            Table(nameof(IGameTableManager.Item), itemIds.Select(item => new Item2Entry { Id = item, MaxStackCount = 10, MaxCharges = 1 }).ToArray());
            Table(nameof(IGameTableManager.AccountItem), new AccountItemEntry { Id = 2485 }, new AccountItemEntry { Id = 2486 });
            if (claimQueue)
            {
                Table(nameof(IGameTableManager.AccountItem), new AccountItemEntry { Id = 2485, AccountCurrencyEnum = 14, AccountCurrencyAmount = 5 },
                    new AccountItemEntry { Id = 2486, AccountCurrencyEnum = 14, AccountCurrencyAmount = 7 });
                Table(nameof(IGameTableManager.AccountCurrencyType), new AccountCurrencyTypeEntry { Id = 14 });
            }
            Table(nameof(IGameTableManager.CurrencyType), new CurrencyTypeEntry { Id = 1 });
            var ip = (RecordingDispatchProxy<IQuestInfo>)(object)Inner.Info; ip.SetProperty(nameof(IQuestInfo.Entry), Entry); ip.SetMethodReturn(nameof(IQuestInfo.IsContract), true);
            ip.SetMethodReturn(nameof(IQuestInfo.IsCommunicatorReceived), true);
            ip.SetProperty(nameof(IQuestInfo.Rewards), rewards.ToImmutableDictionary(row => row.Id));
            ip.SetMethodReturn(nameof(IQuestInfo.GetRewardMoney), 25u); ip.SetMethodReturn(nameof(IQuestInfo.GetRewardExperience), 17u);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Inner.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), Array.Empty<uint>());
            Infos.Add((ushort)id, Inner.Info);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Inner.Global).SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args => Infos.GetValueOrDefault(Convert.ToUInt16(args[0])));
            var contracts = new ContractManager(tables); Assert.True(contracts.TryGetRewardSource(Inner.Info, out Source));
            token = legacy ? null : Guid.NewGuid().ToString("N");
            using (var seed = Inner.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = (ushort)id, State = (byte)QuestState.Achieved, ContractAcceptanceId = token,
                    QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = (ushort)id, Index = 0, Progress = 5 } } });
                seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); seed.SaveChanges();
            }
            var infos = itemIds.ToDictionary(item => item, item =>
            {
                var info = RecordingDispatchProxy<IItemInfo>.Create(out var p); p.SetProperty(nameof(IItemInfo.Id), item);
                p.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = item, MaxStackCount = 10, MaxCharges = 1 }); p.SetMethodReturn(nameof(IItemInfo.IsStackable), true); return info;
            });
            Inner.Source.ManagerProxy.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => infos.GetValueOrDefault((uint)args[0]));
            var database = new AtomicDatabase(() => Inner.Context(new FlushFault(fault), new CommitFault(fault)));
            var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var pre); pre.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => Eligibility((uint)args[1]));
            Actor = new(Inner, prerequisites, rewardContexts: rewardContexts == null ? null : () => rewardContexts(Inner), questTables: tables,
                contracts: customManager ? RecordingDispatchProxy<IContractManager>.Create(out _) : contracts, characterDatabase: database,
                rewardTables: claimQueue ? tables : null, authContexts: authContexts, authDatabase: authDatabase,
                actorSession: worldSession, actorEvents: worldSession?.Events);
            if (worldSession != null)
            {
                if (worldSession is IWorldSession world)
                    world.Player = Actor.Player;
                ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.Session), worldSession);
            }
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
            var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ap.SetMethodReturn(nameof(IAssetManager.GetRewardPropertiesForTier), ImmutableList<RewardPropertyPremiumModifierEntry>.Empty);
            Properties = new(Actor.Player.Account, assets, tables); ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.RewardPropertyManager), Properties);
            Properties.Initialise(Actor.Player);
            using var read = Inner.Context(); var model = new CharacterModel { Id = 42 }; model.Currency.Add(read.CharacterCurrency.AsNoTracking().Single());
            var currency = new CurrencyManager(tables); currency.Initialise(Actor.Player, model); Actor.Set(nameof(Player.CurrencyManager), currency);
            var xp = new XpManager(Actor.Player, model, tables);
            var xpProxy = (RecordingDispatchProxy<IXpManager>)(object)Actor.Player.XpManager;
            xpProxy.SetMethodHandler(nameof(IXpManager.GrantXp), args => { XpCalls++; xp.GrantXp((uint)args[0], (ExpReason)args[1]); OnXp?.Invoke(); return null; });
            xpProxy.SetMethodHandler(nameof(IXpManager.Save), args => { xp.Save((CharacterContext)args[0]); return null; });
            Actor.OnAuth = () => { AuthCalls++; if (fault == "auth") throw new IOException("Contract ordinary Auth fault."); };
        }
        internal void Complete() => Actor.Quests.QuestComplete((ushort)Entry.Id, 0, true);
        internal void AddQuest(ushort id)
        {
            var entry = ContractBuild16042Fixture.Quests().Single(row => row.Id == id); entry.PushedItemIds = []; entry.PushedItemCounts = [];
            var info = RecordingDispatchProxy<IQuestInfo>.Create(out var ip); ip.SetProperty(nameof(IQuestInfo.Entry), entry);
            ip.SetProperty(nameof(IQuestInfo.Objectives), Inner.Info.Objectives); ip.SetMethodReturn(nameof(IQuestInfo.IsContract), true); ip.SetMethodReturn(nameof(IQuestInfo.IsCommunicatorReceived), true);
            ip.SetProperty(nameof(IQuestInfo.Rewards), ContractBuild16042Fixture.Rewards().Where(row => row.Quest2Id == id).ToImmutableDictionary(row => row.Id));
            ip.SetMethodReturn(nameof(IQuestInfo.GetRewardExperience), 17u); ip.SetMethodReturn(nameof(IQuestInfo.GetRewardMoney), 25u); Infos.Add(id, info);
            var model = new CharacterQuestModel { Id = 42, QuestId = id, State = (byte)QuestState.Achieved, ContractAcceptanceId = Guid.NewGuid().ToString("N"),
                QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = id, Index = 0, Progress = 5 } } };
            using (var seed = Inner.Context()) { seed.CharacterQuest.Add(model); seed.SaveChanges(); }
            var quest = new Game.Quest.Quest(Actor.Player, info, model, globalQuestManager: Inner.Global, scriptManager: Inner.Scripts, gameTableManager: Tables);
            var active = (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(Actor.Quests)!;
            active.Add(id, quest);
        }
        internal void AssertCommitted()
        {
            using var read = Inner.Context(); Assert.Equal((byte)QuestState.Completed, read.CharacterQuest.Single().State);
            Assert.Equal(token, read.CharacterContractCompletion.Single().AcceptanceId);
            Assert.Equal(Source.BasePoints, read.CharacterContractRewardTrack.Single().Points);
            foreach (var reward in rewards) Assert.Equal(reward.ObjectAmount, read.Item.Where(item => item.ItemId == reward.ObjectId).AsEnumerable().Sum(item => item.StackCount));
            Assert.Equal(125ul, read.CharacterCurrency.Single().Amount); Assert.Equal(17u, read.Character.Single().TotalXp); Assert.Equal(1, XpCalls);
        }
        public void Dispose() { Actor.Dispose(); Inner.Dispose(); }
    }
    private sealed class AtomicDatabase(Func<CharacterContext> contexts) : CharacterDatabase
    { public override Task SaveAtomic(ulong id, Action<CharacterContext> action) => CharacterAtomicSave.Execute(contexts, id, action); }
    private sealed class FlushFault(string fault) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => fault == "flush" ? throw new IOException("Contract ordinary flush fault.") : new(result);
    }
    private sealed class CommitFault(string fault) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => fault == "before-commit" ? throw new IOException("Contract ordinary commit fault.") : new(result);
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => fault == "lost-ack" ? Task.FromException(new IOException("Contract ordinary acknowledgement lost.")) : Task.CompletedTask;
    }
}
