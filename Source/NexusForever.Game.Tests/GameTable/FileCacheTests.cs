using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;

namespace NexusForever.Game.Tests.TableContracts;

[CollectionDefinition(nameof(FileCacheTests), DisableParallelization = true)]
public sealed class FileCacheCollection;

[Collection(nameof(FileCacheTests))]
public sealed class FileCacheTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("nexus-file-cache-").FullName;
    private static string ModuleVersion => Convert.ToHexString(typeof(FileCache).Assembly.ManifestModule.ModuleVersionId.ToByteArray());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationFailureLetsTheNextLoadRetry(bool blockVersionFile)
    {
        var path = Path.Combine(root, "cache");
        var source = CreateSource();
        var marker = Path.Combine(path, "cacheInfo.txt");
        if (blockVersionFile)
            Directory.CreateDirectory(marker);
        else
            File.WriteAllText(path, "blocks directory creation");
        Configure(path);

        int creatorCalls = 0;
        string Create(string _) { creatorCalls++; return "parsed"; }
        var error = Record.Exception(() => FileCache.LoadWithCache(source, Create));
        Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString());
        Assert.Equal(0, creatorCalls);

        if (blockVersionFile) Directory.Delete(marker);
        else File.Delete(path);
        Assert.Equal("parsed", FileCache.LoadWithCache(source, Create));
        Assert.Equal("parsed", FileCache.LoadWithCache(source, Create));
        Assert.Equal(1, creatorCalls);
        Assert.Equal(ModuleVersion, File.ReadAllText(marker));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationPreservesOtherFilesOnlyForTheMatchingVersion(bool matchingVersion)
    {
        var path = Directory.CreateDirectory(Path.Combine(root, "cache")).FullName;
        var marker = Path.Combine(path, "cacheInfo.txt");
        var previousFile = Path.Combine(path, "previous.cache");
        File.WriteAllText(marker, matchingVersion ? ModuleVersion : "obsolete");
        File.WriteAllText(previousFile, "old data");
        Configure(path);

        Assert.Equal("parsed", FileCache.LoadWithCache(CreateSource(), _ => "parsed"));
        Assert.Equal(matchingVersion, File.Exists(previousFile));
        Assert.Equal(ModuleVersion, File.ReadAllText(marker));
    }

    [Fact]
    public void ChangingTheCacheDirectoryChecksItsOwnVersionBeforeReusingEntries()
    {
        var source = CreateSource();
        var first = Path.Combine(root, "first");
        var second = Directory.CreateDirectory(Path.Combine(root, "second")).FullName;
        Configure(first);
        Assert.Equal("first value", FileCache.LoadWithCache(source, _ => "first value"));
        var previousEntry = Assert.Single(Directory.GetFiles(first, "*.cache"));
        File.Copy(previousEntry, Path.Combine(second, Path.GetFileName(previousEntry)));
        File.WriteAllText(Path.Combine(second, "cacheInfo.txt"), "obsolete");

        Configure(second);
        Assert.Equal("second value", FileCache.LoadWithCache(source, _ => "second value"));
        Assert.Equal("second value", FileCache.LoadWithCache<string>(source, _ => throw new InvalidOperationException("Expected a cache hit.")));
        Assert.Equal(ModuleVersion, File.ReadAllText(Path.Combine(second, "cacheInfo.txt")));

        Configure(first);
        Assert.Equal("first value", FileCache.LoadWithCache<string>(source, _ => throw new InvalidOperationException("Expected a cache hit.")));
    }

    private static void Configure(string path) => FileCache.Configure(new CacheConfig { CachePath = path });

    private string CreateSource()
    {
        var source = Path.Combine(root, "source.tbl");
        File.WriteAllText(source, "source data");
        return source;
    }

    public void Dispose()
    {
        FileCache.Configure(null);
        Directory.Delete(root, recursive: true);
    }
}
