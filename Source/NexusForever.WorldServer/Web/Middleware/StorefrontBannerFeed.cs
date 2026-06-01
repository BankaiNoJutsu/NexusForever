using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NexusForever.WorldServer.Web.Middleware
{
    public class StorefrontBannerMiddleware
    {
        private readonly RequestDelegate next;

        public StorefrontBannerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (StorefrontBannerFeed.TryGetResponse(context.Request.Path, out StorefrontBannerResponse response))
            {
                context.Response.ContentType = response.ContentType;
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.Body.WriteAsync(response.Content, context.RequestAborted);
                return;
            }

            await next(context);
        }
    }

    internal static class StorefrontBannerFeed
    {
        private static readonly byte[] dataJson = Encoding.UTF8.GetBytes(
            """
            {
              "level_50_boost": {
                "title": { "en": "Level 50 Character Boost", "de": "Level 50 Character Boost", "fr": "Level 50 Character Boost" },
                "body": { "en": "Create a combat-ready character and jump straight into Nexus.", "de": "Create a combat-ready character and jump straight into Nexus.", "fr": "Create a combat-ready character and jump straight into Nexus." },
                "location": 0,
                "order": 0,
                "path": "level-50-character-boost.png",
                "type": 0,
                "sup_id": 2718
              },
              "special_offers": {
                "title": { "en": "Special Offers", "de": "Special Offers", "fr": "Special Offers" },
                "body": { "en": "Limited Protostar picks from the restored storefront catalog.", "de": "Limited Protostar picks from the restored storefront catalog.", "fr": "Limited Protostar picks from the restored storefront catalog." },
                "location": 0,
                "order": 1,
                "path": "special-offers.png",
                "type": 0,
                "sup_id": 2958
              },
              "featured_costumes": {
                "title": { "en": "Featured Costumes", "de": "Featured Costumes", "fr": "Featured Costumes" },
                "body": { "en": "", "de": "", "fr": "" },
                "location": 1,
                "order": 0,
                "path": "featured-costumes.png",
                "type": 0,
                "sup_id": 1678
              },
              "housing_picks": {
                "title": { "en": "Housing Picks", "de": "Housing Picks", "fr": "Housing Picks" },
                "body": { "en": "", "de": "", "fr": "" },
                "location": 2,
                "order": 0,
                "path": "housing-picks.png",
                "type": 0,
                "sup_id": 2091
              }
            }
            """);

        private static readonly byte[] bannerImage = Convert.FromBase64String(
            "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");

        private static readonly Dictionary<string, byte[]> uploadAssets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["/banners/uploads/level-50-character-boost.png"] = bannerImage,
            ["/banners/uploads/special-offers.png"] = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAUAAAAB9CAYAAADTA7ptAAACvklEQVR42u3dsQ3CMBBA0QxBgRArsBNTULMxI4QNACPHvvM9S6+EixL4lQ3b63nbASra3ARAAAEEEEAAAQQQQAABBBBggQBe7o8doCIBBAQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEENZj9V0CCAIogAIIAiiAAggCKIACCAIogAIIAiiAAggxA+hn5j8TQBBAARRAEEABFEAQQAEUQBBAARRAEEABFEAQQAEUQBBAARRAEEABFEAQQAGMGsDRx2XMyz3v32uZ9Z4CKIACYZ4ACqAACoR5AiiAAigQ5gkgAigQ5gkgAigQ5gkgAigQ5gng9xAcsY6OV8usX+/5kZ+n6QGMNG/067LczyxBmvX8BFAABVAABVAABVAABVAABVAABVAABVAABVAABVAABVAABVAABVAABVAABVAAi++TE8D4z08ABVAABVAAJwcwwgmN1uvp9awEUAAFUAAFUAAFUAAFUAAFUAAFUAAFUAAFUAAFUAAFUAAFUAAFcKV/hZv1gV51n6N9gAIogAIogAIogAIogAIogAIogAIogAIYPIC9VrTrEUABFEABFEABFEABFEABFEABFEABFED7AFcIYOV9jllCNjM2AiiAAiiAAiiAAiiAAiiAAiiAAmieAAqgAAqgeQIogAIogOYtG8CMJzicBBFAQRJAARRAqE0ABRAEUAAFEARQAAUQBFAABRAEUAAFEARQAAUQBFAABRAEMOVJkGgB7LEEEARQAAEBFEBAAAUQEEABBARQAIGxX9AqSwBBAAVQAEEABVAAQQCLBnAUAQQBFEBAAAUQoAgBBAQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEKAhgKfzdQeoSAABAQQQQAABBBBAAAEEEEAAAQQQQIB03r0pr/TaeNDVAAAAAElFTkSuQmCC"),
            ["/banners/uploads/featured-costumes.png"] = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAUAAAAB9CAYAAADTA7ptAAACvklEQVR42u3cwRHCIBBA0RThwYN1WIgVWIl1WYFtYQdqJgi77GPmH9WQZN7BAbb769kkqWKbmyAJgJIEQEkCoCQBUJIAKEkAlKQFALxdH02SKgZASQCUJABKEgAlCYCSBEBJAqAkAVCSAChJAJQkAEoSAKX1MvoOAEoABCAAJQACEIASAAEIQAmAAASgBEAAAlCKCaBj5j8HQAmAAASgBEAAAlACIAABKAEQgACUAAhAAEoABCAAJQACEIASAAEIQAmAAIwK4OjtMpF+L9Lnsjw/AAIQgAAEIAABCEAAAhCAAAQgAAEIQAACEIAABCAAAQhAAAIQgAAEIADzAjhyRLueX96NXu/TtzEFwEin947+zhUAtA4QgAAEIAABCEAAAhCAAAQgAAEIQAACEIAABCAAAQhAAAIQgAAEIAABCEAAAnDKOrIV8MiyDhCAAAQgAAEIwMPgZNnFseeaAQhAAAIQgAAEIAABCEAAAhCAAAQgAAEIQAACEIAABCAAAZgZQHiYAwABCEB4mAMAAQhAeAAQgAAEIDwACMBgO0GiAdjrf+ppR+LDwxwACEAAwsMcAAhAAMIDgAAEYBEArQMcN4d/3M9Izw+AAAQgAAEIQAACEIAABCAAAQhAAAIQgAAEIAABCEAAAhCASQCMhBIAAQhAAAIQgJIACEBJAASgJAACUBIAASgJgACUAAhAAEoABCAAJQDaCQJACYAABKAEQAACUAIgAAEoARCAAJQACEAASssAaIw7KQiAEgABCEAJgAAEoATAIgD2CIASAAEoCYAAlKQiAVASACUJgJIEQEkCoCQBUJIAKEkAlCQAShIAJQmAkgRASQKgJAFQkgAoSQCUJABKEgAlCYCSBEBJAqAkAVCSAChJAJSkHQCezpcmSRUDoCQAShIAJQmAkgRASQKgJAFQkgAoSQCUpHS9ATQ8hRZ7n1pXAAAAAElFTkSuQmCC"),
            ["/banners/uploads/housing-picks.png"] = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAUAAAAB9CAYAAADTA7ptAAACjElEQVR42u3dwRGCMBBAUYrw4IEmbMEarMHqrId2Ygc6QDazZN/OvCM4MPhPCSzb59UAKlrcBEAAAQQQQAABBBBAAAEEEGCCAD7ezwZQkQACAggggAACCCCAAAIIIIAAAggggAACCCCAAAII8zF9RwBBAAVQAEEABVAAQQAFUABBAAVQAEEABVAAIWcAvWb+NwEEARRAAQQBFEABBAEUQAEEARRAAQQBFEABBAEUQAEEARRAAQQBFEABBAEUwKwBPLrtJeK4iO05s18fAiiAAiiACKAACqAAIoACKIACiAAKoAAKoAAKoAAKoAAKoAAKoAAKoACeDcHec408z8gXovZ6fv9NiQBWDi4CKIACKIAIoAAKoAAigAIogAKIAAqgAAogAiiAAiiACKAACqAAIoACGBZA6/LG/Z5YCaAACqAAIoCJQzri/yWAAogACqAACiACKIACKIAIoAAKoAAigAIogAKIAAqgdYBzHIcACqAACiACKIACKIAIoAAKoACSPIC9RgAFUAARQAEUQAFEAAVQAAUQARRAX4Wz7hABFEABFEAEUAAFUAARQAEUQAFEAAVQAAUQARRAARRA7ARJEkAfRRJAARRAAcwYQBBAARRAEEABFEAQQAEUQBBAARRAEEABFEAQQAEUQBBAARRAEMALhCtqTaoAggAKoACCAAqgAIIACqAAggAKoACCAAqgAEKOABoBBAE0AggCaAQQBNCcjFkEAQQBFEBAAAUQoAgBBAQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEEAAAQQQQAABBBBAAAEEEGBHAG/3tQFUJICAAAIIIIAAAggggAACCCCAAAIIIMDlfAH2Zh5II+jkHAAAAABJRU5ErkJggg==")
        };

        public static bool TryGetResponse(PathString path, out StorefrontBannerResponse response)
        {
            if (path.Equals("/banners/data.json", StringComparison.OrdinalIgnoreCase))
            {
                response = new StorefrontBannerResponse("application/json; charset=utf-8", dataJson);
                return true;
            }

            if (uploadAssets.TryGetValue(path.Value ?? string.Empty, out byte[] asset))
            {
                response = new StorefrontBannerResponse("image/png", asset);
                return true;
            }

            response = default;
            return false;
        }
    }

    internal readonly struct StorefrontBannerResponse
    {
        public StorefrontBannerResponse(string contentType, byte[] content)
        {
            ContentType = contentType;
            Content = content;
        }

        public string ContentType { get; }
        public byte[] Content { get; }
    }
}
