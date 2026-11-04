using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Challenges;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Reward;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeRewardWorldReadinessTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ChallengeRewardWorldReadinessTests() { }
    internal ChallengeRewardWorldReadinessTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task EverySupportedCatalogLoadsBeforeMapReadinessWithoutPublishingAheadOfBootstrap(uint track)
    {
        using var f = new Fixture(options); await f.Begin(track); await f.Medal(track, 0, new(1.25f, 7));
        using var h = new Harness(f); int entered = 0;
        h.Session.SetProperty(nameof(IWorldSession.Player), f.Player);
        h.Owner.PrepareForWorld(() =>
        {
            Assert.False(h.Owner.HasPendingRewardOperation);
            Assert.True(f.Inventory.TryAcquireSaveLease(out var save)); save.Dispose();
            Assert.True(h.Owner.CharacterRewardsReady); Assert.NotNull(h.Owner.NormalSaveIdentity); ++entered;
        });
        Assert.Equal(0, entered); Assert.True(h.Owner.HasPendingRewardOperation);
        Assert.True(h.Presentation.PublishAccountTracks([])); Assert.Empty(h.Reloads);
        await h.Drain(); Assert.Equal(1, entered); Assert.Empty(h.Reloads);
        Assert.Equal(125u, h.Owner.CommittedCharacterRewards.Tracks.Single().Points);
        Assert.False(h.Owner.ChooseRewardTrack(track)); // no claim authority before an actual full reload
        h.Owner.SendInitialPackets();
        var reload = Assert.Single(h.Reloads); Assert.Equal(track, Assert.Single(reload.RewardsTracks).RewardTrackId);
        using var read = f.Context(); Assert.Single(read.CharacterChallengeAttempt); Assert.Single(read.CharacterChallengeMedalReceipt);
        Assert.Empty(read.CharacterChallengeRewardClaim); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task PreMapLoadBlocksTimersChoicesProgressAndNormalSavesWithoutMutatingTheLoadedAttempt()
    {
        using var f = new Fixture(options); await f.Begin(16); using var gate = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim();
        using var h = new Harness(f, () => { started.Set(); if (!gate.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); return f.Context(); });
        int entered = 0; h.Owner.PrepareForWorld(() => ++entered); Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        try
        {
            Assert.False(h.Owner.CharacterRewardsReady); h.Owner.Update(400);
            h.Owner.HandleChoice(f.Challenge(16), ChallengeChoice.Abandon); h.Owner.ReceiveShare(f.Challenge(16), 1); h.Owner.ShareWithTarget(f.Challenge(16));
            Assert.False(h.Owner.TryAdvanceProgress(f.Challenge(16), 100)); h.Owner.TryAdvanceCombatKill(1);
            Assert.Equal(300d, Runtime(h.Owner, f.Challenge(16)).ActiveTimer);
            Assert.True(Runtime(h.Owner, f.Challenge(16)).Activated); Assert.Empty(h.Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            using var save = f.Context(); Assert.Throws<InvalidOperationException>(() => h.Owner.Save(save)); Assert.True(save.IsSaveRejected);
        }
        finally { gate.Set(); }
        await h.Drain(); Assert.Equal(1, entered); Assert.True(h.Owner.CharacterRewardsReady);
        using var read = f.Context(); Assert.Equal(0u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(300d, read.CharacterChallenge.Single().ActiveTimerSeconds);
    }

    [Fact]
    public async Task InvalidatedIdentityDiscoveryCannotAdmitAPlayerOrPublishAfterItFinishes()
    {
        using var f = new Fixture(options); using var gate = new ManualResetEventSlim(); using var started = new ManualResetEventSlim();
        using var h = new Harness(f, () => { started.Set(); if (!gate.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); return f.Context(); });
        int entered = 0; h.Owner.PrepareForWorld(() => ++entered); Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        h.Owner.InvalidateRewardOperations(true); gate.Set(); await h.Drain();
        Assert.Equal(0, entered); Assert.False(h.Owner.CharacterRewardsReady); Assert.Empty(h.Reloads);
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack);
    }

    [Fact]
    public async Task FailedInitialLoadDisconnectsWithoutAdmittingOrCreatingRewardRows()
    {
        using var f = new Fixture(options); using var h = new Harness(f, () => throw new IOException("Initial reward identity unavailable."));
        int entered = 0; h.Owner.PrepareForWorld(() => ++entered); await h.Drain();
        Assert.Equal(0, entered); Assert.True(h.Owner.RequiresAuthoritativeReload); Assert.False(h.Owner.CharacterRewardsReady);
        Assert.Single(h.Session.GetInvocations(nameof(IGameSession.ForceDisconnect))); Assert.Empty(h.Reloads);
        using var read = f.Context(); Assert.Empty(read.CharacterChallengeAttempt); Assert.Empty(read.CharacterChallengeRewardTrack);
    }

    [Fact]
    public async Task ReusedReadyOwnerDoesNotReloadOrRunCallbacksInsideItsPersistenceLease()
    {
        using var f = new Fixture(options); int contexts = 0;
        using var h = new Harness(f, () => { Interlocked.Increment(ref contexts); return f.Context(); });
        int ready = 0; h.Owner.PrepareForWorld(() => ++ready); h.Owner.PrepareForWorld(() => ++ready); await h.Drain();
        Assert.Equal(2, ready); int loadedContexts = contexts; Assert.True(loadedContexts >= 2);
        h.Owner.PrepareForWorld(() => { Assert.False(h.Owner.HasPendingRewardOperation); ++ready; });
        Assert.Equal(2, ready); await h.Drain(); Assert.Equal(3, ready); Assert.Equal(loadedContexts, contexts); Assert.Empty(h.Reloads);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ALoadedOwnersReadinessWaitsForLaterQueueOwnershipOrCancelsWithIt(bool cancel)
    {
        using var f = new Fixture(options); using var h = new Harness(f);
        h.Owner.PrepareForWorld(() => { }); await h.Drain();
        int entered = 0; using var held = h.Owner.HoldRewardQueue();
        h.Owner.PrepareForWorld(() => ++entered); h.Events.Update(.01);
        Assert.Equal(0, entered);
        if (cancel) h.Owner.InvalidateRewardOperations(true);
        held.Dispose(); await h.Drain(); Assert.Equal(cancel ? 0 : 1, entered);
    }

    [Theory]
    [InlineData("session")] [InlineData("account")] [InlineData("account-session")]
    [InlineData("character")] [InlineData("logout")]
    public async Task ReadinessCallbackRetainsTheOriginalActorAndAccountBinding(string change)
    {
        using var f = new Fixture(options); using var h = new Harness(f);
        int entered = 0; h.Owner.PrepareForWorld(() => ++entered);
        if (change == "session") f.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (change == "account") f.PlayerProxy.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        if (change == "account-session") ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (change == "character") f.PlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 43ul);
        if (change == "logout")
        {
            var logout = RecordingDispatchProxy<ILogoutManager>.Create(out var proxy); proxy.SetProperty(nameof(ILogoutManager.State), LogoutState.Logout);
            f.PlayerProxy.SetProperty(nameof(IPlayer.LogoutManager), logout);
        }
        await h.Drain(); Assert.Equal(0, entered); Assert.Empty(h.Reloads);
    }

    [Fact]
    public async Task ADisconnectingNetworkSessionCannotPublishItsReadyCallback()
    {
        using var f = new Fixture(options); using var h = new Harness(f, nativeSession: true);
        int entered = 0; h.Owner.PrepareForWorld(() => ++entered);
        var network = Assert.IsType<WorldSession>(h.World); network.ForceDisconnect(); Assert.True(network.IsDisconnecting);
        await h.Drain(); Assert.Equal(0, entered);
    }

    [Theory]
    [InlineData("none")] [InlineData("player")] [InlineData("account")] [InlineData("logout")]
    public async Task RealCharacterSelectionWaitsForTheLoadAndRechecksTheCapturedSessionOwner(string change)
    {
        using var f = new Fixture(options); using var h = new Harness(f);
        var handler = h.CharacterSelectHandler(out var maps); handler.HandleMessage(h.World, h.Select());
        Assert.Same(f.Player, h.World.Player); Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
        if (change == "player") h.Session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        if (change == "account") h.Session.SetProperty(nameof(IWorldSession.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        if (change == "logout")
        {
            var logout = RecordingDispatchProxy<ILogoutManager>.Create(out var proxy); proxy.SetProperty(nameof(ILogoutManager.State), LogoutState.Logout);
            f.PlayerProxy.SetProperty(nameof(IPlayer.LogoutManager), logout);
        }
        await h.Drain();
        if (change == "none")
        {
            var map = Assert.Single(maps.GetInvocations(nameof(IMapManager.AddToMap))); Assert.Same(f.Player, map.Arguments[0]);
            var position = Assert.IsType<MapPosition>(map.Arguments[1]); Assert.Equal(10u, position.Info.Entry.Id);
            Assert.Equal(new System.Numerics.Vector3(1, 2, 3), position.Position);
        }
        else Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Empty(h.Reloads);
    }

    [Fact]
    public async Task AMapAdmissionCallbackFailureDisconnectsInsteadOfLeavingAReadyPregameSessionHanging()
    {
        using var f = new Fixture(options); using var h = new Harness(f);
        var handler = h.CharacterSelectHandler(out var maps);
        maps.SetMethodHandler(nameof(IMapManager.AddToMap), _ => throw new IOException("Map admission unavailable."));
        handler.HandleMessage(h.World, h.Select()); await h.Drain(allowErrors: true);
        Assert.IsType<IOException>(Assert.Single(h.Errors)); Assert.Single(h.Session.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        Assert.False(h.Owner.HasPendingRewardOperation);
    }

    private sealed class Harness : IDisposable
    {
        internal readonly Fixture F;
        internal readonly ChallengeManager Owner;
        internal readonly IWorldSession World;
        internal readonly RecordingDispatchProxy<IWorldSession> Session;
        internal readonly EventQueue Events;
        internal readonly List<Exception> Errors = [];
        internal readonly RewardTrackPresentation Presentation;
        internal IEnumerable<ServerRewardTracksReload> Reloads => Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>();
        internal Harness(Fixture f, Func<CharacterContext> contexts = null, bool nativeSession = false)
        {
            F = f; PrepareInventory(f); _ = Manager(f);
            World = RecordingDispatchProxy<IWorldSession>.Create(out var session); Session = session;
            Events = new();
            if (nativeSession)
            {
                var network = new WorldSession(null, null, null, null);
                typeof(WorldSession).GetProperty(nameof(IWorldSession.Account))!.SetValue(network, f.Player.Account);
                World = network; Events = network.Events;
            }
            session.SetProperty(nameof(IWorldSession.Account), f.Player.Account); session.SetProperty("Events", Events);
            f.PlayerProxy.SetProperty(nameof(IPlayer.Session), World);
            ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), World);
            Events.UnhandledExceptionHandler = Errors.Add;
            Presentation = new(f.Player); f.PlayerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), Presentation);
            using var read = f.Context(); var model = new CharacterModel { Id = 42 };
            foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
            Owner = new(f.Player, model, f.Tables, contexts ?? f.Context);
            f.PlayerProxy.SetProperty(nameof(IPlayer.ChallengeManager), Owner);
        }
        internal CharacterSelectHandler CharacterSelectHandler(out RecordingDispatchProxy<IMapManager> maps)
        {
            // Selection now owns real account admission through initialization.
            // Keep the disposable SQL owner, roster and runtime account aligned.
            ((RecordingDispatchProxy<IAccount>)(object)F.Player.Account).SetProperty(nameof(IAccount.Id), 12u);
            using (var owner = F.Context())
            { owner.Character.Single(row => row.Id == 42).AccountId = 12; owner.SaveChanges(); }
            var cleanup = new NexusForever.Game.CleanupManager();
            var factory = RecordingDispatchProxy<IEntityFactory>.Create(out var fp); fp.SetMethodReturn(nameof(IEntityFactory.CreateEntity), F.Player);
            var map = RecordingDispatchProxy<IMapManager>.Create(out maps);
            ((RecordingDispatchProxy<IGameTableManager>)(object)F.Tables).SetProperty(nameof(IGameTableManager.World), PlayerBindPointTests.Table([new WorldEntry { Id = 10 }]));
            Session.SetProperty(nameof(IWorldSession.Characters), new List<CharacterModel> { new() { Id = 42, AccountId = 12, WorldId = 10, LocationX = 1, LocationY = 2, LocationZ = 3 } });
            return new(cleanup, factory, F.Tables, map);
        }
        internal ClientCharacterSelect Select()
        {
            var select = (ClientCharacterSelect)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientCharacterSelect));
            typeof(ClientCharacterSelect).GetProperty(nameof(ClientCharacterSelect.CharacterId))!.SetValue(select, 42ul); return select;
        }
        internal async Task Drain(bool allowErrors = false)
        {
            var watch = Stopwatch.StartNew();
            do { Events.Update(.01); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), "World readiness did not settle."); await Task.Delay(5); }
            while (Owner.HasPendingRewardOperation || Events.PendingEvents);
            if (!allowErrors) Assert.Empty(Errors);
        }
        public void Dispose() => Owner.InvalidateRewardOperations(true);
    }
}
