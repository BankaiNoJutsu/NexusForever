using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractLiveCompletionTests
{
    [Fact]
    public async Task LaterQuestAndCurrencyIntentWaitForTheClaimAndThenSaveWithoutReplacingItsCommittedCredit()
    {
        var gate = new QueueClaimPause();
        using var f = new Fixture(options, rewardContexts: inner => inner.Context(gate), claimQueue: true); uint reward = await SeedQueuedClaim(f);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 0)); await AwaitClaimGate(f, gate);
        f.Actor.Quest.Timer = 901; f.Actor.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, 7);
        Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, 0));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Actor.Player.SaveDirect()); Assert.Equal(0, f.AuthCalls);
        gate.Release.TrySetResult(true); await f.Actor.Drain();
        using (var read = f.Inner.Context()) { Assert.Null(read.CharacterQuest.Single().Timer); Assert.Equal(2500100ul, read.CharacterCurrency.Single().Amount); }
        Assert.Equal(2500107ul, f.Actor.Player.CurrencyManager.Single().Amount); Assert.True(f.Actor.Quest.NeedsPersistenceSave);
        await f.Actor.Player.SaveDirect();
        using var final = f.Inner.Context(); Assert.Equal(2500107ul, final.CharacterCurrency.Single().Amount); Assert.Equal(901u, final.CharacterQuest.Single().Timer);
        Assert.Single(final.CharacterContractRewardClaim); Assert.Equal(1, f.AuthCalls); Assert.Equal(0, f.XpCalls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidatedAndDeadActorsReleaseOnlyTerminalBookkeepingWithoutPublishingCommittedItemsOrMoney(bool final)
    {
        var gate = new QueueClaimPause();
        using var f = new Fixture(options, rewardContexts: inner => inner.Context(gate), claimQueue: true); uint reward = await SeedQueuedClaim(f);
        int messages = f.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count;
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 0)); await AwaitClaimGate(f, gate);
        f.Actor.Quests.InvalidateRewardOperations(final); f.Actor.Challenges.InvalidateRewardOperations(final);
        gate.Release.TrySetResult(true);
        if (final)
        {
            var wait = System.Diagnostics.Stopwatch.StartNew();
            while (f.Actor.Challenges.HasPendingRewardOperation || f.Actor.Quests.HasPendingRewardOperation)
            { Assert.True(wait.Elapsed < TimeSpan.FromSeconds(20), "Final claim bookkeeping did not settle without polling session events."); await Task.Delay(5); }
        }
        else await f.Actor.Drain(allowErrors: true);
        using var read = f.Inner.Context(); Assert.Single(read.CharacterContractRewardClaim); Assert.Equal(2500100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(100ul, f.Actor.Player.CurrencyManager.Single().Amount); Assert.Equal(messages, f.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count);
        Assert.True(f.Actor.Quests.RequiresAuthoritativeReload); Assert.True(Assert.IsType<CurrencyManager>(f.Actor.Player.CurrencyManager).RequiresContractCurrencyReload);
    }

    private static async Task AwaitClaimGate(Fixture f, QueueClaimPause gate)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!gate.Entered.Task.IsCompleted)
        { f.Actor.Events.Update(.01); Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(20), "Claim did not enter the owned commit gate."); await Task.Delay(5); }
    }

    private sealed class QueueClaimPause : DbTransactionInterceptor
    {
        internal readonly TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int fired;
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (data.Context is CharacterContext context && context.CharacterContractRewardClaim.Local.Any() && Interlocked.CompareExchange(ref fired, 1, 0) == 0)
            { Entered.TrySetResult(true); await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken); }
            return result;
        }
    }

    internal static async Task<uint> SeedQueuedClaim(Fixture f, uint trackId = 5, bool final = true)
    {
        ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Id), 27u);
        f.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), f.Actor.Quests);
        f.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.CurrencyManager), f.Actor.Player.CurrencyManager);
        var catalog = new ContractRewardCatalog(f.Tables); var track = catalog.GetTrack(trackId);
        var eligible = track.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Append(track.PrerequisiteId).Where(id => id != 0).Distinct().ToArray();
        var selection = ContractRewardSelection.CaptureForChoose(catalog, trackId, 7, Guid.NewGuid().ToString("N"), eligible, _ => 0);
        using (var seed = f.Inner.Context())
        {
            seed.Character.ExecuteUpdate(set => set.SetProperty(row => row.AccountId, 27u));
            seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = trackId, CycleId = 7, Points = track.MaximumPoints + 17, Revision = 3, Active = true, SelectionInitialized = true });
            seed.CharacterContractRewardSlot.AddRange(track.Slots.Select(slot => new CharacterContractRewardSlotModel
                { Id = 42, TrackId = trackId, CycleId = 7, Ordinal = slot.Ordinal, RewardRowId = selection.Rewards[slot.Ordinal], RequiredPoints = slot.RequiredPoints, SelectionJson = selection.Json }));
            seed.SaveChanges();
        }
        await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([new() { RewardTrackId = 9, RewardPointsEarned = 800, IsActive = true }]));
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
        return selection.Rewards[final ? track.Slots.Count - 1 : 0];
    }

    [Theory]
    [InlineData(4u, false, 0u)] [InlineData(4u, false, 1u)] [InlineData(4u, false, 2u)]
    [InlineData(4u, true, 0u)] [InlineData(4u, true, 1u)] [InlineData(4u, true, 2u)]
    [InlineData(5u, false, 0u)] [InlineData(5u, false, 1u)] [InlineData(5u, false, 2u)]
    [InlineData(5u, true, 0u)] [InlineData(5u, true, 1u)] [InlineData(5u, true, 2u)]
    public async Task TheRegisteredActorQueueSettlesAllCanonicalContractChoiceKindsBeforePublishingClaimedState(uint trackId, bool final, uint choice)
    {
        using var f = new Fixture(options, claimQueue: true); uint reward = await SeedQueuedClaim(f, trackId, final);
        var award = new ContractRewardAwardCatalog(f.Tables).GetAward(reward, choice); var before = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        int reloads = Reloads(f).Length; ulong items;
        using (var read = f.Inner.Context()) items = read.Item.AsEnumerable().Aggregate(0ul, (sum, row) => sum + row.StackCount);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, choice)); Assert.True(f.Actor.Quests.HasPendingRewardOperation);
        Assert.Same(before, Assert.Single(f.Actor.Quests.CommittedContractTracks)); Assert.Equal(reloads, Reloads(f).Length);
        await f.Actor.Drain(); Assert.False(f.Actor.Quests.HasPendingRewardOperation);
        var after = Assert.Single(f.Actor.Quests.CommittedContractTracks); Assert.True(after.Slots[award.Ordinal].Claimed);
        Assert.Equal(before.Revision + 1, after.Revision); Assert.Equal(before.Points, after.Points); Assert.Equal(before.CycleId, after.CycleId);
        Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, choice));
        using var verify = f.Inner.Context(); var receipt = Assert.Single(verify.CharacterContractRewardClaim); Assert.Equal(27u, receipt.AccountId);
        Assert.Empty(verify.CharacterChallengeRewardClaim); Assert.Empty(verify.CharacterContractCompletion);
        Assert.Equal(items + (award.Item?.Count ?? 0), verify.Item.AsEnumerable().Aggregate(0ul, (sum, row) => sum + row.StackCount));
        Assert.Equal(award.AccountCurrency == null ? 0 : 1, verify.CharacterContractAccountOutbox.Count());
        Assert.Equal(final ? 2500100ul : 100ul, verify.CharacterCurrency.Single().Amount);
        Assert.Equal(final ? 2500100ul : 100ul, f.Actor.Player.CurrencyManager.Single().Amount);
        Assert.False(Assert.IsType<CurrencyManager>(f.Actor.Player.CurrencyManager).HasPendingContractCurrency);
        Assert.Equal(800u, Reloads(f).Last().RewardsTracks.Single(row => row.RewardTrackId == 9).RewardPointsEarned);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
        Assert.Equal(0, f.AuthCalls); Assert.Equal(0, f.XpCalls);
    }

    [Theory]
    [InlineData("unpublished")] [InlineData("busy")] [InlineData("inventory")] [InlineData("invalidated")]
    [InlineData("foreign-session")] [InlineData("unknown")] [InlineData("choice")] [InlineData("unselected")]
    [InlineData("points")] [InlineData("inactive")] [InlineData("dirty-wallet")] [InlineData("missing-item")]
    [InlineData("ineligible")] [InlineData("cap")]
    public async Task UnownedOrUnpublishedQueuedContractClaimsDoNotEnrollMoneyItemsOrOutbox(string reason)
    {
        using var f = new Fixture(options, claimQueue: true); uint reward = await SeedQueuedClaim(f, final: reason != "ineligible" && reason != "unselected"); IDisposable held = null;
        var before = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        if (reason == "unpublished") f.Actor.Player.RewardTrackPresentation.Invalidate();
        if (reason == "busy") f.Actor.StartSave();
        if (reason == "inventory") Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out held));
        if (reason == "invalidated") f.Actor.Quests.InvalidateRewardOperations();
        if (reason == "foreign-session") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (reason == "points" || reason == "inactive")
        {
            f.Actor.Quests.AdoptContractRewards([before with { Revision = 4, Points = reason == "points" ? 0u : before.Points, Active = reason != "inactive" }]);
            Assert.True(f.Actor.Quests.PublishContractRewards());
        }
        if (reason == "dirty-wallet") f.Actor.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, 7);
        if (reason == "missing-item") f.Inner.Source.ManagerProxy.SetMethodHandler("GetItemInfo", arguments => null);
        if (reason == "ineligible") f.Eligibility = _ => false;
        if (reason == "cap") f.Tables.CurrencyType.GetEntry(1).CapAmount = 1300;
        if (reason == "unselected") reward = new ContractRewardCatalog(f.Tables).GetTrack(5).Slots[0].Candidates.First(row => row.Id != reward).Id;
        Assert.False(f.Actor.Quests.QueueContractRewardClaim(reason == "unknown" ? uint.MaxValue : reward, reason == "choice" ? uint.MaxValue : 0));
        held?.Dispose(); await f.Actor.Drain();
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractRewardClaim); Assert.Empty(read.CharacterContractAccountOutbox);
        Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(3ul, read.CharacterContractRewardTrack.Single().Revision);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task QueuedContractCommitFailuresAndLostAcknowledgementsCannotPublishOrReplayAnUnprovedCredit(bool after)
    {
        var fault = new QueueClaimCommit(after);
        using var f = new Fixture(options, rewardContexts: inner => inner.Context(fault), claimQueue: true); uint reward = await SeedQueuedClaim(f);
        int reloads = Reloads(f).Length; Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 0)); await f.Actor.Drain(allowErrors: !after);
        using var read = f.Inner.Context(); Assert.Equal(after ? 1 : 0, read.CharacterContractRewardClaim.Count());
        Assert.Equal(after ? 2500100ul : 100ul, read.CharacterCurrency.Single().Amount); Assert.Equal(after ? 2500100ul : 100ul, f.Actor.Player.CurrencyManager.Single().Amount);
        Assert.Equal(!after, f.Actor.Quests.RequiresAuthoritativeReload);
        if (after) Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, 0));
        else Assert.Equal(reloads, Reloads(f).Length);
    }

    private sealed class QueueClaimCommit(bool after) : DbTransactionInterceptor
    {
        private int fired;
        private bool Target(DbContext context) => context is CharacterContext character && character.CharacterContractRewardClaim.Local.Any();
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Target(data.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0) throw new IOException("Owned queued claim before commit.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Target(data.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0 ? Task.FromException(new IOException("Owned queued claim acknowledgement loss.")) : Task.CompletedTask;
    }
}
