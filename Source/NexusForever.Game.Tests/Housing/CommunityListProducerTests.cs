using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public sealed class CommunityListProducerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HousingEntryEmitsExactlyOneRegisteredListAndExplicitRefreshUsesSamePayload(bool populated)
    {
        var manager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetRandomVisitableCommunities), populated
            ? new IPublicCommunity[] { new PublicCommunity { NeighbourhoodId = 77, Name = "Community", Owner = "Leader" } }
            : Array.Empty<IPublicCommunity>());
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)7);
        var map = new TestMap(manager, realm);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        map.Enter(player);
        var entry = Assert.Single(Lists(sessionProxy));
        Assert.Equal(populated ? 1 : 0, entry.Communities.Count);
        if (populated)
        {
            var row = Assert.Single(entry.Communities);
            Assert.Equal((ushort)7, row.RealmId);
            Assert.Equal(77ul, row.NeighborhoodId);
            Assert.Equal("Community", row.Name);
            Assert.Equal("Leader", row.Owner);
        }
        Assert.Single(managerProxy.GetInvocations(nameof(IGlobalResidenceManager.GetRandomVisitableCommunities)));
        Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0]).OfType<ServerActionBarSet>());

        new ClientHousingRandomCommunityListHandler(manager, realm).HandleMessage(session, new());
        Assert.Equal(2, Lists(sessionProxy).Count);
        Assert.Equal(entry.Communities.Count, Lists(sessionProxy)[1].Communities.Count);
        Assert.Equal(2, managerProxy.GetInvocations(nameof(IGlobalResidenceManager.GetRandomVisitableCommunities)).Count);
    }

    [Fact]
    public void NonPlayerMapAdditionDoesNotBuildOrSendCommunityList()
    {
        var manager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var proxy);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out _);
        var map = new TestMap(manager, realm);
        map.Enter(RecordingDispatchProxy<IGridEntity>.Create(out _));
        Assert.Empty(proxy.GetInvocations(nameof(IGlobalResidenceManager.GetRandomVisitableCommunities)));
    }

    [Fact]
    public void ListBuilderDoesNotEnumerateUnregisteredCommunityCaches()
    {
        var manager = new GlobalResidenceManager();
        var packet = HousingPacketBuilder.BuildRandomCommunityList(manager, 7);
        Assert.Empty(packet.Communities);
    }

    private static List<ServerHousingRandomCommunityList> Lists(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(i => i.Arguments[0]).OfType<ServerHousingRandomCommunityList>().ToList();

    private sealed class TestMap : ResidenceMapInstance
    {
        public TestMap(IGlobalResidenceManager manager, IRealmContext realm)
            : base(null, RecordingDispatchProxy<IPublicEventManager>.Create(out _), null, manager, null, realm, null)
        {
            typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, new WorldEntry { Id = 900 });
            var grids = new IMapGrid[MapDefines.WorldGridCount * MapDefines.WorldGridCount];
            var (x, z) = MapGrid.GetGridCoord(Vector3.Zero);
            grids[z * MapDefines.WorldGridCount + x] = RecordingDispatchProxy<IMapGrid>.Create(out _);
            typeof(BaseMap).GetField("grids", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, grids);
        }

        public void Enter(IGridEntity entity) => AddEntity(entity, Vector3.Zero);
    }
}
