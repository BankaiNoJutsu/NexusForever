using NexusForever.Network;

namespace NexusForever.Game.Tests.Network;

public class GamePacketCodecPerformanceTests
{
    [Fact]
    public void WriteBytes_PreservesUnalignedBitPacking()
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(true);
            writer.WriteBytes(new byte[] { 0xAC });
            writer.FlushBits();
        }

        Assert.Equal(new byte[] { 0x59, 0x01 }, stream.ToArray());
    }

    [Fact]
    public void ReadBytes_RoundTripsUnalignedPayload()
    {
        byte[] packetData;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(true);
                writer.WriteBytes(new byte[] { 0xAC });
                writer.FlushBits();
            }

            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.True(reader.ReadBit());
        Assert.Equal(new byte[] { 0xAC }, reader.ReadBytes(1u));
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void AlignedBytePayloadsPreserveSequentialWireLayout()
    {
        byte[] payload = [0x78, 0x56, 0x34, 0x12, 0xEF, 0xBE];

        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.WriteBytes(payload);
                writer.FlushBits();
            }

            Assert.Equal(payload, stream.ToArray());
        }

        using var reader = new GamePacketReader(new MemoryStream(payload));

        Assert.Equal(0x12345678u, reader.ReadUInt());
        Assert.Equal(new byte[] { 0xEF, 0xBE }, reader.ReadBytes(2u));
        Assert.Equal(0u, reader.BytesRemaining);
    }
}
