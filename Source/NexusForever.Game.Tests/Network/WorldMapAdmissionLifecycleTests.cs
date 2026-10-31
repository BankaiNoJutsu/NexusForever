using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Network;

public sealed class WorldMapAdmissionLifecycleTests
{
    [Fact]
    public void EnteredWorldBeforeMapAssignmentCannotAcknowledgeOrInvokePlayerEntry()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), true);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        new ClientEnteredWorldHandler().HandleMessage(session, new ClientEnteredWorld());

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.OnEnteredWorld)));
    }

    [Fact]
    public void FinishedPlayerInMapManagerQueueCannotCreateOrEnterAMap()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ILogoutManager logout = RecordingDispatchProxy<ILogoutManager>.Create(out var logoutProxy);
        logoutProxy.SetProperty(nameof(ILogoutManager.State), LogoutState.Finished);
        playerProxy.SetProperty(nameof(IPlayer.LogoutManager), logout);
        playerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        IMapFactory factory = RecordingDispatchProxy<IMapFactory>.Create(out var factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IMapFactory.CreateMap), map);
        var manager = new MapManager(factory);

        manager.AddToMap(player, new MapPosition { Info = new MapInfo { Entry = new WorldEntry { Id = 3460 } } });
        manager.Update(0.01);

        Assert.Empty(factoryProxy.GetInvocations(nameof(IMapFactory.CreateMap)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }
}
