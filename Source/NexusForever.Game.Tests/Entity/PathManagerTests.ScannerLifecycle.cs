using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.IO.Map;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public partial class PathManagerTests
{
    [Fact]
    public void ScannerLifecycle_UnrelatedUnsummonCannotRetireTheActiveInstance()
    {
        var f = Lifecycle(); f.Deploy(); f.Add();
        var alias = RecordingDispatchProxy<IWorldEntity>.Create(out var other);
        other.SetProperty(nameof(IWorldEntity.Guid), 9001u);
        other.SetProperty(nameof(IWorldEntity.SummonerGuid), (uint?)500u);
        f.Owner.OnUnsummon(alias);
        f.Dismiss();
        Assert.Single(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)));
        Assert.Same(f.Bot, f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove))[0].Arguments[0]);
    }

    [Fact]
    public void ScannerLifecycle_DismissRemovesTheOriginalObjectAfterMapChange()
    {
        var f = Lifecycle(); f.Deploy(); f.Add();
        var alias = RecordingDispatchProxy<IWorldEntity>.Create(out var other);
        other.SetProperty(nameof(IWorldEntity.Guid), 9001u); other.SetProperty(nameof(IWorldEntity.SummonerGuid), (uint?)500u);
        var replacement = RecordingDispatchProxy<IBaseMap>.Create(out var newMap);
        newMap.SetMethodReturn(nameof(IBaseMap.GetEntity), alias);
        f.Player.SetProperty(nameof(IPlayer.Map), replacement);
        f.Dismiss();
        Assert.Single(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)));
        Assert.Empty(other.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
        Assert.Single(f.States, row => row.ScanbotUnitId == 9001);
    }

    [Theory]
    [InlineData("map")] [InlineData("session")] [InlineData("missing-owner")]
    public void ScannerLifecycle_QueuedAddCannotAdoptAChangedOwner(string change)
    {
        var f = Lifecycle(); var observer = Observe(f); f.Deploy();
        if (change == "map") f.Player.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IBaseMap>.Create(out _));
        if (change == "session") f.Player.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
        if (change == "missing-owner") f.OwnerPresent = false;
        f.Add();
        Assert.Single(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)));
        Assert.Empty(f.States);
        Assert.Empty(observer.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        f.Bot.OnRemoveFromMap();
        f.OwnerPresent = true; f.Player.SetProperty(nameof(IPlayer.Map), f.Map); f.Player.SetProperty(nameof(IPlayer.Session), f.Session);
        f.Deploy();
        Assert.Equal(2, f.Factory.GetInvocations(nameof(IEntityFactory.CreateEntity)).Count);
    }

    [Fact]
    public void ScannerLifecycle_CurrentQueuedAddPublishesToNearbyPlayer()
    {
        var f = Lifecycle(); var observer = Observe(f); f.Deploy(); f.Add();
        var create = Assert.Single(observer.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerEntityCreate>());
        Assert.Equal(9001u, create.Guid);
        Assert.Equal(9001u, Assert.Single(f.States).ScanbotUnitId);
    }

    [Fact]
    public void ScannerLifecycle_DeployRejectsAnAccountAlreadyReboundToAnotherSession()
    {
        var f = Lifecycle();
        var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 42u);
        accountProxy.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        f.Player.SetProperty(nameof(IPlayer.Account), account);
        f.Deploy();
        Assert.Empty(f.Factory.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(f.States);
        accountProxy.SetProperty(nameof(IAccount.Session), f.Session);
        f.Deploy(); f.Add();
        Assert.Single(f.Factory.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Equal(9001u, Assert.Single(f.States).ScanbotUnitId);
    }

    private static RecordingDispatchProxy<IGameSession> Observe(LifecycleFixture fixture)
    {
        var observer = new Player(RecordingDispatchProxy<NexusForever.Game.Abstract.Entity.Movement.IMovementManager>.Create(out _), null, null, null, null, null, null);
        var session = RecordingDispatchProxy<IGameSession>.Create(out var messages);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(observer, session);
        fixture.MapProxy.SetMethodReturn(nameof(IBaseMap.Search), new IGridEntity[] { observer });
        return messages;
    }

    [Fact]
    public async Task ScannerLifecycle_RejectedDismissStateStillRemovesAndRetriesFreshState()
    {
        var f = Lifecycle(); f.Deploy(); f.Add(); int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPathScientistScanbotState packet && ++attempts == 1)
            {
                packet.ScanbotUnitId = 999;
                throw new IOException("Scanner dismissal state refused before acceptance.");
            }
            return null;
        });
        Assert.Null(Record.Exception(f.Dismiss));
        Assert.Single(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)));
        f.Bot.OnRemoveFromMap();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (f.Events.PendingEvents && watch.Elapsed < TimeSpan.FromSeconds(5)) { f.Events.Update(.01); await Task.Delay(5); }
        Assert.False(f.Events.PendingEvents); Assert.Equal(2, attempts);
        Assert.Equal(0u, f.States.Last().ScanbotUnitId);
    }

    [Fact]
    public async Task ScannerLifecycle_NewDeploymentSupersedesAnOldDismissRetry()
    {
        var f = Lifecycle(); f.Deploy(); f.Add(); bool refused = false;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPathScientistScanbotState { ScanbotUnitId: 0 } && !refused)
            {
                refused = true;
                throw new IOException("Old dismissal state refused before new deployment.");
            }
            return null;
        });
        Assert.Null(Record.Exception(f.Dismiss)); f.Bot.OnRemoveFromMap();
        f.Deploy(); f.Add();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (f.Events.PendingEvents && watch.Elapsed < TimeSpan.FromSeconds(5)) { f.Events.Update(.01); await Task.Delay(5); }
        Assert.False(f.Events.PendingEvents);
        Assert.Equal(9001u, f.States.Last().ScanbotUnitId);
        Assert.Equal(3, f.States.Count());
    }

    [Fact]
    public void ScannerLifecycle_FailedMapCallbackStillRetiresItsOriginalState()
    {
        var f = Lifecycle(); f.Deploy(); f.Add();
        f.Player.SetMethodHandler(nameof(IPlayer.OnUnsummon), _ => throw new IOException("Map unsummon callback failed."));
        Assert.Throws<IOException>(() => f.Bot.OnRemoveFromMap());
        Assert.Equal(0u, f.States.Last().ScanbotUnitId);
        f.Dismiss();
        Assert.Empty(f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)));
    }

    [Fact]
    public void ScannerLifecycle_OriginalCallbacksAndPendingDismissRemainSupported()
    {
        var f = Lifecycle(); f.Deploy(); f.Add();
        Assert.Equal(9001u, Assert.Single(f.States).ScanbotUnitId);
        f.Dismiss(); f.Bot.OnRemoveFromMap();
        Assert.Equal(0u, f.States.Last().ScanbotUnitId);
        f.Deploy(); f.Dismiss(); f.Add();
        Assert.Equal(2, f.MapProxy.GetInvocations(nameof(IBaseMap.EnqueueRemove)).Count);
    }

    private LifecycleFixture Lifecycle()
    {
        BuildScanbotGameTableProvider();
        var movement = RecordingDispatchProxy<NexusForever.Game.Abstract.Entity.Movement.IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(NexusForever.Game.Abstract.Entity.Movement.IMovementManager.GetNetworkEntityCommands), Array.Empty<INetworkEntityCommand>());
        var bot = new ScannerUnitEntity(movement);
        bot.InitialiseRuntimeDependencies(null, null, null, () => configuredGameTableManager);
        var factory = CreateEntityFactory(bot, out var factoryProxy);
        var manager = CreateManager(Path.Scientist, 0, 1, out var owner, out var player, out _, out _, out _, entityFactory: factory);
        ConfigureScanbotPlayer(player, bot, true, out var map);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); var events = new EventQueue();
        messages.SetProperty(nameof(IWorldSession.Player), owner); messages.SetProperty(nameof(IWorldSession.Events), events);
        player.SetProperty(nameof(IPlayer.Session), session);
        var f = new LifecycleFixture(owner, player, bot, owner.Map, map, session, messages, events, factoryProxy);
        map.SetProperty(nameof(IBaseMap.File), new MapFile());
        map.SetMethodReturn(nameof(IBaseMap.Search), Array.Empty<IGridEntity>());
        map.SetMethodHandler(nameof(IBaseMap.GridSearch), args => { args[2] = new List<IMapGrid>(); return null; });
        map.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 500 ? f.OwnerPresent ? owner : null : bot);
        map.SetMethodHandler(nameof(IBaseMap.EnqueueRemove), args => { ((IWorldEntity)args[0]).OnEnqueueRemoveFromMap(); return null; });
        player.SetMethodHandler(nameof(IPlayer.OnSummon), args => { manager.OnScientistScanbotSummoned((IWorldEntity)args[0]); return null; });
        player.SetMethodHandler(nameof(IPlayer.OnUnsummon), args => { manager.OnScientistScanbotUnsummoned((IWorldEntity)args[0]); return null; });
        return f;
    }

    private sealed class LifecycleFixture(IPlayer owner, RecordingDispatchProxy<IPlayer> player,
        ScannerUnitEntity bot, IBaseMap map, RecordingDispatchProxy<IBaseMap> mapProxy, IWorldSession session,
        RecordingDispatchProxy<IWorldSession> messages, EventQueue events, RecordingDispatchProxy<IEntityFactory> factory)
    {
        internal readonly IPlayer Owner = owner;
        internal readonly RecordingDispatchProxy<IPlayer> Player = player;
        internal readonly ScannerUnitEntity Bot = bot;
        internal readonly IBaseMap Map = map;
        internal readonly RecordingDispatchProxy<IBaseMap> MapProxy = mapProxy;
        internal readonly IWorldSession Session = session;
        internal readonly RecordingDispatchProxy<IWorldSession> Messages = messages;
        internal readonly EventQueue Events = events;
        internal readonly RecordingDispatchProxy<IEntityFactory> Factory = factory;
        internal bool OwnerPresent = true;
        internal IEnumerable<ServerPathScientistScanbotState> States => Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerPathScientistScanbotState>();
        internal void Deploy()
        {
            using var bytes = new MemoryStream();
            using (var writer = new GamePacketWriter(bytes)) { writer.Write(77u); writer.Write(false); writer.FlushBits(); }
            using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
            var packet = new ClientPathScientistRequestScanbot(); packet.Read(reader);
            new ClientPathScientistRequestScanbotHandler(NullLogger<ClientPathScientistRequestScanbotHandler>.Instance).HandleMessage(Session, packet);
        }
        internal void Dismiss() => new ClientPathScientistDismissScanbotHandler(NullLogger<ClientPathScientistDismissScanbotHandler>.Instance)
            .HandleMessage(Session, new ClientPathScientistDismissScanbot());
        internal void Add() => Bot.OnAddToMap(Map, 9001, Vector3.Zero);
    }
}
