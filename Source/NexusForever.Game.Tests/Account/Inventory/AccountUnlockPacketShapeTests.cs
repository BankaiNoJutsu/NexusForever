using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Account.Inventory;

public class AccountUnlockPacketShapeTests
{
    [Fact]
    public void ClientItemGenericUnlock_ReadsItemLocation()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(InventoryLocation.Inventory, 9u);
            writer.Write(42u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientItemGenericUnlock();

        packet.Read(reader);

        Assert.Equal(InventoryLocation.Inventory, packet.Location.Location);
        Assert.Equal(42u, packet.Location.BagIndex);
    }

    [Fact]
    public void ServerGenericUnlockPackets_WriteEntryIdsAndResults()
    {
        byte[] accountUnlockData = WritePacket(new ServerGenericUnlock
        {
            GenericUnlockEntryId = 0x1234
        });
        byte[] resultData = WritePacket(new ServerGenericUnlockResult
        {
            Result = GenericUnlockResult.PartialUnlock
        });

        using (var reader = new GamePacketReader(new MemoryStream(accountUnlockData)))
        {
            Assert.Equal(0x1234u, reader.ReadUShort(14u));
        }

        using (var reader = new GamePacketReader(new MemoryStream(resultData)))
        {
            Assert.Equal(GenericUnlockResult.PartialUnlock, reader.ReadEnum<GenericUnlockResult>(3u));
        }
    }

    [Theory]
    [InlineData(typeof(ServerGenericUnlockAccountList))]
    [InlineData(typeof(ServerGenericUnlockCharacterList))]
    [InlineData(typeof(ServerGenericUnlockCharacterRefresh))]
    public void ServerGenericUnlockLists_WriteCountedEntryIds(Type packetType)
    {
        IWritable packet = packetType == typeof(ServerGenericUnlockAccountList)
            ? new ServerGenericUnlockAccountList { GenericUnlockEntryIds = [11u, 22u] }
            : packetType == typeof(ServerGenericUnlockCharacterList)
                ? new ServerGenericUnlockCharacterList { GenericUnlockEntryIds = [11u, 22u] }
                : new ServerGenericUnlockCharacterRefresh { GenericUnlockEntryIds = [11u, 22u] };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
    }

    [Fact]
    public void CostumeUnlockRequests_ReadItemLocationOrItemId()
    {
        byte[] locationData = WritePacket(writer =>
        {
            writer.Write(InventoryLocation.Inventory, 9u);
            writer.Write(7u);
        });
        byte[] forgetData = WritePacket(writer => writer.Write(0x23456u, 18u));

        using (var reader = new GamePacketReader(new MemoryStream(locationData)))
        {
            var packet = new ClientCostumeItemUnlock();
            packet.Read(reader);
            Assert.Equal(InventoryLocation.Inventory, packet.Location.Location);
            Assert.Equal(7u, packet.Location.BagIndex);
        }

        using (var reader = new GamePacketReader(new MemoryStream(forgetData)))
        {
            var packet = new ClientCostumeItemForget();
            packet.Read(reader);
            Assert.Equal(0x23456u, packet.ItemId);
        }
    }

    [Fact]
    public void ServerCostumeUnlockPackets_WriteResultsAndItemIds()
    {
        byte[] singleData = WritePacket(new ServerCostumeItemUnlock
        {
            ItemId = 0x34567u,
            Result = CostumeUnlockResult.UnlockSuccess
        });
        byte[] multipleData = WritePacket(new ServerCostumeItemUnlockMultiple
        {
            Result = CostumeUnlockResult.AlreadyKnown,
            ItemsIds = [101u, 202u]
        });

        using (var reader = new GamePacketReader(new MemoryStream(singleData)))
        {
            Assert.Equal(0x34567u, reader.ReadUInt(18u));
            Assert.Equal(CostumeUnlockResult.UnlockSuccess, reader.ReadEnum<CostumeUnlockResult>(32u));
        }

        using (var reader = new GamePacketReader(new MemoryStream(multipleData)))
        {
            Assert.Equal(CostumeUnlockResult.AlreadyKnown, reader.ReadEnum<CostumeUnlockResult>(32u));
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(101u, reader.ReadUInt());
            Assert.Equal(202u, reader.ReadUInt());
        }
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
