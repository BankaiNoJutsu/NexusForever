using NexusForever.IO.Map;

namespace NexusForever.Game.Tests.IO;

public class MapFileTests
{
    [Fact]
    public void Read_AcceptsZeroGridMapFiles()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            new WritableMapFile("prop_only_world").Write(writer);
        }

        stream.Position = 0;

        var mapFile = new MapFile();
        using var reader = new BinaryReader(stream);
        mapFile.Read(reader);

        Assert.Equal("prop_only_world", mapFile.Asset);
        Assert.Empty(mapFile);
    }
}