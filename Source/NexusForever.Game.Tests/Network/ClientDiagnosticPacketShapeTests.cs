using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class ClientDiagnosticPacketShapeTests
{
    [Fact]
    public void Client0x003D_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x18);
        var packet = new Client0x003D();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x00C8_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x11223344u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x00C8();

        packet.Read(reader);

        Assert.Equal(0x11223344u, packet.Value);
    }

    [Fact]
    public void Client0x00ED_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x20);
        var packet = new Client0x00ED();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x011B_ReadsMappedBytePayload()
    {
        byte[] packetData = WritePacket(writer => writer.Write((byte)0x7A));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x011B();

        packet.Read(reader);

        Assert.Equal((byte)0x7A, packet.Value);
    }

    [Fact]
    public void Client0x011D_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x10203040u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x011D();

        packet.Read(reader);

        Assert.Equal(0x10203040u, packet.Value);
    }

    [Fact]
    public void Client0x012D_ReadsMappedUInt64Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x1122334455667788ul));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x012D();

        packet.Read(reader);

        Assert.Equal(0x1122334455667788ul, packet.Value);
    }

    [Fact]
    public void Client0x0142_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x10);
        var packet = new Client0x0142();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x0550_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x55667788u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0550();

        packet.Read(reader);

        Assert.Equal(0x55667788u, packet.Value);
    }

    [Fact]
    public void Client0x062A_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x22334455u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x062A();

        packet.Read(reader);

        Assert.Equal(0x22334455u, packet.Value);
    }

    [Fact]
    public void Client0x0634_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x33445566u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0634();

        packet.Read(reader);

        Assert.Equal(0x33445566u, packet.Value);
    }

    [Fact]
    public void Client0x063E_ReadsMappedWideStringPayload()
    {
        byte[] packetData = WritePacket(writer => writer.WriteStringWide("diagnostic text"));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x063E();

        packet.Read(reader);

        Assert.Equal("diagnostic text", packet.Text);
    }

    [Fact]
    public void Client0x0701_ReadsMappedUInt64Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x8877665544332211ul));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0701();

        packet.Read(reader);

        Assert.Equal(0x8877665544332211ul, packet.Value);
    }

    [Fact]
    public void Client0x0760_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x58);
        var packet = new Client0x0760();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x0762_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x10);
        var packet = new Client0x0762();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x07B6_ReadsMappedOpaquePayload()
    {
        byte[] payload = BuildPayload(0x20);
        var packet = new Client0x07B6();

        ReadPacket(packet, payload);

        Assert.Equal(payload, packet.Payload);
    }

    [Fact]
    public void Client0x07E3_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x44556677u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x07E3();

        packet.Read(reader);

        Assert.Equal(0x44556677u, packet.Value);
    }

    [Fact]
    public void Client0x0928_ReadsMappedUInt64Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x7766554433221100ul));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0928();

        packet.Read(reader);

        Assert.Equal(0x7766554433221100ul, packet.Value);
    }

    private static void ReadPacket(IReadable packet, byte[] packetData)
    {
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        packet.Read(reader);
    }

    private static byte[] BuildPayload(int length)
    {
        return Enumerable.Range(0, length)
            .Select(i => (byte)(i + 1))
            .ToArray();
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
