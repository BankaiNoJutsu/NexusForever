using System.Runtime.InteropServices;
using NexusForever.GameTable;
using NexusForever.MapGenerator;

namespace NexusForever.Game.Tests.IO;

public class AssetPreparationTests
{
    [Fact]
    public void FailedGenerationRetainsOriginalAssetsAndMarksCacheIncomplete()
    {
        using var f = new Fixture();
        f.WriteAssets(f.Root);
        byte[] before = File.ReadAllBytes(Path.Combine(f.Root, "map", "World.nfmap"));
        Assert.Throws<InvalidOperationException>(() => f.Run(p =>
        {
            f.WriteAssets(p.OutputDir);
            throw new InvalidOperationException("extract failed");
        }));
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(f.Root, "map", "World.nfmap")));
        Assert.True(File.Exists(Path.Combine(f.Root, AssetPreparation.IncompleteFile)));
        Assert.Empty(Directory.GetDirectories(f.Root, ".nexusforever-staging-*"));
        Assert.Throws<InvalidDataException>(() => AssetPreparation.ValidateMaps(Path.Combine(f.Root, "map")));
    }

    [Fact]
    public void InvalidGeneratedAssetsAreNeverPromoted()
    {
        using var f = new Fixture();
        f.WriteAssets(f.Root);
        byte[] before = File.ReadAllBytes(Path.Combine(f.Root, "tbl", "World.tbl"));
        Assert.Throws<EndOfStreamException>(() => f.Run(p =>
        {
            f.WriteAssets(p.OutputDir);
            File.WriteAllBytes(Path.Combine(p.OutputDir, "tbl", "World.tbl"), [1, 2]);
        }));
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(f.Root, "tbl", "World.tbl")));
        Assert.False(File.Exists(Path.Combine(f.Root, AssetPreparation.InventoryFile)));
    }

    [Fact]
    public void RepairCompletesThroughStagingAndVerifiedCacheNeedsNoClientOrRegeneration()
    {
        using var f = new Fixture();
        int generated = 0;
        f.Run(p =>
        {
            Assert.NotEqual(f.Root, p.OutputDir);
            Assert.True(p.Extract && p.Generate);
            Assert.Equal(3, p.MaxParallelism);
            f.WriteAssets(p.OutputDir);
            generated++;
        });
        Assert.Equal(f.Root, f.Parameters.OutputDir);
        Assert.False(File.Exists(Path.Combine(f.Root, AssetPreparation.IncompleteFile)));
        f.Parameters.PatchPath = null;
        f.Run(_ => throw new Exception("A verified cache must be reused."));
        Assert.Equal(1, generated);
    }

    [Fact]
    public void SameSizeCorruptionAndMissingFilesFailInventoryValidation()
    {
        using var f = new Fixture();
        f.Run(p => f.WriteAssets(p.OutputDir));
        string path = Path.Combine(f.Root, "map", "World.nfmap");
        byte[] bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 0x10;
        File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidDataException>(() => AssetPreparation.ValidateInventory(f.Root));
        File.Delete(path);
        Assert.Throws<FileNotFoundException>(() => AssetPreparation.ValidateInventory(f.Root));
    }

    [Fact]
    public void UnrelatedCustomAssetsAndPrepareDirectoryArePreserved()
    {
        using var f = new Fixture();
        f.WriteAssets(f.Root);
        string custom = Path.Combine(f.Root, "map", "Custom.nfmap");
        File.Copy(Path.Combine(f.Root, "map", "World.nfmap"), custom);
        Directory.CreateDirectory(Path.Combine(f.Root, ".prepare"));
        File.WriteAllText(Path.Combine(f.Root, ".prepare", "user.txt"), "keep");
        f.Run(p => f.WriteAssets(p.OutputDir));
        Assert.True(File.Exists(custom));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(f.Root, ".prepare", "user.txt")));
    }

    [Fact]
    public void ConcurrentPreparationIsRejectedBeforeGeneration()
    {
        using var f = new Fixture();
        using var held = File.Open(Path.Combine(f.Root, ".nexusforever-assets.lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => f.Run(_ => throw new Exception("must not run")));
        Assert.False(File.Exists(Path.Combine(f.Root, AssetPreparation.IncompleteFile)));
    }

    [Fact]
    public void InvalidRetainedCustomMapDoesNotCertifySuccessfulPreparation()
    {
        using var f = new Fixture();
        f.WriteAssets(f.Root);
        string custom = Path.Combine(f.Root, "map", "Custom.nfmap");
        File.WriteAllBytes(custom, [1, 2, 3]);
        Assert.Throws<EndOfStreamException>(() => f.Run(p => f.WriteAssets(p.OutputDir)));
        Assert.True(File.Exists(custom));
        Assert.True(File.Exists(Path.Combine(f.Root, AssetPreparation.IncompleteFile)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FilteredPreparationCannotCertifyCompleteCache(int filter)
    {
        using var f = new Fixture();
        if (filter == 0) f.Parameters.WorldId = 3460;
        if (filter == 1) f.Parameters.GridX = 1;
        if (filter == 2) f.Parameters.GridY = 1;
        Assert.Throws<ArgumentException>(() => f.Run(_ => throw new Exception("must not run")));
    }

    [Theory]
    [InlineData("tbl/../outside.tbl")]
    [InlineData("tbl/..\\outside.tbl")]
    [InlineData("../World.tbl")]
    public void InventoryCannotReferenceOutsideAssetFolders(string path)
    {
        using var f = new Fixture();
        File.WriteAllText(Path.Combine(f.Root, AssetPreparation.InventoryFile),
            System.Text.Json.JsonSerializer.Serialize(new { Version = 1, Files = new Dictionary<string, string> { [path] = "bad" } }));
        Assert.Throws<InvalidDataException>(() => AssetPreparation.ValidateInventory(f.Root));
    }

    [Fact]
    public void HeaderRangesAndSignatureAreValidatedBeforeUse()
    {
        using var f = new Fixture();
        string path = Path.Combine(f.Root, "table.tbl");
        WriteHeader(path, new GameTableHeader { Signature = 0x4454424C });
        AssetPreparation.ValidateTableHeader(path);
        foreach (GameTableHeader invalid in new[]
        {
            new GameTableHeader(),
            new GameTableHeader { Signature = 0x4454424C, FieldCount = ulong.MaxValue },
            new GameTableHeader { Signature = 0x4454424C, MaxId = 1 },
            new GameTableHeader { Signature = 0x4454424C, RecordCount = 1, RecordSize = 4 }
        })
        {
            WriteHeader(path, invalid);
            Assert.Throws<InvalidDataException>(() => AssetPreparation.ValidateTableHeader(path));
        }
    }

    [Fact]
    public void WrongMapBuildAndTruncatedGridDataAreRejected()
    {
        using var f = new Fixture();
        f.WriteAssets(f.Root);
        string path = Path.Combine(f.Root, "map", "World.nfmap");
        byte[] bytes = File.ReadAllBytes(path);
        bytes[8] = 0;
        File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidDataException>(() => AssetPreparation.ValidateMaps(Path.Combine(f.Root, "map")));
        f.WriteAssets(f.Root);
        bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..^1]);
        Assert.Throws<EndOfStreamException>(() => AssetPreparation.ValidateMaps(Path.Combine(f.Root, "map")));
    }

    private static void WriteHeader(string path, GameTableHeader header)
    {
        byte[] bytes = new byte[Marshal.SizeOf<GameTableHeader>()];
        MemoryMarshal.Write(bytes, in header);
        File.WriteAllBytes(path, bytes);
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "nf-assets-" + Guid.NewGuid().ToString("N"));
        public Parameters Parameters { get; }
        public Fixture()
        {
            Directory.CreateDirectory(Root);
            string patch = Path.Combine(Root, "Patch");
            Directory.CreateDirectory(patch);
            File.WriteAllText(Path.Combine(patch, "ClientData.index"), "fixture");
            Parameters = new Parameters { OutputDir = Root, PatchPath = Root, MaxParallelism = 3 };
        }
        public void Run(Action<Parameters> generate) => AssetPreparation.Run(Parameters, generate, (path, inventory) =>
        {
            AssetPreparation.ValidateTableHeader(Path.Combine(path, "tbl", "World.tbl"));
            AssetPreparation.ValidateMaps(Path.Combine(path, "map"));
            if (inventory) AssetPreparation.ValidateInventory(path);
        });
        public void WriteAssets(string path)
        {
            Directory.CreateDirectory(Path.Combine(path, "tbl"));
            Directory.CreateDirectory(Path.Combine(path, "map"));
            WriteHeader(Path.Combine(path, "tbl", "World.tbl"), new GameTableHeader { Signature = 0x4454424C });
            using var writer = new BinaryWriter(File.Create(Path.Combine(path, "map", "World.nfmap")));
            writer.Write(0x504D464Eu); writer.Write(4u); writer.Write(16042u); writer.Write("World"); writer.Write(0u);
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
