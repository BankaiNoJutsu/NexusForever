using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Network;

public class HousingPacketShapeTests
{
    [Fact]
    public void HousingResult_ExposesRetailNeighborSuccessValue()
    {
        Assert.Equal(2, (int)HousingResult.Neighbor_Success);
    }

    [Fact]
    public void ClientHousingVisitResidence_ReadsTargetResidenceIdentity()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x1234u, 14u);
            writer.Write(0x0102030405060708ul);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingVisitResidence();

        packet.Read(reader);

        Assert.Equal(0x1234u, packet.TargetResidence.RealmId);
        Assert.Equal(0x0102030405060708ul, packet.TargetResidence.ResidenceId);
    }

    [Fact]
    public void ClientHousingNeighborInvite_ReadsTargetResidenceAndName()
    {
        byte[] packetData = WritePacket(writer =>
        {
            new TargetResidence
            {
                RealmId     = 0x1234,
                ResidenceId = 0x0102030405060708ul
            }.Write(writer);
            writer.WriteStringWide("NeighborName");
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingNeighborInvite();

        packet.Read(reader);

        Assert.Equal(0x1234u, packet.TargetResidence.RealmId);
        Assert.Equal(0x0102030405060708ul, packet.TargetResidence.ResidenceId);
        Assert.Equal("NeighborName", packet.TargetName);
    }

    [Fact]
    public void ClientHousingNeighborSetPermission_ReadsPermission()
    {
        byte[] packetData = WritePacket(writer =>
        {
            new TargetResidence
            {
                RealmId     = 7,
                ResidenceId = 1234ul
            }.Write(writer);
            writer.WriteStringWide("Roommate");
            writer.Write(2u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingNeighborSetPermission();

        packet.Read(reader);

        Assert.Equal(7u, packet.TargetResidence.RealmId);
        Assert.Equal(1234ul, packet.TargetResidence.ResidenceId);
        Assert.Equal("Roommate", packet.TargetName);
        Assert.Equal(2u, packet.Permission);
    }

    [Fact]
    public void ServerHousingResult_WriteSerializesNeighborSuccess()
    {
        var message = new ServerHousingResult
        {
            RealmId     = 0x1234,
            ResidenceId = 0x0102030405060708ul,
            PlayerName  = "NeighborName",
            Result      = HousingResult.Neighbor_Success
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal("NeighborName", reader.ReadWideString());
        Assert.Equal((uint)HousingResult.Neighbor_Success, reader.ReadUInt(7u));
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
