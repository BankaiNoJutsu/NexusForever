using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Reward;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeLiveProducerTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ChallengeLiveProducerTests() { }
    internal ChallengeLiveProducerTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task RealActivationAndThreeMedalsFreezeEachModifierAndCompleteQuestOnceWithoutTheDirectPrize(uint track)
    {
        using var f = New(); PrepareDirectPrize(f, track); using var h = new Harness(f); await h.Load();
        h.Activate(track); string attempt = Runtime(h.Actor.Challenges, f.Source.Challenge(track)).ActiveAttemptId; Assert.NotNull(attempt);
        h.Multiplier = 1.25f; h.Revision = 7; h.Progress(track, 10);
        h.Multiplier = 2; h.Revision = 8; h.Progress(track, 20);
        h.Multiplier = .5f; h.Revision = 9; h.Progress(track, 30);
        Assert.Equal(2u, h.Actor.Quest.Single().Progress); Assert.True(h.Actor.Challenges.HasPendingRewardOperation);
        Assert.False(h.Actor.Challenges.TryAdvanceProgress(f.Source.Challenge(track), 1));
        await h.Actor.Drain(); Assert.False(h.Actor.Challenges.RequiresAuthoritativeReload); Assert.False(h.Actor.Quests.RequiresAuthoritativeReload);
        using var read = f.Context(); var row = read.CharacterChallenge.Single();
        Assert.Equal(attempt, row.ActiveAttemptId); Assert.False(row.Activated); Assert.Equal(1u, row.CompletionCount);
        Assert.Equal(375u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(new uint[] { 125, 200, 50 }, read.CharacterChallengeMedalReceipt.OrderBy(row => row.Medal).Select(row => row.Points));
        Assert.Equal(new long[] { 7, 8, 9 }, read.CharacterChallengeMedalReceipt.OrderBy(row => row.Medal).Select(row => row.ModifierRevision));
        var durable = read.CharacterChallengeAttempt.Single(); Assert.Equal((byte)CharacterChallengeRewardMode.CharacterTrack, durable.RewardMode);
        Assert.True(durable.Terminal); Assert.Equal(7u, durable.EarnedMedalMask); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Empty(read.Item); Assert.Empty(f.Source.Inventory.Single(bag => bag.Location == InventoryLocation.Inventory));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task TheActualCombatAutoActivationProducerCreditsOnlyReachedSourceTiers(int tiers)
    {
        using var f = New(); var entry = f.Source.Tables.Challenge.GetEntry(f.Source.Challenge(16));
        entry.ChallengeFlags |= 0x20; entry.Target = 98765;
        if (tiers < 3) entry.ChallengeTierId02 = 0; if (tiers < 2) entry.ChallengeTierId01 = 0;
        using var h = new Harness(f); await h.Load();
        foreach (var tier in f.Source.Tables.ChallengeTier.Entries) tier.Count = 1;
        for (int i = 0; i < tiers; ++i) h.Actor.Challenges.TryAdvanceCombatKill(98765);
        await h.Actor.Drain(); using var read = f.Context();
        Assert.Equal((uint)tiers * 100, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(tiers, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal((uint)(1 << tiers) - 1, read.CharacterChallengeAttempt.Single().EarnedMedalMask);
        Assert.True(read.CharacterChallengeAttempt.Single().Terminal); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task ANewLegacyModeStaysFrozenWhenModifiersBecomeAvailableAndKeepsItsDirectPrize()
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 16); using var h = new Harness(f); await h.Load();
        h.ModifierAvailable = false; h.Activate(16); h.ModifierAvailable = true;
        h.Complete(16); await h.Actor.Drain();
        using (var read = f.Context())
        {
            var attempt = read.CharacterChallengeAttempt.Single(); Assert.Equal((byte)CharacterChallengeRewardMode.LegacyDirect, attempt.RewardMode);
            Assert.True(attempt.Terminal); Assert.Equal(0u, attempt.EarnedMedalMask); Assert.Empty(read.CharacterChallengeMedalReceipt);
            Assert.Empty(read.CharacterChallengeRewardTrack); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        }
        Assert.Equal(item, Assert.Single(f.Source.Inventory.Single(bag => bag.Location == InventoryLocation.Inventory)).Info.Id);
        await h.Actor.Player.SaveDirect(); using var final = f.Context(); Assert.Equal(item, Assert.Single(final.Item).ItemId);
    }

    [Fact]
    public async Task HistoricalActiveNullMarkersStayLegacyAndNeverBackfillAttemptsOrMedals()
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 16);
        using (var seed = f.Context()) { seed.CharacterChallenge.Add(f.Source.Activated(16).ToModel()); seed.SaveChanges(); }
        using var h = new Harness(f); await h.Load(); h.Complete(16); await h.Actor.Drain(); await h.Actor.Player.SaveDirect();
        using var read = f.Context(); Assert.Null(read.CharacterChallenge.Single().ActiveAttemptId); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeMedalReceipt); Assert.Empty(read.CharacterChallengeRewardTrack);
        Assert.Equal(item, Assert.Single(read.Item).ItemId); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ImmediateRestartCarriesOnlyTheUnsavedHistoricalLegacyStateIntoANewDurableAttempt(bool completed)
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 16); var fault = new LostAck();
        using (var seed = f.Context()) { seed.CharacterChallenge.Add(f.Source.Activated(16).ToModel()); seed.SaveChanges(); }
        using var h = new Harness(f, () => f.Context(fault)); await h.Load();
        if (completed) h.Complete(16); else h.Actor.Challenges.HandleChoice(f.Source.Challenge(16), ChallengeChoice.Abandon);
        using (var before = f.Context()) { Assert.True(before.CharacterChallenge.Single().Activated); Assert.Equal(0u, before.CharacterChallenge.Single().CompletionCount); }
        fault.Enabled = true; h.Activate(16); h.Complete(16); await h.Actor.Drain(); await h.Actor.Player.SaveDirect();
        Assert.True(fault.Fired); using var read = f.Context();
        Assert.Single(read.CharacterChallengeAttempt); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(300u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(completed ? 2u : 1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(completed ? 3u : 2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(completed ? new[] { item } : Array.Empty<uint>(), read.Item.Select(row => row.ItemId));
    }

    [Fact]
    public async Task LegacyActivationCarryCannotStealAnOwnerCreateAMarkerOrSkipCompletionHistory()
    {
        using var f = New(); var before = f.Source.Activated(16);
        using (var seed = f.Context()) { seed.CharacterChallenge.Add(before.ToModel()); seed.SaveChanges(); }
        var legacy = before with { Activated = false, ActiveSeconds = 0, Completions = 1 };
        foreach (var invalid in new[] { legacy with { CharacterId = 43 }, legacy with { ChallengeId = f.Source.Challenge(17) },
            legacy with { AttemptId = Guid.NewGuid().ToString("N") }, legacy with { Activated = true }, legacy with { Completions = 2 } })
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Source.Execute(repo => repo.BeginAttemptFromLegacy(
                before, invalid, before with { Completions = 1 }, Guid.NewGuid().ToString("N"), true)));
        }
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack);
        Assert.True(read.CharacterChallenge.Single().Activated); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Theory]
    [InlineData(29u)] [InlineData(30u)] [InlineData(31u)] [InlineData(32u)] [InlineData(34u)]
    public async Task EmptyCatalogActivationAndCompletionRetainTheLegacyNullPath(uint track)
    {
        using var f = New(); using var h = new Harness(f); await h.Load(); h.Activate(track); h.Complete(track); await h.Actor.Drain(); await h.Actor.Player.SaveDirect();
        using var read = f.Context(); Assert.Null(read.CharacterChallenge.Single().ActiveAttemptId); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
        Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack); Assert.Empty(read.CharacterChallengeMedalReceipt);
        Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData("time")] [InlineData("hole")] [InlineData("missing-tier")]
    public async Task UnvalidatedSourceShapesDoNotOptIntoTrackEarning(string shape)
    {
        using var f = New(); var entry = f.Source.Tables.Challenge.GetEntry(f.Source.Challenge(16));
        if (shape == "time") entry.ChallengeFlags |= 8;
        if (shape == "hole") entry.ChallengeTierId01 = 0;
        if (shape == "missing-tier") entry.ChallengeTierId01 = uint.MaxValue;
        using var h = new Harness(f); await h.Load(); h.Activate(16);
        Assert.Null(Runtime(h.Actor.Challenges, f.Source.Challenge(16)).ActiveAttemptId);
        h.Actor.Challenges.HandleChoice(f.Source.Challenge(16), ChallengeChoice.Abandon); await h.Actor.Drain(); await h.Actor.Player.SaveDirect();
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack);
    }

    [Theory]
    [InlineData("abandon", 0)] [InlineData("abandon", 1)] [InlineData("abandon", 2)]
    [InlineData("timeout", 0)] [InlineData("timeout", 1)] [InlineData("timeout", 2)]
    [InlineData("area", 0)] [InlineData("area", 1)] [InlineData("area", 2)]
    public async Task NonCompletingTerminationKeepsOnlyEarnedMedalsAndDoesNotRunCompletionEffects(string termination, int medals)
    {
        using var f = New(); using var h = new Harness(f); await h.Load(); h.Activate(16);
        if (medals > 0) h.Progress(16, 10); if (medals > 1) h.Progress(16, 20);
        if (termination == "abandon") h.Actor.Challenges.HandleChoice(f.Source.Challenge(16), ChallengeChoice.Abandon);
        if (termination == "timeout") h.Actor.Challenges.Update(301);
        if (termination == "area") { var row = Runtime(h.Actor.Challenges, f.Source.Challenge(16)); row.LeftArea = true; row.AreaFailTimer = 1; h.Actor.Challenges.Update(2); }
        await h.Actor.Drain(); using var read = f.Context();
        Assert.True(read.CharacterChallengeAttempt.Single().Terminal); Assert.False(read.CharacterChallenge.Single().Activated);
        Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal((uint)medals * 100, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(medals, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task ImmediateRestartsRetainTheirQueuedLineageAndThreeDistinctCompletionEvents()
    {
        using var f = New(); using var h = new Harness(f); await h.Load(); var identities = new HashSet<string>();
        for (int i = 0; i < 3; ++i) { h.Activate(16); Assert.True(identities.Add(Runtime(h.Actor.Challenges, f.Source.Challenge(16)).ActiveAttemptId)); h.Complete(16); }
        Assert.Equal(4u, h.Actor.Quest.Single().Progress); await h.Actor.Drain();
        using var read = f.Context(); Assert.Equal(3, read.CharacterChallengeAttempt.Count()); Assert.All(read.CharacterChallengeAttempt, row => Assert.True(row.Terminal));
        Assert.Equal(9, read.CharacterChallengeMedalReceipt.Count()); Assert.Equal(900u, read.CharacterChallengeRewardTrack.Single().Points);
        Assert.Equal(3u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(4u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(3u, Runtime(h.Actor.Challenges, f.Source.Challenge(16)).CompletionCount);
    }

    [Fact]
    public async Task RealNormalSaveAuthGapCanContainActivationAndAllMedalsWithoutLeakingCompletionIntoItsOlderTarget()
    {
        using var f = New(); using var h = new Harness(f); await h.Load();
        h.Actor.OnAuth = () => { h.Activate(16); h.Complete(16); };
        await h.Actor.Player.SaveDirect();
        using (var older = f.Context()) { Assert.Empty(older.CharacterChallenge); Assert.Equal(1u, older.CharacterQuestObjective.Single().Progress); Assert.Empty(older.CharacterChallengeAttempt); }
        h.Actor.OnAuth = null; await h.Actor.Drain(); using var read = f.Context();
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.False(h.Actor.Quests.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task LaterLiveProgressAndTimersSurviveEarlierMedalAdoptionAndSaveThroughTheNormalPath()
    {
        using var f = New(); using var h = new Harness(f); await h.Load(); h.Activate(16); h.Progress(16, 10); h.Progress(16, 3); h.Actor.Challenges.Update(2.25);
        await h.Actor.Drain(); var row = Runtime(h.Actor.Challenges, f.Source.Challenge(16)); Assert.Equal(3u, row.CurrentCount); Assert.Equal(297.75, row.ActiveTimer);
        using (var before = f.Context()) { Assert.Equal(0u, before.CharacterChallenge.Single().CurrentCount); Assert.Equal(300d, before.CharacterChallenge.Single().ActiveTimerSeconds); }
        await h.Actor.Player.SaveDirect(); using var read = f.Context(); Assert.Equal(3u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(297.75, read.CharacterChallenge.Single().ActiveTimerSeconds);
    }

    [Fact]
    public async Task ATrackedAttemptCannotFallBackToADirectPrizeWhenItsModifierOwnerBecomesUnavailable()
    {
        using var f = New(); PrepareDirectPrize(f, 16); using var h = new Harness(f); await h.Load(); h.Activate(16); h.Progress(16, 10); await h.Actor.Drain();
        h.ModifierAvailable = false; h.Progress(16, 20); await h.Actor.Drain();
        Assert.True(h.Actor.Challenges.RequiresAuthoritativeReload); Assert.True(h.Actor.Quests.RequiresAuthoritativeReload);
        Assert.NotEmpty(h.Session.GetInvocations(nameof(IGameSession.ForceDisconnect))); using var read = f.Context();
        Assert.Single(read.CharacterChallengeMedalReceipt); Assert.Equal(100u, read.CharacterChallengeRewardTrack.Single().Points); Assert.Empty(read.Item);
        Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task LostAcknowledgementKeepsTheLiveCommandAndNeverReplaysCompletionOrDirectPrizes(bool legacy, bool terminal)
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 16); var fault = new LostAck();
        using var h = new Harness(f, () => f.Context(fault)); await h.Load();
        h.ModifierAvailable = !legacy; h.Activate(16); await h.Actor.Drain();
        if (terminal) { h.Progress(16, 10); h.Progress(16, 20); await h.Actor.Drain(); }
        fault.Enabled = true;
        if (terminal) h.Progress(16, 30); else h.Complete(16);
        await h.Actor.Drain();
        Assert.True(fault.Fired); Assert.False(h.Actor.Challenges.RequiresAuthoritativeReload); Assert.Equal(2u, h.Actor.Quest.Single().Progress);
        using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt); Assert.Equal(legacy ? 0 : 3, read.CharacterChallengeMedalReceipt.Count());
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(legacy ? new[] { item } : Array.Empty<uint>(), f.Source.Inventory.Single(bag => bag.Location == InventoryLocation.Inventory).Select(row => row.Info.Id));
    }

    [Fact]
    public async Task AHistoricalLegacyCompletionDuringTrackedQueueWorkKeepsItsDirectPrizeAndBothQuestEvents()
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 17); PrepareDirectPrize(f, 16);
        f.Source.Tables.Challenge.GetEntry(f.Source.Challenge(17)).ChallengeTypeEnum = 1;
        using (var seed = f.Context()) { seed.CharacterChallenge.Add(f.Source.Activated(17).ToModel()); seed.SaveChanges(); }
        using var h = new Harness(f); await h.Load(); h.Activate(16); h.Complete(17); h.Complete(16); await h.Actor.Drain();
        await h.Actor.Player.SaveDirect(); using var read = f.Context();
        Assert.Equal(2, read.CharacterChallenge.Count(row => row.CompletionCount == 1)); Assert.Equal(3u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count()); Assert.Single(read.CharacterChallengeAttempt);
        Assert.Equal(item, Assert.Single(read.Item).ItemId); Assert.Null(read.CharacterChallenge.Single(row => row.ChallengeId == f.Source.Challenge(17)).ActiveAttemptId);
    }

    [Fact]
    public async Task AHistoricalLegacyPrizeUsesUnreservedCapacityWhileARealClaimRetainsItsDeliverySlot()
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 17);
        f.Source.Tables.Challenge.GetEntry(f.Source.Challenge(17)).ChallengeTypeEnum = 1;
        using (var seed = f.Context()) { seed.CharacterChallenge.Add(f.Source.Activated(17).ToModel()); seed.SaveChanges(); }
        using var h = new Harness(f); await h.Load(); h.Multiplier = f.Source.Catalog.GetTrack(16).MaximumPoints / 100f + 10;
        h.Activate(16); h.Complete(16); await h.Actor.Drain();
        var slot = h.Actor.Challenges.CommittedCharacterRewards.Tracks.Single().Slots[0];
        using (var cache = f.Source.Reserve(slot.RewardRowId, 0)) Assert.True(cache.Cancel());
        h.DispatchClaim(slot.RewardRowId, 0); Assert.True(f.Source.Inventory.HasPendingRewardDelivery);
        h.Complete(17); await h.Actor.Drain(); await h.Actor.Player.SaveDirect();
        using var read = f.Context(); Assert.Single(read.CharacterChallengeRewardClaim); Assert.Equal(2, read.Item.Count());
        Assert.Contains(read.Item, row => row.ItemId == item); Assert.Equal(3u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(2, read.CharacterChallenge.Count(row => row.CompletionCount == 1)); Assert.False(f.Source.Inventory.HasPendingRewardDelivery);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ANewActorResumesTheDurableAttemptModeProgressAndSelectionsWithoutResampling(bool legacy)
    {
        using var f = New(); uint item = PrepareDirectPrize(f, 16); string attempt; uint[] choices;
        using (var first = new Harness(f))
        {
            await first.Load(); first.ModifierAvailable = !legacy; first.Multiplier = 1.25f; first.Activate(16); first.Progress(16, 10); first.Progress(16, 3);
            await first.Actor.Drain(); await first.Actor.Player.SaveDirect();
            attempt = Runtime(first.Actor.Challenges, f.Source.Challenge(16)).ActiveAttemptId;
            choices = first.Actor.Challenges.CommittedCharacterRewards.Tracks.SelectMany(row => row.Slots).Select(row => row.RewardRowId).ToArray();
        }
        using var second = new Harness(f); await second.Load(); Assert.Equal(attempt, Runtime(second.Actor.Challenges, f.Source.Challenge(16)).ActiveAttemptId);
        Assert.Equal(3u, Runtime(second.Actor.Challenges, f.Source.Challenge(16)).CurrentCount);
        Assert.Equal(choices, second.Actor.Challenges.CommittedCharacterRewards.Tracks.SelectMany(row => row.Slots).Select(row => row.RewardRowId));
        second.Multiplier = 2; second.Revision = 8; second.Progress(16, 17); second.Multiplier = .5f; second.Revision = 9; second.Progress(16, 30);
        await second.Actor.Drain(); await second.Actor.Player.SaveDirect(); using var read = f.Context();
        Assert.Single(read.CharacterChallengeAttempt); Assert.True(read.CharacterChallengeAttempt.Single().Terminal);
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal(legacy ? 0 : 3, read.CharacterChallengeMedalReceipt.Count());
        if (legacy) Assert.Equal(item, Assert.Single(read.Item).ItemId);
        else { Assert.Equal(375u, read.CharacterChallengeRewardTrack.Single().Points); Assert.Empty(read.Item); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AFailedQuestPreparationDoesNotCommitCompletionOrAnUntrackedLegacyPrize(bool legacy)
    {
        using var f = New(); PrepareDirectPrize(f, 16); var fault = new ProbeFault();
        using var h = new Harness(f, () => f.Context(fault)); await h.Load(); h.ModifierAvailable = !legacy; h.Activate(16); h.Progress(16, 10); h.Progress(16, 20); await h.Actor.Drain();
        fault.Enabled = true; h.Progress(16, 30); await h.Actor.Drain();
        Assert.True(fault.Fired); Assert.True(h.Actor.Challenges.RequiresAuthoritativeReload); Assert.True(h.Actor.Quests.RequiresAuthoritativeReload);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Actor.Player.SaveDirect());
        using var read = f.Context(); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); Assert.False(read.CharacterChallengeAttempt.Single().Terminal);
        Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Empty(read.Item);
        Assert.Equal(legacy ? 0 : 2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task RealLiveEarningFeedsRegisteredChooseClaimHandlersAndResetsOneCycleWithoutReearning()
    {
        using var f = New(); using var h = new Harness(f); await h.Load();
        h.Multiplier = f.Source.Catalog.GetTrack(16).MaximumPoints / 100f + 10; h.Activate(16); h.Complete(16); await h.Actor.Drain();
        uint earned = h.Actor.Challenges.CommittedCharacterRewards.Tracks.Single().Points;
        var track = h.Actor.Challenges.CommittedCharacterRewards.Tracks.Single(); ulong cycle = track.CycleId;
        h.DispatchChoose(16); await h.Actor.Drain();
        foreach (var slot in track.Slots)
        {
            using (var cache = f.Source.Reserve(slot.RewardRowId, 0)) Assert.True(cache.Cancel());
            h.DispatchClaim(slot.RewardRowId, 0); await h.Actor.Drain();
        }
        using var read = f.Context(); var current = read.CharacterChallengeRewardTrack.Single();
        Assert.Equal(cycle + 1, current.CycleId); Assert.Equal(earned - f.Source.Catalog.GetTrack(16).MaximumPoints, current.Points);
        Assert.Equal(track.Slots.Count, read.CharacterChallengeRewardClaim.Count()); Assert.Equal(3, read.CharacterChallengeMedalReceipt.Count());
        Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.NotEmpty(read.Item);
    }

    private Fixture New() { var f = new Fixture(options); f.Seed(); return f; }
    private static uint PrepareDirectPrize(Fixture f, uint track)
    {
        var row = f.Source.Tables.RewardTrackRewards.Entries.Where(row => row.RewardTrackId == track && (row.RewardPointFlags & 4) != 0).OrderBy(row => row.Id).First();
        using var held = f.Source.Reserve(row.Id, 0); Assert.True(held.Cancel()); return held.ItemId;
    }

    private sealed class Harness : IDisposable
    {
        internal readonly Fixture F;
        internal readonly Actor Actor;
        internal readonly IWorldSession World;
        internal readonly RecordingDispatchProxy<IWorldSession> Session;
        internal float Multiplier = 1;
        internal long Revision = 1;
        internal bool ModifierAvailable = true;
        private readonly MessageManager registry;
        internal Harness(Fixture f, Func<CharacterContext> contexts = null)
        {
            F = f;
            var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var pp); pp.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
            Actor = new(f, prerequisites, contexts);
            var modifiers = RecordingDispatchProxy<IRewardPropertyManager>.Create(out var mp);
            mp.SetMethodHandler(nameof(IRewardPropertyManager.CaptureChallengeModifier), args => { args[0] = new ChallengeRewardModifierSnapshot(Multiplier, Revision); return ModifierAvailable; });
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.RewardPropertyManager), modifiers);
            World = RecordingDispatchProxy<IWorldSession>.Create(out var session); Session = session;
            session.SetProperty(nameof(IWorldSession.Player), Actor.Player); session.SetProperty(nameof(IWorldSession.Account), Actor.Player.Account); session.SetProperty("Events", Actor.Events);
            typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(Actor.Player, World);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.Session), World);
            typeof(Inventory).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(f.Source.Inventory, Actor.Player);
            registry = new(NullLogger<MessageManager>.Instance);
            foreach (var type in typeof(ClientRewardTrackChooseTrack).Assembly.GetTypes()) registry.RegisterMessage(type);
            registry.RegisterMessageHandler(typeof(ClientRewardTrackChooseTrackHandler)); registry.RegisterMessageHandler(typeof(ClientRewardTrackClaimRewardPointHandler));
        }
        internal async Task Load() { var load = Actor.Load(); Assert.True(Actor.Player.RewardTrackPresentation.PublishAccountTracks([])); await load; }
        internal void Activate(uint track) => Actor.Challenges.HandleChoice(F.Source.Challenge(track), ChallengeChoice.Activate);
        internal void Progress(uint track, uint value) => Assert.True(Actor.Challenges.TryAdvanceProgress(F.Source.Challenge(track), value));
        internal void Complete(uint track) { Progress(track, 10); Progress(track, 20); Progress(track, 30); }
        internal void DispatchChoose(uint track) => Dispatch(new ClientRewardTrackChooseTrackHandler(F.Source.Tables), 0x07CF, writer => writer.Write(track, 14));
        internal void DispatchClaim(uint row, uint choice) => Dispatch(new ClientRewardTrackClaimRewardPointHandler(F.Source.Tables), 0x07CE, writer => { writer.Write(row, 14); writer.Write(choice); });
        private void Dispatch(object handler, uint opcode, Action<GamePacketWriter> payload)
        {
            using var bytes = new MemoryStream(); using (var writer = new GamePacketWriter(bytes)) { payload(writer); writer.FlushBits(); }
            using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray())); var message = (IReadable)Activator.CreateInstance(registry.GetMessageType((GameMessageOpcode)opcode))!; message.Read(reader);
            registry.GetMessageHandlerDelegate((GameMessageOpcode)opcode)(handler, World, message);
        }
        public void Dispose() => Actor.Dispose();
    }
    private sealed class LostAck : DbTransactionInterceptor
    {
        internal bool Enabled, Fired;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Enabled && !Fired && eventData.Context?.ChangeTracker.Entries().Any() == true)
            { Fired = true; return Task.FromException(new IOException("Live producer acknowledgement lost.")); }
            return Task.CompletedTask;
        }
    }
    private sealed class ProbeFault : DbTransactionInterceptor
    {
        internal bool Enabled, Fired;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Enabled && !Fired && eventData.Context?.ChangeTracker.Entries().Any() == false)
            { Fired = true; return Task.FromException(new IOException("Live Quest preparation unavailable.")); }
            return Task.CompletedTask;
        }
    }
}
