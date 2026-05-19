using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class ProtocolRuntimeHardeningTests
{
    [Fact]
    public void ClientResurrectAccept_ReadRejectsUnsupportedResurrectionType()
    {
        byte[] packetData = BuildResurrectionAcceptPacket((ResurrectionType)32);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientResurrectAccept();

        InvalidPacketValueException exception = Assert.Throws<InvalidPacketValueException>(() => message.Read(reader));

        Assert.Contains("Unsupported resurrection accept type", exception.Message);
    }

    [Fact]
    public void ServerEntityCreate_WriteRejectsSpellInitData()
    {
        var packet = CreateEntityCreatePacket();
        packet.SpellInitData.Add(new ServerEntityCreate.SpellInit());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("spell initialisation data", exception.Message);
    }

    [Fact]
    public void ServerEntityCreate_WriteRejectsUnsupportedWorldPlacementType()
    {
        var packet = CreateEntityCreatePacket();
        packet.WorldPlacementData = new ServerEntityCreate.WorldPlacement
        {
            Type = 2
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("WorldPlacement", exception.Message);
    }

    private static ServerEntityCreate CreateEntityCreatePacket()
    {
        return new ServerEntityCreate
        {
            Guid = 99u,
            Type = EntityType.Taxi,
            EntityModel = new TaxiEntityModel()
        };
    }

    private static byte[] BuildResurrectionAcceptPacket(ResurrectionType type)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(42u);
            writer.Write(type, 32u);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static void WritePacket(ServerEntityCreate packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
    }
}
