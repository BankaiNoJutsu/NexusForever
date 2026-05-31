using System.Xml;
using System.Xml.Linq;
using NexusForever.Network.Sts.Model;

namespace NexusForever.Game.Tests.Sts;

public class StsResponseSerializationTests
{
    [Fact]
    public void AuthUserInfoResponse_WritesNativeParserIdentityFields()
    {
        var response = new AuthUserInfoResponse
        {
            UserId     = "player@example.test",
            UserCenter = 7u,
            UserName   = "player@example.test",
            LoginName  = "player@example.test",
            UserStatus = 0u,
            Created    = "2026-05-31T18:00:00.0000000Z"
        };

        XDocument document = WriteResponse(response);
        XElement reply = document.Root!;

        Assert.Equal("player@example.test", reply.Element("UserId")?.Value);
        Assert.Equal("7", reply.Element("UserCenter")?.Value);
        Assert.Equal("player@example.test", reply.Element("UserName")?.Value);
        Assert.Equal("player@example.test", reply.Element("LoginName")?.Value);
        Assert.Equal("0", reply.Element("UserStatus")?.Value);
        Assert.Equal("2026-05-31T18:00:00.0000000Z", reply.Element("Created")?.Value);
    }

    [Fact]
    public void PresenceUserInfoResponse_WritesAccessMaskAndAlias()
    {
        var response = new PresenceUserInfoResponse
        {
            LocationId = "",
            UserId     = "player@example.test",
            UserCenter = 7u,
            UserName   = "player@example.test",
            LoginName  = "player@example.test",
            AccessMask = 1L,
            UserStatus = 0u,
            Status     = "",
            Created    = "2026-05-31T18:00:00.0000000Z"
        };
        response.Aliases.Add("player@example.test");

        XDocument document = WriteResponse(response);
        XElement reply = document.Root!;

        Assert.Equal("player@example.test", reply.Element("UserId")?.Value);
        Assert.Equal("7", reply.Element("UserCenter")?.Value);
        Assert.Equal("player@example.test", reply.Element("UserName")?.Value);
        Assert.Equal("player@example.test", reply.Element("LoginName")?.Value);
        Assert.Equal("1", reply.Element("AccessMask")?.Value);
        Assert.Equal("0", reply.Element("UserStatus")?.Value);
        Assert.Equal("player@example.test", reply.Element("Aliases")?.Element("Alias")?.Value);
    }

    [Fact]
    public void AuthPageVerifiedIpsResponse_WritesEmptyItemsPage()
    {
        var response = new AuthPageVerifiedIpsResponse();

        XDocument document = WriteResponse(response);
        XElement reply = document.Root!;

        Assert.Equal("0", reply.Element("TotalPage")?.Value);
        Assert.Equal("0", reply.Element("TotalCount")?.Value);

        XElement items = reply.Element("Items")!;
        Assert.NotNull(items);
        Assert.Equal("array", items.Attribute("type")?.Value);
        Assert.Empty(items.Elements("Item"));
    }

    private static XDocument WriteResponse(NexusForever.Network.Sts.IWritable response)
    {
        var document = new XDocument();

        using (XmlWriter writer = document.CreateWriter())
        {
            response.Write(writer);
        }

        return document;
    }
}
