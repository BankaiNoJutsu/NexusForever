using System.Text;
using Microsoft.Data.Sqlite;
using Nexus.Archive;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.MapGenerator;

namespace NexusForever.Game.Tests.IO;

public class ArchiveStreamReaderTests
{
    private static readonly byte[] Plain = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("NexusForever archive compatibility\n", 8)));

    // Python stdlib lzma FORMAT_ALONE: retain the five properties bytes, omit
    // its eight-byte length (the WildStar index supplies that length).
    private static readonly byte[] Lzma = Convert.FromBase64String("XQAAgAAAJxlLB3pUJclluPkLmj2Xl1r4OgjC7wUhn6+2OzfvglualVRe8qIXJ8P//7KyAAA=");
    // Python stdlib zlib with wbits=-15, matching the archive's raw DEFLATE data.
    private static readonly byte[] Deflate = Convert.FromBase64String("80utKC12yy9KLUstUkgsSs7ILEtVSM7PLUgsyUzKzMksqeTyG4lKAA==");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompressedArchiveDataDecodesThroughPatchedDependency(bool lzma)
    {
        byte[] compressed = lzma ? Lzma : Deflate;
        var source = new TrackingStream(compressed);
        var entry = Entry(ArchiveFileFlags.File | (lzma ? ArchiveFileFlags.CompressedLzma : ArchiveFileFlags.CompressedDeflate), compressed.Length);
        using (Stream decoded = ArchiveStreamReader.Decompress(entry, source))
        {
            using var result = new MemoryStream();
            decoded.CopyTo(result);
            Assert.Equal(Plain, result.ToArray());
        }
        Assert.True(source.Disposed);
    }

    [Fact]
    public void LzmaPropertiesSupportStreamsThatReturnShortReads()
    {
        using var source = new TrackingStream(Lzma, shortReads: true);
        using Stream decoded = ArchiveStreamReader.Decompress(Entry(ArchiveFileFlags.CompressedLzma, Lzma.Length), source);
        using var result = new MemoryStream();
        decoded.CopyTo(result);
        Assert.Equal(Plain, result.ToArray());
    }

    [Theory]
    [InlineData(ArchiveFileFlags.File)]
    [InlineData(ArchiveFileFlags.CompressedLzma | ArchiveFileFlags.CompressedDeflate)]
    public void ExistingUncompressedOrUnrecognizedCombinationReturnsOriginalStream(ArchiveFileFlags flags)
    {
        using var source = new TrackingStream(Plain);
        Assert.Same(source, ArchiveStreamReader.Decompress(Entry(flags, Plain.Length), source));
        Assert.False(source.Disposed);
        Assert.Equal(0, source.Position);
    }

    [Fact]
    public void DeclaredLzmaSizeCannotBeShorterThanPropertiesAndDisposesInput()
    {
        var source = new TrackingStream(Lzma);
        Assert.Throws<InvalidDataException>(() => ArchiveStreamReader.Decompress(Entry(ArchiveFileFlags.CompressedLzma, 4), source));
        Assert.True(source.Disposed);
    }

    [Fact]
    public void TruncatedLzmaPropertiesFailWithoutLeakingInput()
    {
        var source = new TrackingStream(Lzma[..3]);
        Assert.Throws<EndOfStreamException>(() => ArchiveStreamReader.Decompress(Entry(ArchiveFileFlags.CompressedLzma, Lzma.Length), source));
        Assert.True(source.Disposed);
    }

    [Fact]
    public void LoadedNativeSqliteContainsAggregateTermBoundsFix()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        var version = Version.Parse((string)command.ExecuteScalar()!);
        Assert.True(version >= new Version(3, 50, 2), $"Loaded vulnerable native SQLite {version}");
    }

    private static IArchiveFileEntry Entry(ArchiveFileFlags flags, long compressedSize)
    {
        var entry = RecordingDispatchProxy<IArchiveFileEntry>.Create(out var proxy);
        proxy.SetProperty(nameof(IArchiveFileEntry.Flags), flags);
        proxy.SetProperty(nameof(IArchiveFileEntry.CompressedSize), compressedSize);
        proxy.SetProperty(nameof(IArchiveFileEntry.UncompressedSize), (long)Plain.Length);
        return entry;
    }

    private sealed class TrackingStream(byte[] bytes, bool shortReads = false) : MemoryStream(bytes)
    {
        public bool Disposed { get; private set; }
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, shortReads ? Math.Min(count, 1) : count);
        public override int Read(Span<byte> buffer) => base.Read(shortReads ? buffer[..Math.Min(buffer.Length, 1)] : buffer);
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
