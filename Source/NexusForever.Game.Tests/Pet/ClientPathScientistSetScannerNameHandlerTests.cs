using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPathScientistSetScannerNameHandlerTests
{
    private const uint ScanBotProfileId = 77u;
    private const string ScanBotName = "Vector";

    [Fact]
    public void HandleMessage_WithUnlockedScanBotRenamesProfile()
    {
        IWorldSession session = CreateSession(
            RecordingDispatchProxy<IPetCustomisation>.Create(out _),
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPathScientistSetScannerNameHandler();

        handler.HandleMessage(session, ReadSetScannerName(PetType.ScanBot, ScanBotProfileId, ScanBotName));

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation rename =
            Assert.Single(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.RenamePet)));
        Assert.Equal(PetType.ScanBot, rename.Arguments[0]);
        Assert.Equal(ScanBotProfileId, rename.Arguments[1]);
        Assert.Equal(ScanBotName, rename.Arguments[2]);
    }

    [Fact]
    public void HandleMessage_WithNonScanBotTypeThrowsBeforeRename()
    {
        IWorldSession session = CreateSession(
            RecordingDispatchProxy<IPetCustomisation>.Create(out _),
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPathScientistSetScannerNameHandler();

        Assert.Throws<InvalidPacketValueException>(() =>
            handler.HandleMessage(session, ReadSetScannerName(PetType.GroundMount, ScanBotProfileId, ScanBotName)));

        Assert.Empty(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.RenamePet)));
    }

    [Fact]
    public void HandleMessage_WithLockedScanBotProfileThrowsBeforeRename()
    {
        IWorldSession session = CreateSession(
            null,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy);
        var handler = new ClientPathScientistSetScannerNameHandler();

        Assert.Throws<InvalidPacketValueException>(() =>
            handler.HandleMessage(session, ReadSetScannerName(PetType.ScanBot, ScanBotProfileId, ScanBotName)));

        Assert.Empty(petCustomisationProxy.GetInvocations(nameof(IPetCustomisationManager.RenamePet)));
    }

    private static IWorldSession CreateSession(
        IPetCustomisation scanBotCustomisation,
        out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IPetCustomisationManager petCustomisationManager =
            RecordingDispatchProxy<IPetCustomisationManager>.Create(out petCustomisationProxy);

        petCustomisationProxy.SetMethodHandler(nameof(IPetCustomisationManager.GetCustomisation), args =>
            (PetType)args[0] == PetType.ScanBot && (uint)args[1] == ScanBotProfileId
                ? scanBotCustomisation
                : null);
        playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static ClientPathScientistSetScannerName ReadSetScannerName(PetType petType, uint profileId, string name)
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write(petType, 2u);
        writer.Write(profileId);
        writer.WriteStringWide(name);
        writer.FlushBits();

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var packet = new ClientPathScientistSetScannerName();
        packet.Read(reader);
        return packet;
    }
}
