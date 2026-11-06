using System.Net;
using System.Net.Sockets;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Network;

[Collection(LoadHarnessWorldBootstrapCollection.Name)]
public class LoadHarnessCharacterListTests
{
    [Fact]
    public void CharacterListReadsAllVariableWidthRowsAndRetainsWireIdentity()
    {
        var list = List(17, 42);
        list.Characters[0].Name = new string('界', 130);
        list.Characters[0].Appearance.Add(new ItemVisual { DisplayId = 123, DyeData = 0x12345678 });
        list.Characters[0].Gear.Add(new ItemVisual { DisplayId = 456, ColourSetId = 321 });
        list.Characters[0].Labels.Add(3); list.Characters[0].Values.Add(9);
        list.Characters[0].Bones.AddRange([1.25f, 2.5f]);
        list.Characters[1].GearMask = 0x7f;
        list.EnabledCharacterCreationIds.Add(1); list.DisabledCharacterCreationIds.AddRange([2u, 3u]);
        var read = CharacterListObservation.Read(Body(list));
        Assert.Equal((ushort)7, read.Realm);
        Assert.Equal(new ulong[] { 17, 42 }, read.Characters.Select(row => row.Id));
        Assert.Equal(new string('界', 130), read.Characters[0].Name);
        Assert.Equal(1u, read.Characters[0].AppearanceCount);
        Assert.Equal(1u, read.Characters[0].GearCount);
        Assert.Equal(0x7fu, read.Characters[1].GearMask);
        Assert.Equal((ushort)52, read.Characters[1].Zone);
        Assert.Equal(64, read.BodySha256.Length);
        Assert.Equal(Body(list), Convert.FromHexString(read.BodyHex));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(9)] [InlineData(20)]
    public void TruncatedListCannotQualifyEvenIfTheSelectedRowWasRead(int removed)
    {
        byte[] body = Body(List(42));
        if (removed == 0) body = body.Concat(new byte[] { 0 }).ToArray();
        else body = body[..^removed];
        Assert.ThrowsAny<Exception>(() => CharacterListObservation.Read(body));
    }

    [Fact]
    public void DuplicateCharacterRowsCannotQualify()
        => Assert.Throws<InvalidDataException>(() => CharacterListObservation.Read(Body(List(42, 42))));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingOwnedWireRowFailsBeforeSelection(bool empty)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, 123);
        var connecting = client.ConnectAsync();
        using var peer = await listener.AcceptTcpClientAsync(); await connecting;
        Task<CharacterListObservation> waiting = client.WaitForCharacterList(42, TimeSpan.FromSeconds(2));
        await peer.GetStream().WriteAsync(PacketWriters.BuildFrame(GameMessageOpcode.ServerCharacterList,
            writer => (empty ? List() : List(17)).Write(writer)));
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => waiting);
        Assert.Contains("absent", error.Message);
        Assert.Equal(0, peer.Available);
    }

    private static ServerCharacterList List(params ulong[] ids)
    {
        var list = new ServerCharacterList { RealmId = 7 };
        foreach (ulong id in ids) list.Characters.Add(new()
        {
            Id = id, Name = "Listed" + id, Level = 50, WorldId = 51, WorldZoneId = 52, RealmId = 7
        });
        return list;
    }

    private static byte[] Body(ServerCharacterList list)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { list.Write(writer); writer.FlushBits(); }
        return stream.ToArray();
    }
}
