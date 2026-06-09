using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPetCustomisationHandlerTests
{
    [Fact]
    public void HandleMessage_WithValidSlotDelegatesToPetCustomisationManager()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPetCustomisationHandler();

        handler.HandleMessage(session, ReadCustomisation(PetType.ScanBot, 77u, 3, 12));

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation addCustomisation =
            Assert.Single(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.AddCustomisation)));
        Assert.Equal(PetType.ScanBot, addCustomisation.Arguments[0]);
        Assert.Equal(77u, addCustomisation.Arguments[1]);
        Assert.Equal((ushort)3, addCustomisation.Arguments[2]);
        Assert.Equal((ushort)12, addCustomisation.Arguments[3]);
        Assert.Empty(GetMessages<ServerPetCustomisationFailed>(sessionProxy));
    }

    [Fact]
    public void HandleMessage_WithInvalidSlotSendsFailureWithoutDelegating()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPetCustomisationHandler();

        handler.HandleMessage(session, ReadCustomisation(PetType.ScanBot, 77u, 4, 12));

        Assert.Empty(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.AddCustomisation)));
        ServerPetCustomisationFailed failure = Assert.Single(GetMessages<ServerPetCustomisationFailed>(sessionProxy));
        Assert.Equal(PetCustomizeResult.InvalidSlot, failure.Reason);
        Assert.Equal(PetType.ScanBot, failure.Type);
        Assert.Equal(77u, failure.PetUnitId);
        Assert.Equal((ushort)4, failure.FlairSlotIndex);
        Assert.Equal((ushort)12, failure.PetFlairId);
    }

    [Fact]
    public void HandleMessage_WithUnsupportedPetTypeSendsFailureWithoutDelegating()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPetCustomisationHandler();

        handler.HandleMessage(session, ReadCustomisation((PetType)3, 77u, 0, 12));

        Assert.Empty(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.AddCustomisation)));
        ServerPetCustomisationFailed failure = Assert.Single(GetMessages<ServerPetCustomisationFailed>(sessionProxy));
        Assert.Equal(PetCustomizeResult.PetTypeNotSupported, failure.Reason);
        Assert.Equal((PetType)3, failure.Type);
        Assert.Equal(77u, failure.PetUnitId);
        Assert.Equal((ushort)0, failure.FlairSlotIndex);
        Assert.Equal((ushort)12, failure.PetFlairId);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IPetCustomisationManager petCustomisationManager =
            RecordingDispatchProxy<IPetCustomisationManager>.Create(out petCustomisationProxy);

        playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static ClientPetCustomisation ReadCustomisation(PetType petType, uint petObjectId, ushort flairSlotIndex, ushort flairId)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(petType, 2u);
            writer.Write(petObjectId);
            writer.Write(flairSlotIndex);
            writer.Write(flairId, 14u);
            writer.FlushBits();
        }

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var packet = new ClientPetCustomisation();
        packet.Read(reader);
        return packet;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
