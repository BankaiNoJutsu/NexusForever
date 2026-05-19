using NexusForever.IO.Map;

namespace NexusForever.Game.Tests.IO;

public class MapFileCellTests
{
    [Fact]
    public void Read_AcceptsKnownNoPayloadFlags()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(1u);
            writer.Write(2u);
            writer.Write(0x0cu);
        }

        stream.Position = 0;

        var cell = new MapFileCell();
        cell.Read(new BinaryReader(stream));

        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void Read_RejectsUnknownFlags()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(1u);
            writer.Write(2u);
            writer.Write(0x20u);
        }

        stream.Position = 0;

        var cell = new MapFileCell();
        Assert.Throws<InvalidDataException>(() => cell.Read(new BinaryReader(stream)));
    }
}
