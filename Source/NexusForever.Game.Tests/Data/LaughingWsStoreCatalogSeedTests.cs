using System.Text.RegularExpressions;

namespace NexusForever.Game.Tests.Data;

public class LaughingWsStoreCatalogSeedTests
{
    [Fact]
    public void StoreCatalogSeed_DoesNotEmitDungeonChaseUnsupportedType0AccountItem()
    {
        string seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Tools",
            "DataMapping",
            "sql",
            "laughingws_store_catalog_seed.sql"));

        Assert.Contains("account item 86919 exceeds current 15-bit store wire/schema proof", seed);

        foreach (Match match in Regex.Matches(seed, @"\((\d+),(\d+),(\d+),(\d+)\)"))
        {
            uint itemId = uint.Parse(match.Groups[2].Value);
            uint type = uint.Parse(match.Groups[3].Value);
            Assert.False(type == 0u && itemId == 86919u);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Source", "NexusForever.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate NexusForever repository root.");
    }
}
