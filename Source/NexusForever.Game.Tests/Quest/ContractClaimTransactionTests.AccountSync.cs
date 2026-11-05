using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractClaimTransactionTests
{
    [Theory]
    [InlineData(4u)] [InlineData(5u)]
    public async Task ActorClaimsDeliverAccountCreditsRefreshTheWalletAndAdoptTheSourceAcknowledgementBeforeAnotherClaim(uint trackId)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, trackId, final: false);
        Assert.True(f.Live.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.Live.Actor.Drain();
        using var source = f.Live.Inner.Context(); var outbox = Assert.Single(source.CharacterContractAccountOutbox); Assert.True(outbox.Delivered);
        var track = Assert.Single(f.Live.Actor.Quests.CommittedContractTracks); Assert.Equal(5ul, track.Revision); Assert.True(track.Slots[0].Claim.AccountDelivered);
        using var auth = f.Auth(); var wallet = Assert.Single(auth.AccountCurrency); Assert.Equal(105ul, wallet.Amount);
        Assert.Single(auth.AccountCurrencyOperation); Assert.Equal(wallet.Amount, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        Assert.Equal(100ul, f.Live.Actor.Player.CurrencyManager.Single().Amount);
        Assert.False(f.Live.Actor.Challenges.HasPendingRewardOperation); Assert.False(f.Live.Actor.Quests.QueueContractRewardClaim(reward, 2));
        Assert.Equal(wallet.Amount, f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Select(call => call.Arguments[0]).OfType<ServerAccountCurrencyGrant>().Last().AccountCurrency.Amount);
    }

    [Theory]
    [InlineData(4u)] [InlineData(5u)]
    public async Task AccountClaimBitWaitsForWalletCreditAndSourceAcknowledgement(uint trackId)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions);
        uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, trackId, final: false);
        var authGate = new SyncPause();
        var sourceGate = new SyncPause(source: true);
        f.AuthFault = authGate;
        f.CharacterFault = sourceGate;
        var session = f.Live.Inner.Source.SessionProxy;
        object[] Packets() => session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).ToArray();
        bool ShowsClaim(object packet) => packet is ServerRewardTracksReload reload
            && reload.RewardsTracks.Any(row => row.RewardTrackId == trackId && (row.ClaimedRewards.GetBuffer()[0] & 1) != 0);

        void AssertPending(uint accountProgress)
        {
            using var pending = f.Live.Inner.Context();
            Assert.False(Assert.Single(pending.CharacterContractAccountOutbox).Delivered);
            Assert.True(f.Actor.Quests.CommittedContractTracks.Single().Slots[0].Claimed);
            Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, 2));

            // Another owner can trigger a composed reload while the wallet transaction is paused.
            Assert.True(f.Actor.Player.RewardTrackPresentation.PublishAccountTracks(
                [new() { RewardTrackId = 9, RewardPointsEarned = accountProgress, IsActive = true }]));
            Assert.Contains(Packets().OfType<ServerRewardTracksReload>(),
                packet => packet.RewardsTracks.Any(row => row.RewardTrackId == 9 && row.RewardPointsEarned == accountProgress));
            Assert.DoesNotContain(Packets(), ShowsClaim);
        }

        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2));
        try
        {
            await f.AwaitGate(authGate);
            AssertPending(801);
            authGate.Release.TrySetResult();
            await f.AwaitGate(sourceGate);
            AssertPending(802);
        }
        finally
        {
            authGate.Release.TrySetResult();
            sourceGate.Release.TrySetResult();
            await f.Actor.Drain();
        }

        object[] settled = Packets();
        int wallet = Array.FindIndex(settled, packet =>
            packet is ServerAccountCurrencyGrant grant && grant.AccountCurrency.Amount == 105);
        int claimed = Array.FindIndex(settled, ShowsClaim);
        Assert.True(wallet >= 0 && claimed > wallet);
        using var read = f.Live.Inner.Context();
        Assert.True(Assert.Single(read.CharacterContractAccountOutbox).Delivered);
        Assert.True(f.Actor.Player.RewardTrackPresentation.IsPublishedContractSnapshot(f.Actor.Quests.CommittedContractTracks));
    }

    [Fact]
    public async Task AccountDeliveryKeepsPendingAndLaterWalletIntentAndOrdinaryQuestSavesBehindItsQueueBarrier()
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var gate = new SyncPause(); f.AuthFault = gate;
        f.Wallets.CurrencyAddAmount(AccountCurrencyType.ProtostarPromissoryNote, 10);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.AwaitGate(gate);
        f.Wallets.CurrencySubtractAmount(AccountCurrencyType.ProtostarPromissoryNote, 4); f.Actor.Quest.Timer = 901;
        Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Actor.Player.SaveDirect());
        gate.Release.TrySetResult(); await f.Actor.Drain();
        Assert.Equal(111ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        using (var before = f.Auth()) { Assert.Equal(105ul, before.AccountCurrency.Single().Amount); Assert.Single(before.AccountCurrencyOperation); }
        await f.Actor.Player.SaveDirect();
        using var auth = f.Auth(); Assert.Equal(111ul, auth.AccountCurrency.Single().Amount); Assert.Equal(3, auth.AccountCurrencyOperation.Count());
        using var source = f.Live.Inner.Context(); Assert.Equal(901u, source.CharacterQuest.Single().Timer); Assert.Equal(5ul, source.CharacterContractRewardTrack.Single().Revision);
    }

    [Theory]
    [InlineData("grant", "retry")]
    [InlineData("wallet", "retry")]
    [InlineData("wallet", "persistent")]
    [InlineData("wallet", "replacement")]
    public async Task CommittedContractWalletResumesAtTheFailedPacket(string failedPacket, string outcome)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions);
        uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var session = f.Live.Inner.Source.SessionProxy;
        List<string> attempted = [];
        List<string> sent = [];
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            string label = args[0] switch
            {
                ServerAccountCurrencyGrant => "grant",
                ServerWalletUpdate => "wallet",
                ServerRewardTracksReload reload when reload.RewardsTracks.Any(track =>
                    (track.RewardTrackId is 4 or 5) && (track.ClaimedRewards.GetBuffer()[0] & 1) != 0) => "claim",
                _ => null
            };
            if (label == null)
                return null;

            bool firstAttempt = !attempted.Contains(label);
            attempted.Add(label);
            if (label == failedPacket && (firstAttempt || outcome == "persistent"))
            {
                if (outcome == "replacement")
                    f.Actor.Events.EnqueueEvent(new PredicateEvent(() => true,
                        () => session.SetProperty(nameof(IGameSession.Events), new EventQueue())));
                throw new IOException("Owned wallet packet failed before enqueue.");
            }
            sent.Add(label);
            return null;
        });

        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2));
        await f.Actor.Drain();

        if (outcome != "retry")
        {
            string[] expected = outcome == "persistent" ? ["grant", "wallet", "wallet", "wallet"] : ["grant", "wallet"];
            Assert.Equal(expected, attempted);
            Assert.Equal(new[] { "grant" }, sent);
            Assert.Equal(outcome == "persistent" ? 1 : 0, session.GetInvocations(nameof(IGameSession.ForceDisconnect)).Count);
            Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        }
        else
        {
            string[] expected = failedPacket == "grant"
                ? ["grant", "grant", "wallet", "claim"]
                : ["grant", "wallet", "wallet", "claim"];
            Assert.Equal(expected, attempted);
            Assert.Equal(new[] { "grant", "wallet", "claim" }, sent);
            Assert.Empty(session.GetInvocations(nameof(IGameSession.ForceDisconnect)));
            Assert.True(f.Actor.Quests.CommittedContractTracks.Single().Slots[0].Claim.AccountDelivered);
        }

        using var auth = f.Auth();
        Assert.Equal(105ul, auth.AccountCurrency.Single().Amount);
        Assert.Single(auth.AccountCurrencyOperation);
        using var read = f.Live.Inner.Context();
        Assert.True(Assert.Single(read.CharacterContractAccountOutbox).Delivered);
        Assert.Equal(5ul, read.CharacterContractRewardTrack.Single().Revision);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task AccountSynchronizationRecoversEitherDatabaseCommitFaultWithoutRegrantingOrAdvancingTheTrackTwice(bool source, bool after)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var fault = new SyncCommitFault(source, after); if (source) f.CharacterFault = fault; else f.AuthFault = fault;
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.Actor.Drain(); Assert.True(fault.Fired);
        using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
        using var read = f.Live.Inner.Context(); Assert.True(read.CharacterContractAccountOutbox.Single().Delivered); Assert.Equal(5ul, read.CharacterContractRewardTrack.Single().Revision);
        Assert.Equal(5ul, f.Actor.Quests.CommittedContractTracks.Single().Revision); Assert.Equal(105ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
    }

    [Theory]
    [InlineData("logout")] [InlineData("finished")] [InlineData("session")] [InlineData("queue")] [InlineData("final")]
    public async Task ClosingOrReplacedActorsDoNotPublishLateAccountCreditsAndFinalDisposalNeedsNoSessionPolling(string state)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var gate = new SyncPause(); f.AuthFault = gate; Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.AwaitGate(gate);
        Task observation = f.Observation(); int packets = f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count;
        RecordingDispatchProxy<IGameSession> replacement = null;
        if (state == "session")
        {
            var session = RecordingDispatchProxy<IGameSession>.Create(out replacement); f.Actor.Set(nameof(Player.Session), session);
            ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), session);
        }
        else if (state == "queue")
            f.Live.Inner.Source.SessionProxy.SetProperty(nameof(IGameSession.Events), new EventQueue());
        else if (state == "final") { f.Actor.Quests.InvalidateRewardOperations(true); f.Actor.Challenges.InvalidateRewardOperations(true); }
        else f.Actor.Player.LogoutManager.State = state == "logout" ? LogoutState.Logout : LogoutState.Finished;
        gate.Release.TrySetResult(); await observation.WaitAsync(TimeSpan.FromSeconds(20));
        if (state != "final") await f.Actor.Drain(allowErrors: true);
        Assert.False(f.Actor.Challenges.HasPendingRewardOperation); Assert.Equal(100ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        Assert.Equal(packets, f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count);
        if (replacement != null) Assert.Empty(replacement.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        if (state == "queue") Assert.Empty(f.Live.Inner.Source.SessionProxy.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        using var read = f.Live.Inner.Context(); Assert.True(read.CharacterContractAccountOutbox.Single().Delivered); Assert.Equal(5ul, read.CharacterContractRewardTrack.Single().Revision);
        using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RebindingDuringWalletGrantSuppressesTheRemainingContractNotices(bool replacePlayer)
    {
        var world = RecordingDispatchProxy<IWorldSession>.Create(out var session);
        session.SetProperty(nameof(IGameSession.Events), new EventQueue());
        using var f = new AccountSyncFixture(characterOptions, authOptions, worldSession: world);
        uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        int firstGrants = 0, trailingNotices = 0;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAccountCurrencyGrant)
            {
                firstGrants++;
                if (replacePlayer)
                    world.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
                else
                    session.SetProperty(nameof(IGameSession.Events), new EventQueue());
            }
            else if (firstGrants != 0 && args[0] is ServerWalletUpdate or ServerRewardTracksReload)
                trailingNotices++;
            return null;
        });

        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2));
        await f.Actor.Drain();

        Assert.Equal(1, firstGrants);
        Assert.Equal(0, trailingNotices);
        Assert.Empty(session.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        using var read = f.Live.Inner.Context();
        Assert.True(Assert.Single(read.CharacterContractAccountOutbox).Delivered);
        using var auth = f.Auth();
        Assert.Equal(105ul, auth.AccountCurrency.Single().Amount);
        Assert.Single(auth.AccountCurrencyOperation);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RelogDrainsPendingOrAlreadyAcknowledgedCreditsBeforeWorldReadinessEvenWhenAdmissionCachedTheOlderWallet(bool acknowledged)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions, enabled: false); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.Actor.Drain();
        if (acknowledged)
        {
            using var source = f.Live.Inner.Context(); var claim = source.CharacterContractAccountOutbox.Single().ClaimId;
            Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await new ContractAccountDelivery(() => f.Live.Inner.Context(), f.Auth, new(f.Live.Tables), 1).DeliverAsync(claim)).Outcome);
        }
        f.Reopen(); int ready = 0; Assert.Equal(100ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        f.Actor.Challenges.PrepareForWorld(() => ++ready); Assert.Equal(0, ready); await f.Actor.Drain();
        Assert.Equal(1, ready); Assert.Equal(105ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        Assert.Equal(5ul, f.Actor.Quests.CommittedContractTracks.Single().Revision);
        using var read = f.Live.Inner.Context(); Assert.True(read.CharacterContractAccountOutbox.Single().Delivered);
        using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
    }

    [Theory]
    [InlineData("deleted")] [InlineData("other-account")] [InlineData("other-realm")]
    public async Task AccountWideDrainingPaysAnIndependentDeletedSourceButNeverAnotherAccountOrRealm(string scope)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var catalog = new ContractRewardAwardCatalog(f.Live.Tables); var original = f.Actor.Quests.CommittedContractTracks.Single();
        var quote = ContractRewardClaimQuote.Capture(catalog, 84, [original with { CharacterId = 84 }], original.TrackId, original.CycleId, original.Revision, reward, 2);
        var intent = ContractAccountIntent.Capture(catalog, scope == "other-realm" ? (ushort)2 : (ushort)1, scope == "other-account" ? 28u : 27u, Guid.NewGuid().ToString("N"), quote);
        using (var seed = f.Live.Inner.Context())
        {
            seed.CharacterContractAccountOutbox.Add(new() { ClaimId = intent.ClaimId, CharacterId = 84, AccountId = intent.AccountId, RealmId = intent.RealmId,
                TrackId = quote.Award.TrackId, CycleId = quote.CycleId, Ordinal = quote.Award.Ordinal, PayloadJson = intent.Json }); seed.SaveChanges();
        }
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.Actor.Drain();
        using var auth = f.Auth(); Assert.Equal(scope == "deleted" ? 110ul : 105ul, auth.AccountCurrency.Single().Amount);
        Assert.Equal(scope == "deleted" ? 2 : 1, auth.AccountCurrencyOperation.Count());
        using var read = f.Live.Inner.Context(); Assert.Equal(scope == "deleted", read.CharacterContractAccountOutbox.Single(row => row.ClaimId == intent.ClaimId).Delivered);
        Assert.Equal(5ul, f.Actor.Quests.CommittedContractTracks.Single().Revision);
    }

    [Theory]
    [InlineData("json")] [InlineData("account")] [InlineData("realm")] [InlineData("lost-receipt")]
    public async Task CorruptRelogIntentsCannotBypassProofOrInventAnAuthCredit(string fault)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions, enabled: false); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.Actor.Drain();
        using (var change = f.Live.Inner.Context())
        {
            var row = change.CharacterContractAccountOutbox.Single();
            if (fault == "json") row.PayloadJson = "{}"; if (fault == "account") row.AccountId = 28;
            if (fault == "realm") row.RealmId = 2; if (fault == "lost-receipt") row.Delivered = true; change.SaveChanges();
        }
        f.Reopen(); int ready = 0; f.Actor.Challenges.PrepareForWorld(() => ++ready); await f.Actor.Drain(allowErrors: true);
        Assert.Equal(0, ready); Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        Assert.Equal(100ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        using var auth = f.Auth(); Assert.Equal(100ul, auth.AccountCurrency.Single().Amount); Assert.Empty(auth.AccountCurrencyOperation);
    }

    [Fact]
    public async Task SubsequentMilestonesUseTheAcknowledgedRevisionAndFinalItemClaimsKeepTheirExistingPayouts()
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint first = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(first, 2)); await f.Actor.Drain();
        var track = f.Actor.Quests.CommittedContractTracks.Single(); Assert.True(f.Actor.Quests.QueueContractRewardClaim(track.Slots[1].RewardRowId, 2)); await f.Actor.Drain();
        track = f.Actor.Quests.CommittedContractTracks.Single(); Assert.Equal(7ul, track.Revision);
        Assert.True(f.Actor.Quests.QueueContractRewardClaim(track.Slots[^1].RewardRowId, 2)); await f.Actor.Drain();
        track = f.Actor.Quests.CommittedContractTracks.Single(); Assert.Equal(8ul, track.Revision); Assert.Equal(7ul, track.CycleId);
        using var source = f.Live.Inner.Context(); Assert.Equal(2, source.CharacterContractAccountOutbox.Count()); Assert.Equal(3, source.CharacterContractRewardClaim.Count());
        Assert.NotEmpty(source.Item); Assert.Equal(2500100ul, source.CharacterCurrency.Single().Amount);
        using var auth = f.Auth(); Assert.Equal(110ul, auth.AccountCurrency.Single().Amount); Assert.Equal(2, auth.AccountCurrencyOperation.Count());
        Assert.Empty(auth.AccountEntitlement); Assert.Empty(auth.AccountInventory);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TransferredOrDeletedCharactersCannotPublishAFreshSnapshotToThePreviousAccount(bool delete)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var gate = new SyncPause(); f.AuthFault = gate; Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 2)); await f.AwaitGate(gate);
        int tracks = f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count(call => call.Arguments[0] is ServerRewardTracksReload);
        using (var change = f.Live.Inner.Context())
        { if (delete) await change.Character.ExecuteDeleteAsync(); else await change.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 28u)); }
        gate.Release.TrySetResult(); await f.Actor.Drain(allowErrors: true);
        Assert.True(f.Actor.Challenges.RequiresAuthoritativeReload);
        Assert.Equal(tracks, f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count(call => call.Arguments[0] is ServerRewardTracksReload));
        using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
        using var source = f.Live.Inner.Context(); Assert.True(source.CharacterContractAccountOutbox.Single().Delivered);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OnlinePollingFindsLatePendingAndAcknowledgedIntentsWithoutRepeatingWalletOrTrackPackets(bool acknowledged)
    {
        using var f = new AccountSyncFixture(characterOptions, authOptions); uint reward = await ContractLiveCompletionTests.SeedQueuedClaim(f.Live, final: false);
        var catalog = new ContractRewardAwardCatalog(f.Live.Tables); var track = f.Actor.Quests.CommittedContractTracks.Single();
        var quote = ContractRewardClaimQuote.Capture(catalog, 84, [track with { CharacterId = 84 }], track.TrackId, track.CycleId, track.Revision, reward, 2);
        var intent = ContractAccountIntent.Capture(catalog, 1, 27, Guid.NewGuid().ToString("N"), quote);
        using (var seed = f.Live.Inner.Context())
        {
            seed.CharacterContractAccountOutbox.Add(new() { ClaimId = intent.ClaimId, CharacterId = 84, AccountId = 27, RealmId = 1,
                TrackId = track.TrackId, CycleId = track.CycleId, Ordinal = quote.Award.Ordinal, PayloadJson = intent.Json }); seed.SaveChanges();
        }
        if (acknowledged) Assert.Equal(ContractAccountDeliveryOutcome.Delivered,
            (await new ContractAccountDelivery(() => f.Live.Inner.Context(), f.Auth, catalog, 1).DeliverAsync(intent.ClaimId)).Outcome);
        Assert.Equal(100ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        f.Actor.Challenges.Update(31); await f.Actor.Drain(); Assert.Equal(105ul, f.Wallets.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        int messages = f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count;
        f.Actor.Challenges.Update(31); await f.Actor.Drain();
        Assert.Equal(messages, f.Live.Inner.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count);
        using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
        using var source = f.Live.Inner.Context(); Assert.True(source.CharacterContractAccountOutbox.Single().Delivered); Assert.Equal(3ul, source.CharacterContractRewardTrack.Single().Revision);
    }

    private sealed class SyncPause(bool source = false) : DbTransactionInterceptor
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int fired;
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            bool isTarget = source
                ? data.Context is CharacterContext character && character.CharacterContractAccountOutbox.Local.Any(row => row.Delivered)
                : data.Context is AuthContext auth && auth.AccountCurrencyOperation.Local.Any();
            if (isTarget && Interlocked.CompareExchange(ref fired, 1, 0) == 0)
            { Entered.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken); }
            return result;
        }
    }
    private sealed class SyncCommitFault(bool source, bool after) : DbTransactionInterceptor
    {
        private int fired;
        internal bool Fired => fired != 0;
        private bool Target(DbContext context) => source ? context is CharacterContext character && character.CharacterContractAccountOutbox.Local.Any(row => row.Delivered)
            : context is AuthContext auth && auth.AccountCurrencyOperation.Local.Any();
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Target(data.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0) throw new IOException("Owned Contract sync before commit."); return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Target(data.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0 ? Task.FromException(new IOException("Owned Contract sync lost acknowledgement.")) : Task.CompletedTask;
    }

    private sealed class AccountSyncFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-ContractAccountSync-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<AuthContext> authOptions;
        internal readonly ContractLiveCompletionTests.Fixture Live;
        internal AccountCurrencyManager Wallets { get; private set; }
        internal Actor Actor => reopened ?? Live.Actor;
        private Actor reopened;
        private readonly AuthDatabase database;
        internal IInterceptor AuthFault { get; set; }
        internal IInterceptor CharacterFault { get; set; }
        internal AccountSyncFixture(DbContextOptions<CharacterContext> characterOptions, DbContextOptions<AuthContext> authOptions, bool enabled = true,
            IWorldSession worldSession = null)
        {
            Directory.CreateDirectory(directory);
            this.authOptions = authOptions ?? new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(directory, "auth.sqlite")};Pooling=False").Options;
            using (var context = Auth())
            {
                context.Database.EnsureCreated(); context.Account.Add(new() { Id = 27, Email = "contract-sync27@example.local" });
                context.AccountCurrency.Add(new() { Id = 27, CurrencyId = 14, Amount = 100 }); context.SaveChanges();
            }
            using var identity = Auth();
            var config = RecordingDispatchProxy<IConnectionString>.Create(out var cp);
            cp.SetProperty(nameof(IConnectionString.Provider), identity.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql);
            cp.SetProperty(nameof(IConnectionString.ConnectionString), identity.Database.GetDbConnection().ConnectionString);
            database = new AuthDatabase(); database.Initialise(config);
            Live = new(characterOptions, claimQueue: true, rewardContexts: inner => inner.Context(CharacterFault),
                authContexts: enabled ? () => Auth() : null, authDatabase: database, worldSession: worldSession);
            SetWallet();
        }
        private void SetWallet()
        {
            var account = Actor.Player.Account; var proxy = (RecordingDispatchProxy<IAccount>)(object)account;
            proxy.SetProperty(nameof(IAccount.Id), 27u);
            var model = new AccountModel { Id = 27 }; model.AccountCurrency.Add(new() { Id = 27, CurrencyId = 14, Amount = 100 });
            Wallets = new(account, model, Live.Tables); proxy.SetProperty(nameof(IAccount.CurrencyManager), Wallets);
            proxy.SetMethodHandler(nameof(IAccount.Save), arguments => { Wallets.Save((AuthContext)arguments[0]); return null; });
        }
        internal void Reopen()
        {
            Actor.Dispose();
            var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var proxy); proxy.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
            reopened = new(Live.Inner, prerequisites, () => Live.Inner.Context(CharacterFault), Live.Tables, new ContractManager(Live.Tables),
                rewardTables: Live.Tables, authContexts: () => Auth(), authDatabase: database);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            using var read = Live.Inner.Context(); var model = new NexusForever.Database.Character.Model.CharacterModel { Id = 42 };
            foreach (var row in read.CharacterCurrency.AsNoTracking()) model.Currency.Add(row);
            var currencies = new CurrencyManager(Live.Tables); currencies.Initialise(Actor.Player, model); Actor.Set(nameof(Player.CurrencyManager), currencies);
            Live.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), Actor.Quests); Live.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencies);
            SetWallet();
        }
        internal async Task AwaitGate(SyncPause gate)
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!gate.Entered.Task.IsCompleted)
            { Actor.Events.Update(.01); Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(20), "Account sync did not enter its owned commit gate."); await Task.Delay(5); }
        }
        internal Task Observation()
        {
            var queue = typeof(ChallengeManager).GetField("rewardQueue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Actor.Challenges)!;
            var request = queue.GetType().GetField("current", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(queue)!;
            var operation = request.GetType().GetProperty("Accounts")!.GetValue(request)!;
            return ((AccountCurrencyManager.WalletRefresh)typeof(ContractAccountSync).GetField("refresh", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(operation)!).Completion;
        }
        internal AuthContext Auth() => AuthFault == null ? new(authOptions)
            : new(new DbContextOptionsBuilder<AuthContext>(authOptions).AddInterceptors(AuthFault).Options);
        public void Dispose()
        {
            reopened?.Dispose(); Live.Dispose();
            string path = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(path) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(path).StartsWith("NexusForever-ContractAccountSync-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe fixture cleanup path.");
            Directory.Delete(path, recursive: true);
        }
    }
}
