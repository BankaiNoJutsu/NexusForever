using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Network;

public class ItemLocationPacketShapeTests
{
    [Fact]
    public void ItemLocation_ReadMapsRetailRealmBankWireLocation()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x0Au, 9u);
            writer.Write(4u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var itemLocation = new ItemLocation();
        itemLocation.Read(reader);

        Assert.Equal(InventoryLocation.RealmBank, itemLocation.Location);
        Assert.Equal(4u, itemLocation.BagIndex);
    }

    [Fact]
    public void ItemLocation_WriteUsesRetailRealmBankWireLocation()
    {
        var itemLocation = new ItemLocation
        {
            Location = InventoryLocation.RealmBank,
            BagIndex = 11u
        };

        byte[] packetData = WritePacket(itemLocation.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x0Au, reader.ReadUInt(9u));
        Assert.Equal(11u, reader.ReadUInt());
    }

    [Fact]
    public void ToDragDropData_UsesRetailRealmBankWireLocation()
    {
        ulong dragDrop = ItemLocation.ToDragDropData(InventoryLocation.RealmBank, 0x11);

        Assert.Equal(0x0A11ul, dragDrop);
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
