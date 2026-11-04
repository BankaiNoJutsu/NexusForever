using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Reward;

public sealed class RewardTrackPresentationTests
{
    [Theory]
    [InlineData("latest")]
    [InlineData("superseded")]
    [InlineData("replaced-queue")]
    [InlineData("new-owner")]
    [InlineData("reentrant-owner")]
    [InlineData("reentrant-ready-owner")]
    public async Task ReloadRecoversAfterBothImmediateAttemptsFail(string scenario)
    {
        var f = new Fixture();
        var queue = new EventQueue();
        f.SessionProxy.SetProperty(nameof(IGameSession.Events), queue);
        f.Presentation.ExpectContractTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()], reload: false));
        object first = new(), latest = new();
        object character = new();
        bool reentrantOwner = scenario is "reentrant-owner" or "reentrant-ready-owner";
        int attempts = 0, terminalFailures = 0;
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ =>
        {
            if (++attempts <= 2) throw new IOException("Reward reload failed before enqueue.");
            if (attempts == 3 && reentrantOwner)
            {
                f.Presentation.ExpectCharacterTracks();
                if (scenario == "reentrant-ready-owner")
                    Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], character, reload: false));
            }
            return null;
        });

        Assert.True(f.Presentation.PublishContractTracks([Row(5, 450, 0, 0, true)], first,
            onReloadFailure: () => terminalFailures++));
        Assert.Equal(2, attempts);
        Assert.True(queue.PendingEvents);
        Assert.False(f.Presentation.IsPublishedContractSnapshot(first));

        if (scenario == "replaced-queue")
        {
            var errors = new List<Exception>();
            var replacement = new EventQueue { UnhandledExceptionHandler = errors.Add };
            f.SessionProxy.SetProperty(nameof(IGameSession.Events), replacement);
            queue.Update(0);
            Assert.False(queue.PendingEvents);
            Assert.Equal(2, attempts);
            Assert.False(f.Presentation.IsPublishedContractSnapshot(first));
            f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ => throw new IOException("Replacement queue reload failed."));
            Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
            using var replacementTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (replacement.PendingEvents) { replacement.Update(0); await Task.Delay(10, replacementTimeout.Token); }
            Assert.IsType<IOException>(Assert.Single(errors));
            Assert.Equal(0, terminalFailures);
            return;
        }

        Assert.True(f.Presentation.PublishContractTracks([Row(5, 125, 0, 0, true)], latest, reload: scenario == "superseded"));
        if (scenario != "superseded") Assert.False(f.Presentation.IsPublishedContractSnapshot(latest));
        if (scenario == "new-owner")
        {
            f.Presentation.ExpectCharacterTracks();
            await Task.Delay(TimeSpan.FromMilliseconds(1100));
            queue.Update(0);
            Assert.Equal(2, attempts);
            Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], character, reload: false));
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        if (reentrantOwner)
        {
            while (attempts == 2) { queue.Update(0); await Task.Delay(10, timeout.Token); }
            Assert.False(f.Presentation.IsPublishedContractSnapshot(latest));
            Assert.True(queue.PendingEvents);
            if (scenario == "reentrant-owner")
                Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], character, reload: false));
        }
        while (queue.PendingEvents) { queue.Update(0); await Task.Delay(10, timeout.Token); }
        Assert.Equal(reentrantOwner ? 4 : 3, attempts);
        Assert.Equal(125u, Assert.Single(Decode(f.Reloads.Last()), row => row.Id == 5).Points);
        Assert.False(f.Presentation.IsPublishedContractSnapshot(first));
        Assert.True(f.Presentation.IsPublishedContractSnapshot(latest));
        Assert.Equal(0, terminalFailures);
        if (reentrantOwner || scenario == "new-owner") Assert.True(f.Presentation.IsPublishedCharacterSnapshot(character));
    }

    [Fact]
    public void ReloadRetryCannotAuthorizeClaimsAfterItsEnqueueRebindsThePlayer()
    {
        var f = new Fixture();
        var actor = (IPlayer)(object)f.PlayerProxy;
        var world = RecordingDispatchProxy<IWorldSession>.Create(out var session);
        session.SetProperty(nameof(IWorldSession.Player), actor);
        f.PlayerProxy.SetProperty(nameof(IPlayer.Session), world);
        f.AccountProxy.SetProperty(nameof(IAccount.Session), world);
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()], reload: false));
        object snapshot = new();
        int attempts = 0;
        var replacement = RecordingDispatchProxy<IPlayer>.Create(out _);
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ =>
        {
            if (++attempts == 1) throw new IOException("First reload failed before enqueue.");
            session.SetProperty(nameof(IWorldSession.Player), replacement);
            return null;
        });

        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], snapshot));
        Assert.Equal(2, attempts);
        Assert.False(f.Presentation.IsPublishedCharacterSnapshot(snapshot));
        session.SetProperty(nameof(IWorldSession.Player), actor);
        Assert.False(f.Presentation.IsPublishedCharacterSnapshot(snapshot));
    }

    [Theory]
    [InlineData(0, 1, 2)] [InlineData(0, 2, 1)] [InlineData(1, 0, 2)]
    [InlineData(1, 2, 0)] [InlineData(2, 0, 1)] [InlineData(2, 1, 0)]
    public void AllThreeOwnersMustBeReadyBeforeTheReplacingReloadInAnyOrder(int first, int second, int third)
    {
        var f = new Fixture(); object contract = new(), character = new();
        f.Presentation.ExpectCharacterTracks(); f.Presentation.ExpectContractTracks();
        bool Publish(int owner) => owner switch
        {
            0 => f.Presentation.PublishAccountTracks([AccountTrack()]),
            1 => f.Presentation.PublishCharacterTracks([CharacterTrack()], character),
            _ => f.Presentation.PublishContractTracks([Row(4, 150, 0, 0, true), Row(5, 450, 0, 0, true)], contract)
        };
        Assert.True(Publish(first)); Assert.True(Publish(second)); Assert.Empty(f.Reloads);
        Assert.False(f.Presentation.IsPublishedContractSnapshot(contract)); Assert.True(Publish(third));
        var rows = Decode(Assert.Single(f.Reloads)); Assert.Equal(new uint[] { 4, 5, 9, 16 }, rows.Select(row => row.Id).Order());
        Assert.Equal(450u, Assert.Single(rows, row => row.Id == 5).Points);
        Assert.True(f.Presentation.IsPublishedContractSnapshot(contract)); Assert.True(f.Presentation.IsPublishedCharacterSnapshot(character));
    }

    [Fact]
    public void LoadedEmptyContractContributionIsDistinctFromAnUnloadedOwner()
    {
        var f = new Fixture(); object empty = new(); f.Presentation.ExpectContractTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()])); Assert.Empty(f.Reloads);
        Assert.True(f.Presentation.PublishContractTracks([], empty)); Assert.Single(f.Reloads);
        Assert.True(f.Presentation.IsPublishedContractSnapshot(empty));
        f.Presentation.ExpectContractTracks(); Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        AssertUnion(f.Reloads.Last(), 800, 200);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReentrantContractPublicationBindsOnlyTheLastReloadsTwoActualSnapshots(bool nestedReload)
    {
        var f = new Fixture(); object first = new(), second = new(), character = new(); bool entered = false;
        f.Presentation.ExpectCharacterTracks(); f.Presentation.ExpectContractTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], character));
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ =>
        {
            if (!entered) { entered = true; Assert.True(f.Presentation.PublishContractTracks([Row(5, 900, 0, 0, false)], second, nestedReload)); }
            return null;
        });
        Assert.True(f.Presentation.PublishContractTracks([Row(5, 450, 0, 0, false)], first));
        Assert.True(f.Presentation.IsPublishedContractSnapshot(nestedReload ? second : first));
        Assert.False(f.Presentation.IsPublishedContractSnapshot(nestedReload ? first : second));
        Assert.True(f.Presentation.IsPublishedCharacterSnapshot(character));
        Assert.Equal(nestedReload ? 900u : 450u, Assert.Single(Decode(f.Reloads.Last()), row => row.Id == 5).Points);
    }

    [Fact]
    public void ContractCacheOnlyUpdatesAreCopiedAndCannotAuthorizeAnUnsentSnapshot()
    {
        var f = new Fixture(); object first = new(), second = new(); var row = Row(5, 450, 0, 0, false);
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.PublishContractTracks([row], first));
        Assert.True(f.Presentation.PublishContractTracks([row], second, reload: false)); row.RewardPointsEarned = 999;
        row.ClaimedRewards.GetBuffer()[0] = 255; row.RewardTrackRewardsIds.Add(270);
        f.Reloads.Last().RewardsTracks.Single(track => track.RewardTrackId == 5).RewardPointsEarned = 888;
        Assert.True(f.Presentation.IsPublishedContractSnapshot(first)); Assert.False(f.Presentation.IsPublishedContractSnapshot(second));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        var sent = Assert.Single(Decode(f.Reloads.Last()), value => value.Id == 5);
        Assert.Equal(450u, sent.Points); Assert.Equal(0u, sent.Claimed); Assert.Empty(sent.Rewards);
        Assert.True(f.Presentation.IsPublishedContractSnapshot(second)); Assert.False(f.Presentation.IsPublishedContractSnapshot(first));
        f.Presentation.Invalidate(); Assert.False(f.Presentation.IsPublishedContractSnapshot(second));
        Assert.False(f.Presentation.PublishContractTracks([], new()));
    }

    [Theory]
    [InlineData(9)] [InlineData(16)] [InlineData(0)] [InlineData(0x4000)]
    public void InvalidContractContributionsCannotReplaceTheAcceptedCache(uint id)
    {
        var f = new Fixture(); object snapshot = new(); Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        Assert.True(f.Presentation.PublishContractTracks([Row(5, 450, 0, 0, false)], snapshot));
        Assert.False(f.Presentation.PublishContractTracks([Row(id, 999, 0, 0, false)], new()));
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.Equal(450u, Assert.Single(Decode(f.Reloads.Last()), row => row.Id == 5).Points);
        Assert.True(f.Presentation.IsPublishedContractSnapshot(snapshot));
    }

    [Fact]
    public void OnlyTheSnapshotActuallyIncludedInAFullReloadCanAuthorizeAClaim()
    {
        var f = new Fixture(); object first = new(), second = new(); f.Presentation.ExpectCharacterTracks();
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], first));
        Assert.False(f.Presentation.IsPublishedCharacterSnapshot(first));
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.IsPublishedCharacterSnapshot(first));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], second, reload: false));
        Assert.True(f.Presentation.IsPublishedCharacterSnapshot(first)); Assert.False(f.Presentation.IsPublishedCharacterSnapshot(second));
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()], reload: false));
        Assert.False(f.Presentation.IsPublishedCharacterSnapshot(second));
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.IsPublishedCharacterSnapshot(second)); Assert.False(f.Presentation.IsPublishedCharacterSnapshot(first));
        f.Presentation.Invalidate(); Assert.False(f.Presentation.IsPublishedCharacterSnapshot(second));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReentrantPublicationBindsTheLastSentSnapshotRatherThanTheLastContribution(bool nestedReload)
    {
        var f = new Fixture(); object first = new(), second = new(); bool reentered = false;
        f.Presentation.ExpectCharacterTracks(); Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (!reentered) { reentered = true; Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], second, nestedReload)); }
            return null;
        });
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], first));
        Assert.True(f.Presentation.IsPublishedCharacterSnapshot(nestedReload ? second : first));
        Assert.False(f.Presentation.IsPublishedCharacterSnapshot(nestedReload ? first : second));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequiredCharacterAndAccountReadinessSuppressPartialReloadInEitherOrder(bool accountFirst)
    {
        var f = new Fixture();
        f.Presentation.ExpectCharacterTracks();
        if (accountFirst) Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        else Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        Assert.Empty(f.Reloads);
        if (accountFirst) Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        else Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        AssertUnion(Assert.Single(f.Reloads), 800, 200);
    }

    [Fact]
    public void CharacterOptionalAllowsExistingAccountOnlyConsumerThenComposesWhenPresent()
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.Equal(new uint[] { 9 }, Decode(Assert.Single(f.Reloads)).Select(row => row.Id));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        AssertUnion(f.Reloads.Last(), 800, 200);
    }

    [Fact]
    public void ExplicitEmptyCharacterSnapshotIsReadyAndDoesNotInheritAnotherCharactersRows()
    {
        var f = new Fixture();
        f.Presentation.ExpectCharacterTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.Empty(f.Reloads);
        Assert.True(f.Presentation.PublishCharacterTracks([]));
        Assert.Equal(new uint[] { 9 }, Decode(Assert.Single(f.Reloads)).Select(row => row.Id));
    }

    [Fact]
    public void RepeatedExpectDoesNotEraseAlreadyLoadedCharacterContribution()
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false));
        f.Presentation.ExpectCharacterTracks();
        f.Presentation.ExpectCharacterTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        AssertUnion(Assert.Single(f.Reloads), 800, 200);
    }

    [Fact]
    public void InputRowsListsAndClaimBuffersAreDefensivelyCopied()
    {
        var f = new Fixture();
        f.Presentation.ExpectCharacterTracks();
        RewardTrack row = AccountTrack();
        var input = new List<RewardTrack> { row };
        Assert.True(f.Presentation.PublishAccountTracks(input));
        row.RewardTrackId = 39;
        row.RewardPointsEarned = 999;
        row.RewardTrackRewardsIds[0] = 558;
        row.ClaimedRewards.GetBuffer()[0] = 0xFF;
        input.Clear();
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        DecodedRow account = Assert.Single(Decode(Assert.Single(f.Reloads)), candidate => candidate.Id == 9);
        Assert.Equal(800u, account.Points);
        Assert.Equal(1u, account.Claimed);
        Assert.Equal(new uint[] { 276, 312 }, account.Rewards);
    }

    [Fact]
    public void PreviouslyQueuedPacketMutationCannotCorruptLaterSnapshotOrAnotherPacket()
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        ServerRewardTracksReload first = Assert.Single(f.Reloads);
        first.RewardsTracks[0].RewardPointsEarned = 999;
        first.RewardsTracks[0].RewardTrackRewardsIds.Clear();
        first.RewardsTracks[0].ClaimedRewards.GetBuffer()[0] = 0xFF;
        first.RewardsTracks.Clear();
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        ServerRewardTracksReload second = f.Reloads.Last();
        AssertUnion(second, 800, 200);
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack(300)]));
        ServerRewardTracksReload third = f.Reloads.Last();
        third.RewardsTracks.Single(row => row.RewardTrackId == 9).RewardPointsEarned = 42;
        AssertUnion(second, 800, 200);
    }

    [Fact]
    public void CacheOnlyPublicationUpdatesFutureUnionWithoutEmittingAnyPacket()
    {
        var f = new Fixture();
        f.Presentation.ExpectCharacterTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()], reload: false));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false));
        Assert.Empty(f.Reloads);
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack(1600)], reload: false));
        Assert.Empty(f.Reloads);
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack(300)]));
        AssertUnion(Assert.Single(f.Reloads), 1600, 300);
    }

    [Theory]
    [InlineData("null-list")]
    [InlineData("null-row")]
    [InlineData("duplicate-track")]
    [InlineData("zero-track")]
    [InlineData("track-overflow")]
    [InlineData("short-mask")]
    [InlineData("null-mask")]
    [InlineData("null-rewards")]
    [InlineData("too-many-rewards")]
    [InlineData("zero-reward")]
    [InlineData("reward-overflow")]
    [InlineData("two-active")]
    [InlineData("cross-owner")]
    public void InvalidAccountSnapshotIsRejectedAtomicallyAndOldCacheSurvives(string invalid)
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        int packetCount = f.Reloads.Count();
        RewardTrack row = AccountTrack(999);
        IReadOnlyList<RewardTrack> input = new[] { row };
        switch (invalid)
        {
            case "null-list": input = null; break;
            case "null-row": input = new RewardTrack[] { null }; break;
            case "duplicate-track": input = new[] { row, AccountTrack() }; break;
            case "zero-track": row.RewardTrackId = 0; break;
            case "track-overflow": row.RewardTrackId = 0x4000; break;
            case "short-mask": row.ClaimedRewards = new NetworkBitArray(16, NetworkBitArray.BitOrder.LeastSignificantBit); break;
            case "null-mask": row.ClaimedRewards = null; break;
            case "null-rewards": row.RewardTrackRewardsIds = null; break;
            case "too-many-rewards": row.RewardTrackRewardsIds = Enumerable.Repeat(276u, 33).ToList(); break;
            case "zero-reward": row.RewardTrackRewardsIds[0] = 0; break;
            case "reward-overflow": row.RewardTrackRewardsIds[0] = 0x4000; break;
            case "two-active": input = new[] { row, new RewardTrack { RewardTrackId = 10, IsActive = true, RewardTrackRewardsIds = [282] } }; break;
            case "cross-owner": row.RewardTrackId = 16; break;
        }
        Assert.False(f.Presentation.PublishAccountTracks(input));
        Assert.Equal(packetCount, f.Reloads.Count());
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack(300)]));
        AssertUnion(f.Reloads.Last(), 800, 300);
    }

    [Fact]
    public void InvalidCharacterContributionCannotSetReadinessOrReplaceAccountRows()
    {
        var f = new Fixture();
        f.Presentation.ExpectCharacterTracks();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.False(f.Presentation.PublishCharacterTracks([AccountTrack()]));
        Assert.Empty(f.Reloads);
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        AssertUnion(Assert.Single(f.Reloads), 800, 200);
    }

    [Fact]
    public void StaleActorSessionCannotPublishToOldOrReplacementConnection()
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        IGameSession replacement = RecordingDispatchProxy<IGameSession>.Create(out var replacementProxy);
        f.AccountProxy.SetProperty(nameof(IAccount.Session), replacement);
        Assert.False(f.Presentation.PublishAccountTracks([AccountTrack(1600)]));
        Assert.False(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        Assert.Single(f.Reloads);
        Assert.Empty(replacementProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void NewActorCoordinatorDoesNotCarryOldCharacterSnapshotEvenOnSameAccountAndSession()
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.True(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        f.Presentation.Invalidate();
        IPlayer alternate = RecordingDispatchProxy<IPlayer>.Create(out var actorProxy);
        actorProxy.SetProperty(nameof(IPlayer.Account), f.Account);
        actorProxy.SetProperty(nameof(IPlayer.Session), f.Session);
        actorProxy.SetProperty(nameof(IPlayer.CharacterId), 2ul);
        var presentation = new RewardTrackPresentation(alternate);
        presentation.ExpectCharacterTracks();
        int count = f.Reloads.Count();
        Assert.True(presentation.PublishAccountTracks([AccountTrack(1600)]));
        Assert.Equal(count, f.Reloads.Count());
        Assert.True(presentation.PublishCharacterTracks([]));
        Assert.Equal(new uint[] { 9 }, Decode(f.Reloads.Last()).Select(row => row.Id));
        Assert.False(f.Presentation.PublishCharacterTracks([CharacterTrack(999)]));
    }

    [Fact]
    public void InvalidateIsIdempotentAndRejectsBothOwnersWithoutPackets()
    {
        var f = new Fixture();
        f.Presentation.Invalidate(); f.Presentation.Invalidate();
        f.Presentation.ExpectCharacterTracks();
        Assert.False(f.Presentation.PublishAccountTracks([AccountTrack()]));
        Assert.False(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        Assert.Empty(f.Reloads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualPlayerLifetimeInvalidationPreventsLazyGetterFromReactivating(bool createdBeforeInvalidation)
    {
        var f = new Fixture();
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(Player).GetProperty(nameof(Player.Account))!.SetValue(player, f.Account);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, f.Session);
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new NexusForever.Game.Abstract.Identity { Id = 44, RealmId = 1 });
        if (createdBeforeInvalidation)
            Assert.True(player.RewardTrackPresentation.PublishAccountTracks([AccountTrack()]));
        int count = f.Reloads.Count();
        // Exercise the actual lifetime hook used by Cleanup/Dispose, including
        // closure before this actor has ever requested its lazy presentation.
        typeof(Player).GetMethod("InvalidateRewardTrackPresentation", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(player, null);
        Assert.False(player.RewardTrackPresentation.PublishAccountTracks([AccountTrack(1600)]));
        Assert.False(player.RewardTrackPresentation.PublishCharacterTracks([CharacterTrack()]));
        Assert.Equal(count, f.Reloads.Count());
    }

    [Fact]
    public void ActualNetworkSessionForceDisconnectPendingRejectsBeforeAnyEnqueue()
    {
        var session = new ClosingGameSession();
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        IPlayer actor = RecordingDispatchProxy<IPlayer>.Create(out var actorProxy);
        actorProxy.SetProperty(nameof(IPlayer.Account), account);
        actorProxy.SetProperty(nameof(IPlayer.Session), session);
        actorProxy.SetProperty(nameof(IPlayer.CharacterId), 7ul);
        var presentation = new RewardTrackPresentation(actor);

        session.ForceDisconnect(); // Real NetworkSession enters Pending without a tick/socket.

        Assert.True(session.IsDisconnecting);
        Assert.False(presentation.IsCurrent);
        Assert.False(presentation.PublishAccountTracks([AccountTrack()]));
        Assert.False(presentation.PublishCharacterTracks([CharacterTrack()]));
        // A false guard reaching GameSession's enqueue would fail: this isolated
        // session deliberately has no message manager/encryption or live socket.
    }

    [Theory]
    [InlineData(LogoutState.Logout)]
    [InlineData(LogoutState.Finished)]
    public void ClosingLogoutStateRejectsFurtherPublicationWithoutChangingQueuedSnapshot(LogoutState state)
    {
        var f = new Fixture();
        Assert.True(f.Presentation.PublishAccountTracks([AccountTrack()]));
        ILogoutManager logout = RecordingDispatchProxy<ILogoutManager>.Create(out var logoutProxy);
        logoutProxy.SetProperty(nameof(ILogoutManager.State), state);
        f.PlayerProxy.SetProperty(nameof(IPlayer.LogoutManager), logout);
        Assert.False(f.Presentation.IsCurrent);
        Assert.False(f.Presentation.PublishAccountTracks([AccountTrack(1600)]));
        Assert.False(f.Presentation.PublishCharacterTracks([CharacterTrack()]));
        DecodedRow prior = Assert.Single(Decode(Assert.Single(f.Reloads)));
        Assert.Equal(800u, prior.Points);
    }

    private static RewardTrack AccountTrack(uint points = 800) => Row(9, points, 1, 3, true, 276, 312);
    private static RewardTrack CharacterTrack(uint points = 200) => Row(16, points, 2, 0, true, 320, 321, 324);
    private static RewardTrack Row(uint id, uint points, uint claimed, uint unlocked, bool active, params uint[] rewards)
    {
        var row = new RewardTrack { RewardTrackId = id, RewardPointsEarned = points, Unlocked = unlocked, IsActive = active,
            RewardTrackRewardsIds = rewards.ToList() };
        for (uint i = 0; i < 32; i++) if ((claimed & (1u << (int)i)) != 0) row.ClaimedRewards.SetBit(i, true);
        return row;
    }

    private static void AssertUnion(ServerRewardTracksReload packet, uint accountPoints, uint characterPoints)
    {
        DecodedRow[] rows = Decode(packet);
        Assert.Equal(2, rows.Length);
        var account = Assert.Single(rows, row => row.Id == 9);
        var character = Assert.Single(rows, row => row.Id == 16);
        Assert.Equal(accountPoints, account.Points); Assert.Equal(characterPoints, character.Points);
        Assert.Equal(1u, account.Claimed); Assert.Equal(2u, character.Claimed);
        Assert.Equal(3u, account.Unlocked); Assert.Equal(0u, character.Unlocked);
        Assert.True(account.Active); Assert.True(character.Active); // One active per owner/type, not globally one.
        Assert.Equal(new uint[] { 276, 312 }, account.Rewards);
        Assert.Equal(new uint[] { 320, 321, 324 }, character.Rewards);
    }

    private static DecodedRow[] Decode(ServerRewardTracksReload packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { packet.Write(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        uint count = reader.ReadUInt();
        var rows = new List<DecodedRow>();
        for (uint i = 0; i < count; i++)
        {
            uint id = reader.ReadUInt(14), points = reader.ReadUInt(), claimed = reader.ReadUInt();
            uint rewardCount = reader.ReadUInt(), unlocked = reader.ReadUInt();
            var rewards = new List<uint>();
            for (uint j = 0; j < rewardCount; j++) rewards.Add(reader.ReadUInt());
            rows.Add(new DecodedRow(id, points, claimed, unlocked, rewards.ToArray(), reader.ReadBit()));
        }
        return rows.ToArray();
    }

    private sealed record DecodedRow(uint Id, uint Points, uint Claimed, uint Unlocked, uint[] Rewards, bool Active);
    private sealed class ClosingGameSession() : GameSession(null, null)
    {
        protected override IWritable BuildEncryptedMessage(byte[] data)
            => throw new InvalidOperationException("A closing session must reject presentation before enqueue.");
    }

    private sealed class Fixture
    {
        public IGameSession Session { get; }
        public RecordingDispatchProxy<IGameSession> SessionProxy { get; }
        public IAccount Account { get; }
        public RecordingDispatchProxy<IAccount> AccountProxy { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public RewardTrackPresentation Presentation { get; }
        public IEnumerable<ServerRewardTracksReload> Reloads => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>();
        public Fixture()
        {
            Session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy); SessionProxy = sessionProxy;
            Account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy); AccountProxy = accountProxy;
            accountProxy.SetProperty(nameof(IAccount.Session), Session);
            IPlayer actor = RecordingDispatchProxy<IPlayer>.Create(out var actorProxy);
            PlayerProxy = actorProxy;
            actorProxy.SetProperty(nameof(IPlayer.Account), Account);
            actorProxy.SetProperty(nameof(IPlayer.Session), Session);
            actorProxy.SetProperty(nameof(IPlayer.CharacterId), 1ul);
            Presentation = new RewardTrackPresentation(actor);
        }
    }
}
