using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Reward;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractLiveCompletionTests
{
    private static void SeedChoice(Fixture f, params uint[] tracks)
    {
        using var seed = f.Inner.Context();
        foreach (uint track in tracks) seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = track, Points = 350, CycleId = 7, Revision = 3 });
        seed.SaveChanges();
    }

    [Fact]
    public async Task IndependentChoiceRecoveryKeepsTheConfiguredProviderStrategyAroundItsReadOnlyTransaction()
    {
        using var f = new Fixture(options); SeedChoice(f, 5); await f.Actor.Load();
        var catalog = new ContractRewardCatalog(f.Tables); var definition = catalog.GetTrack(5); string operation = Guid.NewGuid().ToString("N");
        var satisfied = definition.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Append(definition.PrerequisiteId).Distinct().ToArray();
        var selection = ContractRewardSelection.CaptureForChoose(catalog, 5, 7, operation, satisfied, _ => 0);
        var command = CharacterChallengeRewardCommand.ChooseContract(42, 5, operation, catalog, f.Actor.Quests.CommittedContractTracks, selection, satisfied);
        var probe = new ChoiceProbe("lost-ack") { Enabled = true };
        var executor = new CharacterChallengeRewardExecutor(() => f.Inner.Context(new ChoiceFlush(probe), new ChoiceCommit(probe)), f.Inner.Source.Catalog, f.Inner.Source.Tables);
        var result = await executor.ExecuteAsync(command);
        Assert.True(result.Outcome == CharacterChallengeRewardExecutionOutcome.Committed, result.Error?.GetType().FullName + "\n" + result.Error?.StackTrace);
        Assert.True(result.Recovered); Assert.True(probe.Fired); Assert.True(result.State.ContractTracks.Single().Active);
        using var read = f.Inner.Context(); Assert.Equal(4ul, read.CharacterContractRewardTrack.Single().Revision); Assert.Equal(5, read.CharacterContractRewardSlot.Count());
    }

    [Theory]
    [InlineData(4u)] [InlineData(5u)]
    public async Task ChoosingAnAdvertisedLegacyCyclePersistsEligibleRowsWithoutInventingAcceptanceHistory(uint track)
    {
        using var f = new Fixture(options); SeedChoice(f, track); await f.Actor.Load();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        Assert.True(f.Actor.Quests.ChooseContractRewardTrack(track)); Assert.False(f.Actor.Quests.ChooseContractRewardTrack(track));
        await f.Actor.Drain(); var after = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        Assert.True(after.Active); Assert.True(after.SelectionInitialized); Assert.Equal(350u, after.Points); Assert.Equal(7ul, after.CycleId); Assert.Equal(4ul, after.Revision);
        var catalog = new ContractRewardCatalog(f.Tables); Assert.Equal(catalog.GetTrack(track).Slots.Count, after.Slots.Count);
        Assert.All(after.Slots, slot => { var selection = ContractRewardSelection.Restore(catalog, slot.SelectionJson); Assert.Null(selection.AcceptanceId); Assert.NotNull(selection.OperationId); });
        Assert.False(f.Actor.Quests.ChooseContractRewardTrack(track)); Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(0, f.AuthCalls); Assert.Equal(0, f.XpCalls);
    }

    [Fact]
    public async Task ChoosingAnInitializedCyclePreservesItsCompletionSelectionAndOrdinaryRewards()
    {
        using var f = new Fixture(options); await f.Actor.Load(); f.Complete(); await f.Actor.Drain();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([])); var before = Assert.Single(f.Actor.Quests.CommittedContractTracks);
        f.Eligibility = _ => false; Assert.True(f.Actor.Quests.ChooseContractRewardTrack(f.Source.TrackId)); await f.Actor.Drain();
        var after = Assert.Single(f.Actor.Quests.CommittedContractTracks); Assert.True(after.Active); Assert.Equal(before.Slots, after.Slots);
        Assert.Equal(before.Points, after.Points); Assert.Equal(before.CycleId, after.CycleId); Assert.Equal(before.Revision + 1, after.Revision); f.AssertCommitted();
    }

    [Fact]
    public async Task IndependentContractTypesCoexistWithChallengeAndLoyaltyAcrossLaterCommands()
    {
        using var f = new Fixture(options); SeedChoice(f, 4, 5); await f.Actor.Load();
        Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([new() { RewardTrackId = 9, RewardPointsEarned = 800, IsActive = true }]));
        Assert.True(f.Actor.Quests.ChooseContractRewardTrack(4)); await f.Actor.Drain();
        f.Actor.Quest.Timer = 901;
        f.Actor.Challenges.HandleChoice(f.Inner.Source.Challenge(16), ChallengeChoice.Activate); await f.Actor.Drain();
        Assert.True(f.Actor.Challenges.TryAdvanceProgress(f.Inner.Source.Challenge(16), 5));
        Assert.True(f.Actor.Quests.ChooseContractRewardTrack(5)); await f.Actor.Drain();
        Assert.All(f.Actor.Quests.CommittedContractTracks, state => Assert.True(state.Active));
        using (var beforeSave = f.Inner.Context()) { Assert.Null(beforeSave.CharacterQuest.Single().Timer); Assert.Equal(0u, beforeSave.CharacterChallenge.Single().CurrentCount); }
        await f.Actor.Player.SaveDirect();
        using (var afterSave = f.Inner.Context()) { Assert.Equal(901u, afterSave.CharacterQuest.Single().Timer); Assert.Equal(5u, afterSave.CharacterChallenge.Single().CurrentCount); }
        Assert.True(f.Actor.Challenges.TryAdvanceProgress(f.Inner.Source.Challenge(16), 10)); await f.Actor.Drain();
        var rows = Reloads(f).Last().RewardsTracks; Assert.Equal(new uint[] { 4, 5, 9, 16 }, rows.Select(row => row.RewardTrackId).Order());
        Assert.All(rows, row => Assert.True(row.IsActive)); Assert.Equal(800u, rows.Single(row => row.RewardTrackId == 9).RewardPointsEarned);
        Assert.Equal(100u, rows.Single(row => row.RewardTrackId == 16).RewardPointsEarned);
        using var read = f.Inner.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item);
    }

    [Theory]
    [InlineData("unpublished")] [InlineData("cache-only")] [InlineData("busy")] [InlineData("inventory")]
    [InlineData("unknown")] [InlineData("not-advertised")] [InlineData("invalidated")] [InlineData("foreign-session")] [InlineData("ineligible")]
    public async Task RejectedContractChoicesCannotAllocateOrMutateAnUnownedCycle(string reason)
    {
        using var f = new Fixture(options); SeedChoice(f, 5); await f.Actor.Load(); IDisposable held = null;
        if (reason != "unpublished") Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        if (reason == "cache-only") f.Actor.Quests.AdoptContractRewards([f.Actor.Quests.CommittedContractTracks.Single() with { Points = 351, Revision = 4 }]);
        if (reason == "busy") f.Actor.StartSave();
        if (reason == "inventory") Assert.True(f.Inner.Source.Inventory.TryAcquireSaveLease(out held));
        if (reason == "invalidated") f.Actor.Quests.InvalidateRewardOperations();
        if (reason == "foreign-session") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (reason == "ineligible") f.Eligibility = _ => false;
        else f.Eligibility = _ => throw new InvalidOperationException("Rejected request evaluated eligibility.");
        Assert.False(f.Actor.Quests.ChooseContractRewardTrack(reason == "unknown" ? 35u : reason == "not-advertised" ? 4u : 5u));
        held?.Dispose(); await f.Actor.Drain();
        using var read = f.Inner.Context(); var state = read.CharacterContractRewardTrack.Single();
        Assert.False(state.Active); Assert.False(state.SelectionInitialized); Assert.Equal(350u, state.Points); Assert.Equal(3ul, state.Revision);
        Assert.Empty(read.CharacterContractRewardSlot); Assert.Empty(read.Item); Assert.Empty(read.CharacterContractCompletion);
    }

    [Fact]
    public async Task ReentrantEligibilityCannotQueueAChoiceAheadOfANewNormalSave()
    {
        using var f = new Fixture(options); SeedChoice(f, 5); await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([])); bool entered = false;
        f.Eligibility = _ => { if (!entered) { entered = true; f.Actor.StartSave(); } return true; };
        Assert.False(f.Actor.Quests.ChooseContractRewardTrack(5)); await f.Actor.Drain(); Assert.True(entered);
        using var read = f.Inner.Context(); Assert.False(read.CharacterContractRewardTrack.Single().Active); Assert.Empty(read.CharacterContractRewardSlot);
    }

    [Fact]
    public async Task AStalePublishedChoiceCannotRebaseToChangedSqlProgression()
    {
        using var f = new Fixture(options); SeedChoice(f, 5); await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        using (var edit = f.Inner.Context()) { var row = edit.CharacterContractRewardTrack.Single(); row.Points++; row.Revision++; edit.SaveChanges(); }
        Assert.True(f.Actor.Quests.ChooseContractRewardTrack(5)); await f.Actor.Drain(allowErrors: true); Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        using var read = f.Inner.Context(); Assert.Equal(351u, read.CharacterContractRewardTrack.Single().Points); Assert.Equal(4ul, read.CharacterContractRewardTrack.Single().Revision);
        Assert.False(read.CharacterContractRewardTrack.Single().Active); Assert.Empty(read.CharacterContractRewardSlot);
    }

    [Theory]
    [InlineData("flush")] [InlineData("before-commit")] [InlineData("lost-ack")]
    [InlineData("tamper-slot")] [InlineData("tamper-marker")] [InlineData("suppress-active")]
    public async Task ContractChoiceFaultsCannotSplitActiveStateFromItsSelectedRows(string mode)
    {
        var probe = new ChoiceProbe(mode);
        using var f = new Fixture(options, rewardContexts: inner => inner.Context(new ChoiceFlush(probe), new ChoiceCommit(probe)));
        SeedChoice(f, 5); await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([])); probe.Enabled = true;
        Assert.True(f.Actor.Quests.ChooseContractRewardTrack(5)); await f.Actor.Drain(allowErrors: true); Assert.True(probe.Fired);
        using var read = f.Inner.Context(); var row = read.CharacterContractRewardTrack.Single();
        Assert.Equal(mode == "lost-ack", row.Active); Assert.Equal(mode == "lost-ack", row.SelectionInitialized); Assert.Equal(350u, row.Points);
        Assert.Equal(mode == "lost-ack" ? 4ul : 3ul, row.Revision);
        Assert.Equal(mode == "lost-ack" ? 5 : 0, read.CharacterContractRewardSlot.Count());
        Assert.Equal(mode != "lost-ack", f.Actor.Challenges.RequiresAuthoritativeReload); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item);
        if (mode == "lost-ack") Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
    }

    [Theory]
    [InlineData(4u)] [InlineData(5u)]
    public async Task TheRegisteredChoosePacketReachesTheRealQuestOwnerAndSurvivesRelog(uint track)
    {
        using var f = new Fixture(options); SeedChoice(f, track);
        var originalSession = f.Actor.Player.Session;
        var world = RecordingDispatchProxy<IWorldSession>.Create(out var proxy); proxy.SetProperty(nameof(IWorldSession.Player), f.Actor.Player);
        proxy.SetProperty(nameof(IWorldSession.Account), f.Actor.Player.Account); proxy.SetProperty(nameof(IGameSession.Events), f.Actor.Events);
        f.Actor.Set(nameof(IPlayer.Session), world); f.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.Session), world);
        ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), world);
        await f.Actor.Load(); Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks([]));
        var registry = new MessageManager(NullLogger<MessageManager>.Instance); registry.RegisterMessage(typeof(ClientRewardTrackChooseTrack)); registry.RegisterMessageHandler(typeof(ClientRewardTrackChooseTrackHandler));
        using var bytes = new MemoryStream(); using (var writer = new GamePacketWriter(bytes)) { writer.Write(track, 14); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
        var message = (IReadable)Activator.CreateInstance(registry.GetMessageType((GameMessageOpcode)0x07CF))!; message.Read(reader);
        registry.GetMessageHandlerDelegate((GameMessageOpcode)0x07CF)(new ClientRewardTrackChooseTrackHandler(f.Tables), world, message);
        await f.Actor.Drain(); var before = f.Actor.Quests.CommittedContractTracks.Single(); Assert.True(before.Active); f.Actor.Dispose();
        f.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.Session), originalSession);
        ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), originalSession);
        using var relog = new Challenges.RewardNormalSaveSnapshotTests.Actor(f.Inner, questTables: f.Tables, contracts: new ContractManager(f.Tables));
        await relog.Load(); Assert.True(relog.Quests.CommittedContractTracks.Single().Active); Assert.True(ContractRewardSnapshot.Same(before, relog.Quests.CommittedContractTracks.Single()));
    }

    private sealed class ChoiceProbe(string mode)
    {
        internal bool Enabled, Fired;
        internal bool Fire(string candidate) { if (!Enabled || Fired || candidate != mode) return false; Fired = true; return true; }
    }
    private sealed class ChoiceFlush(ChoiceProbe probe) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (probe.Fire("flush")) throw new IOException("Contract choose flush fault.");
            var context = (CharacterContext)data.Context;
            if (probe.Fire("tamper-slot")) context.CharacterContractRewardSlot.Local.First().RewardRowId = 69;
            if (probe.Fire("tamper-marker")) context.CharacterContractRewardTrack.Local.Single().SelectionInitialized = false;
            if (probe.Fire("suppress-active")) context.Entry(context.CharacterContractRewardTrack.Local.Single()).Property(row => row.Active).IsModified = false;
            return new(result);
        }
    }
    private sealed class ChoiceCommit(ChoiceProbe probe) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
            => probe.Fire("before-commit") ? throw new IOException("Contract choose precommit fault.") : new(result);
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => probe.Fire("lost-ack") ? Task.FromException(new IOException("Contract choose acknowledgement lost.")) : Task.CompletedTask;
    }
}
