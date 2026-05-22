using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingVisitResidenceHandlerTests
{
    private const ushort RealmId = 7;
    private const ulong ResidenceId = 0x0102030405060708ul;

    [Fact]
    public void HandleMessage_FromNonResidenceMap_ThrowsInvalidPacket()
    {
        ClientHousingVisitResidenceHandler handler = CreateHandler(out _, out _);
        IWorldSession session = CreateSession(
            residenceMap: false,
            out _,
            out _);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest()));
    }

    [Fact]
    public void HandleMessage_UnknownResidence_SendsVisitFailed()
    {
        ClientHousingVisitResidenceHandler handler = CreateHandler(out _, out _);
        IWorldSession session = CreateSession(
            residenceMap: true,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateRequest());

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(RealmId, result.RealmId);
        Assert.Equal(ResidenceId, result.ResidenceId);
        Assert.Equal(string.Empty, result.PlayerName);
        Assert.Equal(HousingResult.Visit_Failed, result.Result);
    }

    [Fact]
    public void HandleMessage_PrivateResidence_SendsVisitPrivate()
    {
        IResidence residence = CreateResidence(ResidencePrivacyLevel.Private, out _);
        ClientHousingVisitResidenceHandler handler = CreateHandler(out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy, out _);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidence), residence);
        IWorldSession session = CreateSession(
            residenceMap: true,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateRequest());

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Visit_Private, result.Result);
    }

    [Fact]
    public void HandleMessage_PublicResidence_TeleportsToResidenceEntrance()
    {
        IResidence residence = CreateResidence(ResidencePrivacyLevel.Public, out _);
        IResidenceEntrance entrance = CreateEntrance(new WorldEntry { Id = 123u }, new Vector3(1f, 2f, 3f), Quaternion.Identity);
        IResidenceMapLock mapLock = RecordingDispatchProxy<IResidenceMapLock>.Create(out _);

        ClientHousingVisitResidenceHandler handler = CreateHandler(
            out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy,
            out RecordingDispatchProxy<IMapLockManager> mapLockManagerProxy);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidence), residence);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), entrance);
        mapLockManagerProxy.SetMethodReturn(nameof(IMapLockManager.GetResidenceLock), mapLock);
        IWorldSession session = CreateSession(
            residenceMap: true,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateRequest());

        Assert.Empty(GetEncryptedMessages(sessionProxy));
        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(teleport.Arguments[0]);
        Assert.Same(entrance.Entry, position.Info.Entry);
        Assert.Same(mapLock, position.Info.MapLock);
        Assert.Equal(new Vector3(1f, 2f, 3f), position.Position);
    }

    private static ClientHousingVisitResidenceHandler CreateHandler(
        out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy,
        out RecordingDispatchProxy<IMapLockManager> mapLockManagerProxy)
    {
        IGlobalResidenceManager residenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out residenceManagerProxy);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out mapLockManagerProxy);
        return new ClientHousingVisitResidenceHandler(
            NullLogger<ClientHousingVisitResidenceHandler>.Instance,
            residenceManager,
            mapLockManager);
    }

    private static IWorldSession CreateSession(
        bool residenceMap,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IBaseMap map = residenceMap
            ? RecordingDispatchProxy<IResidenceMapInstance>.Create(out _)
            : RecordingDispatchProxy<IBaseMap>.Create(out _);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IGridEntity.Map), map);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        return session;
    }

    private static ClientHousingVisitResidence CreateRequest()
    {
        var request = new ClientHousingVisitResidence();
        request.TargetResidence.RealmId = RealmId;
        request.TargetResidence.ResidenceId = ResidenceId;
        return request;
    }

    private static IResidence CreateResidence(ResidencePrivacyLevel privacyLevel, out RecordingDispatchProxy<IResidence> residenceProxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.PrivacyLevel), privacyLevel);
        residenceProxy.SetProperty(nameof(IResidence.PropertyInfoId), PropertyInfoId.Residence);
        return residence;
    }

    private static IResidenceEntrance CreateEntrance(WorldEntry entry, Vector3 position, Quaternion rotation)
    {
        IResidenceEntrance entrance = RecordingDispatchProxy<IResidenceEntrance>.Create(out RecordingDispatchProxy<IResidenceEntrance> entranceProxy);
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Entry), entry);
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Position), position);
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Rotation), rotation);
        return entrance;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }
}
