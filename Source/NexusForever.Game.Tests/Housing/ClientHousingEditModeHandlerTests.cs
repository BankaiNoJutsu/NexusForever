using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingEditModeHandlerTests
{
    private const ushort RealmId = 7;
    private const ulong TargetCharacterId = 1234ul;

    [Fact]
    public void HandleMessage_FromNonResidenceMap_ThrowsInvalidPacket()
    {
        ClientHousingEditModeHandler handler = CreateHandler(out _);
        IWorldSession session = CreateSession(
            RecordingDispatchProxy<IBaseMap>.Create(out _),
            out _,
            out _);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(true)));
    }

    [Fact]
    public void HandleMessage_UnknownResidence_ThrowsInvalidPacket()
    {
        ClientHousingEditModeHandler handler = CreateHandler(out _);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out RecordingDispatchProxy<IResidenceMapInstance> mapProxy);
        IWorldSession session = CreateSession(map, out _, out _);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(true)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IResidenceMapInstance.SetEditMode)));
    }

    [Fact]
    public void HandleMessage_WithoutModifyPermission_ThrowsInvalidPacket()
    {
        ClientHousingEditModeHandler handler = CreateHandler(out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy);
        IResidence residence = CreateResidence(canModify: false, out _);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), residence);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out RecordingDispatchProxy<IResidenceMapInstance> mapProxy);
        IWorldSession session = CreateSession(map, out _, out _);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(true)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IResidenceMapInstance.SetEditMode)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandleMessage_WithModifyPermission_StoresMapEditMode(bool enabled)
    {
        ClientHousingEditModeHandler handler = CreateHandler(out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy);
        IResidence residence = CreateResidence(canModify: true, out _);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), residence);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out RecordingDispatchProxy<IResidenceMapInstance> mapProxy);
        IWorldSession session = CreateSession(map, out IPlayer player, out _);

        handler.HandleMessage(session, CreateRequest(enabled));

        RecordingDispatchProxy<IResidenceMapInstance>.Invocation editMode =
            Assert.Single(mapProxy.GetInvocations(nameof(IResidenceMapInstance.SetEditMode)));
        Assert.Same(player, editMode.Arguments[0]);
        Assert.Same(residence, editMode.Arguments[1]);
        Assert.Equal(enabled, editMode.Arguments[2]);
    }

    private static ClientHousingEditModeHandler CreateHandler(out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy)
    {
        IGlobalResidenceManager residenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out residenceManagerProxy);
        return new ClientHousingEditModeHandler(
            NullLogger<ClientHousingEditModeHandler>.Instance,
            residenceManager);
    }

    private static IWorldSession CreateSession(
        IBaseMap map,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 5678ul);
        return session;
    }

    private static IResidence CreateResidence(bool canModify, out RecordingDispatchProxy<IResidence> residenceProxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), TargetCharacterId);
        residenceProxy.SetMethodReturn(nameof(IResidence.CanModifyResidence), canModify);
        return residence;
    }

    private static ClientHousingEditMode CreateRequest(bool enabled)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            var identity = new NexusForever.Network.World.Message.Model.Shared.Identity
            {
                RealmId = RealmId,
                Id      = TargetCharacterId
            };
            identity.Write(writer);
            writer.Write(enabled);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        ClientHousingEditMode request = new();
        request.Read(reader);
        return request;
    }
}
