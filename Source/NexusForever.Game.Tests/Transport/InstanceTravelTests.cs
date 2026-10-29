using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model.Instance;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using NexusForever.WorldServer.Network.Message.Handler.Instance;

namespace NexusForever.Game.Tests.Transport;

public class InstanceTravelTests
{
    [Fact]
    public void CachedPortalReturnSurvivesRepeatedQueriesBeforeBindFallback()
    {
        var map = new TestContentMap();
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var identity = new Identity { RealmId = 1, Id = 42 };
        pp.SetProperty(nameof(IPlayer.Identity), identity);
        var fallback = new MapPosition();
        pp.SetMethodReturn(nameof(IPlayer.GetOpenWorldReturnPosition), fallback);
        var cache = (Dictionary<Identity, IMapPosition>)typeof(ContentMapInstance)
            .GetField("playerReturnLocations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map)!;
        var portalReturn = new MapPosition();
        cache[identity] = portalReturn;

        Assert.Same(portalReturn, map.Return(player));
        Assert.Same(portalReturn, map.Return(player));
        cache.Clear();
        Assert.Same(fallback, map.Return(player));
    }

    [Fact]
    public void LeaveButtonDelegatesToCurrentInstanceQueue()
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var map = RecordingDispatchProxy<IMapInstance>.Create(out var mp);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        pp.SetProperty(nameof(IPlayer.Map), map);
        sp.SetProperty(nameof(IWorldSession.Player), player);
        var handler = new ClientLeavePendingRemovalInstanceHandler(NullLogger<ClientLeavePendingRemovalInstanceHandler>.Instance);

        handler.HandleMessage(session, new ClientLeavePendingRemovalInstance());

        Assert.Same(player, Assert.Single(mp.GetInvocations(nameof(IMapInstance.LeavePendingRemoval))).Arguments[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LeavingAGroupOnlyRemovesPlayerFromThatGroupsInstance(bool ownsCurrentInstance)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playersProxy);
        playersProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        var groups = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var map = RecordingDispatchProxy<IContentMapInstance>.Create(out var mp);
        var mapLock = RecordingDispatchProxy<IMapLock>.Create(out var lp);
        lp.SetProperty(nameof(IMapLock.Type), MapLockType.Group);
        lp.SetProperty(nameof(IMapLock.InstanceId), Guid.NewGuid());
        mp.SetProperty(nameof(IMapInstance.MapLock), mapLock);
        mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 51 });
        pp.SetProperty(nameof(IPlayer.Map), map);
        var locks = RecordingDispatchProxy<IMapLockManager>.Create(out var locksProxy);
        if (ownsCurrentInstance)
            locksProxy.SetMethodReturn(nameof(IMapLockManager.GetGroupLock), mapLock);
        var handler = new GroupMemberRemovedHandler(players, groups, locks);

        await handler.Handle(new GroupMemberRemovedMessage
        {
            Group = new() { Id = 77, Leader = new() { RealmId = 1, Id = 43 }, Members = [] },
            RemovedMember = new() { Identity = new() { RealmId = 1, Id = 42 } }
        });

        var calls = mp.GetInvocations(nameof(IMapInstance.RequestPendingRemoval));
        Assert.Equal(ownsCurrentInstance ? 1 : 0, calls.Count);
        if (ownsCurrentInstance)
            Assert.Equal(WorldRemovalReason.GroupMembership, calls[0].Arguments[1]);
    }

    private sealed class TestContentMap : ContentMapInstance
    {
        public TestContentMap() : base(RecordingDispatchProxy<IEntityFactory>.Create(out _),
            RecordingDispatchProxy<IPublicEventManager>.Create(out _), null) { }
        public IMapPosition Return(IPlayer player) => GetPlayerReturnLocation(player);
    }
}
