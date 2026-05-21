using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class ClientDiagnosticPacketShapeTests
{
    [Fact]
    public void Client0x0550_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x55667788u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0550();

        packet.Read(reader);

        Assert.Equal(0x55667788u, packet.Value);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
