using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NexusForever.WorldServer.Web.Middleware;

namespace NexusForever.Game.Tests.Storefront;

public class StorefrontBannerFeedTests
{
    [Fact]
    public void BannerFeed_ReturnsDefaultSplashBanners()
    {
        Assert.True(StorefrontBannerFeed.TryGetResponse("/banners/data.json", out StorefrontBannerResponse response));
        Assert.StartsWith("application/json", response.ContentType);

        using JsonDocument document = JsonDocument.Parse(response.Content);
        JsonElement root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("level_50_boost", out JsonElement level50Banner));
        Assert.Equal(0, level50Banner.GetProperty("location").GetInt32());
        Assert.Equal(0, level50Banner.GetProperty("type").GetInt32());
        Assert.Equal(2718, level50Banner.GetProperty("sup_id").GetInt32());
        Assert.Equal("Level 50 Character Boost", level50Banner.GetProperty("title").GetProperty("en").GetString());

        Assert.Contains(root.EnumerateObject(), property => property.Value.GetProperty("location").GetInt32() == 1);
        Assert.Contains(root.EnumerateObject(), property => property.Value.GetProperty("location").GetInt32() == 2);
    }

    [Fact]
    public void BannerFeed_ReturnsUploadAssetsReferencedByDataJson()
    {
        StorefrontBannerFeed.TryGetResponse("/banners/data.json", out StorefrontBannerResponse response);

        using JsonDocument document = JsonDocument.Parse(response.Content);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            string path = property.Value.GetProperty("path").GetString();
            Assert.True(StorefrontBannerFeed.TryGetResponse($"/banners/uploads/{path}", out StorefrontBannerResponse asset));
            Assert.Equal("image/png", asset.ContentType);
            Assert.Equal(0x89, asset.Content[0]);
            Assert.Equal(0x50, asset.Content[1]);
            Assert.Equal(0x4E, asset.Content[2]);
            Assert.Equal(0x47, asset.Content[3]);
        }
    }
}
