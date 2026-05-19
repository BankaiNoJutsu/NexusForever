using NexusForever.IO.Area;

namespace NexusForever.Game.Tests.IO;

public class AreaFileTests
{
    [Fact]
    public void Constructor_SkipsUnknownTopLevelChunk()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(0x61726561u);
            writer.Write(0u);
            writer.Write(0x12345678u);
            writer.Write(3u);
            writer.Write(new byte[] { 1, 2, 3 });
        }

        stream.Position = 0;

        var areaFile = new AreaFile(stream);

        Assert.Empty(areaFile.Chunks);
    }
}
