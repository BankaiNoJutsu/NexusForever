using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Network;

public class TransportPacketShapeTests
{
    [Fact]
    public void ClientRapidTransport_ReadsTaxiNodeAndContextToken()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write((ushort)0x1234, 14u);
            writer.Write(0x89ABCDEFu);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRapidTransport();

        packet.Read(reader);

        Assert.Equal((ushort)0x1234, packet.TaxiNode);
        Assert.Equal(0x89ABCDEFu, packet.ContextToken);
    }

    [Fact]
    public void ClientFlightPathPurchase_ReadsCountedRouteIds()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(2u);
            writer.Write(100u);
            writer.Write(200u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientFlightPathPurchase();

        packet.Read(reader);

        Assert.Equal([100u, 200u], packet.RouteIds);
    }

    [Fact]
    public void ClientFlightPathPurchase_RejectsRouteCountBeyondRemainingBytes()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(2u);
            writer.Write(100u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientFlightPathPurchase();

        Assert.Throws<InvalidPacketValueException>(() => packet.Read(reader));
    }

    [Fact]
    public void ServerFlightPathUpdate_WritesCountedFlightPathIds()
    {
        var packet = new ServerFlightPathUpdate();
        packet.FlightPathIds.Add(11u);
        packet.FlightPathIds.Add(22u);

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
    }

    [Fact]
    public void ClientVehicleEmbark_ReadsVehicleAndTwoContextFields()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x11111111u);
            writer.Write(3u);
            writer.Write(0u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientVehicleEmbark();

        packet.Read(reader);

        Assert.Equal(0x11111111u, packet.VehicleUnitId);
        Assert.Equal(3u, packet.Unknown1);
        Assert.Equal(0u, packet.Unknown2);
    }

    [Fact]
    public void ServerVehiclePassengerSelf_WritesSeatBits()
    {
        var packet = new ServerVehiclePassengerSelf
        {
            Self = 0x11111111u,
            Vehicle = 0x22222222u,
            SeatType = VehicleSeatType.Gunner,
            SeatPosition = 5
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11111111u, reader.ReadUInt());
        Assert.Equal(0x22222222u, reader.ReadUInt());
        Assert.Equal(VehicleSeatType.Gunner, reader.ReadEnum<VehicleSeatType>(2u));
        Assert.Equal((byte)5, reader.ReadByte(3u));
    }

    [Fact]
    public void ServerVehiclePassengerAdd_WritesVehicleSeatAndPassengerUnit()
    {
        var packet = new ServerVehiclePassengerAdd
        {
            Self = 0x33333333u,
            SeatType = VehicleSeatType.Passenger,
            SeatPosition = 4,
            UnitId = 0x44444444u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x33333333u, reader.ReadUInt());
        Assert.Equal(VehicleSeatType.Passenger, reader.ReadEnum<VehicleSeatType>(2u));
        Assert.Equal((byte)4, reader.ReadByte(3u));
        Assert.Equal(0x44444444u, reader.ReadUInt());
    }

    [Fact]
    public void ServerVehiclePassengerRemove_WritesVehicleAndPassengerUnits()
    {
        var packet = new ServerVehiclePassengerRemove
        {
            Self      = 0x55555555u,
            Passenger = 0x66666666u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x55555555u, reader.ReadUInt());
        Assert.Equal(0x66666666u, reader.ReadUInt());
    }

    [Fact]
    public void VehiclePassenger_WritesSeatAndPassengerUnit()
    {
        var passenger = new VehiclePassenger
        {
            SeatType = VehicleSeatType.Pilot,
            SeatPosition = 1,
            UnitId = 0x77777777u
        };

        byte[] packetData = WritePacket(passenger);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(VehicleSeatType.Pilot, reader.ReadEnum<VehicleSeatType>(2u));
        Assert.Equal((byte)1, reader.ReadByte(3u));
        Assert.Equal(0x77777777u, reader.ReadUInt());
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
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
