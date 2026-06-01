using System.Text.RegularExpressions;

namespace NexusForever.Game.Tests.Data;

public class LaughingWsStoreCatalogSeedTests
{
    private static readonly uint[] FeaturedBackfillOfferGroupIds =
    [
        1727u,
        2807u,
        2808u,
        3123u,
        3124u,
        3125u,
        3209u,
        3223u,
        3312u,
        3315u,
        3318u,
        3332u,
    ];

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

        Match section = Regex.Match(
            seed,
            @"-- store_offer_item_data:.*?(?=\r?\n\r?\n-- store_offer_item_price:)",
            RegexOptions.Singleline);
        Assert.True(section.Success);

        MatchCollection itemRows = Regex.Matches(
            section.Value,
            @"\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
        Assert.NotEmpty(itemRows);

        foreach (Match match in itemRows)
        {
            uint itemId = uint.Parse(match.Groups[2].Value);
            uint type = uint.Parse(match.Groups[3].Value);
            Assert.False(type == 0u && itemId == 86919u);
        }
    }

    [Fact]
    public void StoreCatalogSeed_BackfillsFeaturedWithClientVisibleOfferGroups()
    {
        string seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Tools",
            "DataMapping",
            "sql",
            "laughingws_store_catalog_seed.sql"));

        foreach (uint offerGroupId in FeaturedBackfillOfferGroupIds)
            Assert.Contains($"({offerGroupId}, 76,", seed);
    }

    [Fact]
    public void StoreCatalogSeed_SeedsOfferPricesAsCurrentlyActive()
    {
        string seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Tools",
            "DataMapping",
            "sql",
            "laughingws_store_catalog_seed.sql"));

        Match section = Regex.Match(
            seed,
            @"-- store_offer_item_price:.*?(?=\r?\n\r?\n-- Skipped rows:|\r?\n\r?\nCOMMIT;)",
            RegexOptions.Singleline);
        Assert.True(section.Success);

        MatchCollection priceRows = Regex.Matches(
            section.Value,
            @"\((\d+),\s*\d+,\s*-?\d+(?:\.\d+)?,\s*\d+,\s*-?\d+(?:\.\d+)?,\s*-?\d+,\s*(-?\d+)\)");
        Assert.NotEmpty(priceRows);

        foreach (Match priceRow in priceRows)
        {
            uint offerId = uint.Parse(priceRow.Groups[1].Value);
            long expiry = long.Parse(priceRow.Groups[2].Value);
            Assert.True(expiry <= 0L, $"Offer {offerId} has positive expiry {expiry}.");
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
