using System.Runtime.InteropServices;
using System.Text;
using NexusForever.Game.Static;
using NexusForever.GameTable;

namespace NexusForever.Game.Tests.Network;

public class UpstreamTextTableTests
{
    [Fact]
    public void StreamAndPathLoadTheSameUnicodeTextAndSparseIds()
    {
        byte[] text = Encoding.Unicode.GetBytes("Hello \U0001F30D\0");
        int headerSize = Marshal.SizeOf<TextTableHeader>();
        var header = new TextTableHeader
        {
            Signature = 0x4C544558,
            Language = Language.English,
            RecordCount = 1,
            StringTableOffset = 8,
            StringTableLength = (ulong)text.Length / 2
        };
        byte[] bytes = new byte[headerSize + 8 + text.Length];
        MemoryMarshal.Write(bytes.AsSpan(), in header);
        BitConverter.GetBytes(7u).CopyTo(bytes, headerSize);
        text.CopyTo(bytes, headerSize + 8);

        var input = new MemoryStream(bytes);
        var fromStream = new TextTable(input);
        Assert.False(input.CanRead); // Upstream transfers ownership to TextTable.
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, bytes);
            var fromPath = new TextTable(path);
            Assert.Equal("Hello \U0001F30D", fromStream.GetEntry(7));
            Assert.Equal(fromPath.GetEntry(7), fromStream.GetEntry(7));
            Assert.Equal(Language.English, Assert.Single(fromStream.Entries).Language);
            Assert.Null(fromStream.GetEntry(0));
            Assert.Null(fromStream.GetEntry(8));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(104)]
    public void InvalidStreamStillRejectsTruncatedHeadersAndWrongSignatures(int length)
    {
        Assert.Throws<InvalidDataException>(() => new TextTable(new MemoryStream(new byte[length])));
    }
}
