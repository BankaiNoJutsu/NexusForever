using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Instance;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Instance;

namespace NexusForever.Game.Tests.Group;

public class ClientRaidInfoRequestHandlerTests
{
    [Fact]
    public void HandleMessage_ReturnsEmptyRaidInfoCompatibilityResponse()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 10u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = 10ul
        });
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out RecordingDispatchProxy<IMapLockManager> mapLockManagerProxy);
        mapLockManagerProxy.SetMethodReturn(nameof(IMapLockManager.TryGetSoloLockCollection), null);

        var handler = new ClientRaidInfoRequestHandler(
            NullLogger<ClientRaidInfoRequestHandler>.Instance,
            mapLockManager);

        handler.HandleMessage(session, new ClientRaidInfoRequest());

        ServerRaidInfoResponse response = Assert.Single(GetEncryptedMessages<ServerRaidInfoResponse>(sessionProxy));
        Assert.Empty(response.Raids);
    }

    [Fact]
    public void HandleMessage_WithSoloInstanceLockReturnsRaidInfoRow()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out Identity identity);
        MapLock instanceLock = new(NullLogger<MapLock>.Instance);
        instanceLock.Initialise(MapLockType.Solo, worldId: 1234u);

        MapLock residenceLock = new(NullLogger<MapLock>.Instance);
        residenceLock.Initialise(MapLockType.Residence, worldId: 0u);

        var lockCollection = new MapLockCollection();
        lockCollection.AddMapLock(instanceLock);
        lockCollection.AddMapLock(residenceLock);

        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out RecordingDispatchProxy<IMapLockManager> mapLockManagerProxy);
        mapLockManagerProxy.SetMethodReturn(nameof(IMapLockManager.TryGetSoloLockCollection), lockCollection);

        var handler = new ClientRaidInfoRequestHandler(
            NullLogger<ClientRaidInfoRequestHandler>.Instance,
            mapLockManager);

        handler.HandleMessage(session, new ClientRaidInfoRequest());

        ServerRaidInfoResponse response = Assert.Single(GetEncryptedMessages<ServerRaidInfoResponse>(sessionProxy));
        ServerRaidInfoResponse.RaidInfo raid = Assert.Single(response.Raids);
        Assert.Equal(System.BitConverter.ToUInt64(instanceLock.InstanceId.ToByteArray(), 0), raid.SavedInstanceId);
        Assert.Equal(1234, raid.WorldId);
        Assert.Equal(7f, raid.DaysUntilExpire);
        Assert.Equal(0u, raid.PrimeLevel);
        Assert.True(raid.DateExpireUTC > (ulong)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        RecordingDispatchProxy<IMapLockManager>.Invocation lookup =
            Assert.Single(mapLockManagerProxy.GetInvocations(nameof(IMapLockManager.TryGetSoloLockCollection)));
        Assert.Equal(identity, lookup.Arguments[0]);
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out Identity identity)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        identity = new Identity
        {
            RealmId = 1,
            Id      = 10ul
        };
        playerProxy.SetProperty(nameof(IPlayer.Guid), 10u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }
}
