using NexusForever.Game.Static.Costume;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Costume;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.Network.World.Message.Static;
using NetworkCostume = NexusForever.Network.World.Message.Model.Costume.Costume;

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
            Assert.Equal(0x23456u, packet.Item2Id);
        }
    }

    [Fact]
    public void ServerCostumeUnlockPackets_WriteResultsAndItemIds()
    {
        byte[] singleData = WritePacket(new ServerCostumeItemUnlock
        {
            Item2Id = 0x34567u,
            Result = CostumeUnlockResult.UnlockSuccess
        });
        byte[] multipleData = WritePacket(new ServerCostumeItemUnlockMultiple
        {
            Result = CostumeUnlockResult.AlreadyKnown,
            Item2Ids = [101u, 202u]
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

    [Fact]
    public void CostumeSaveAndNetworkCostume_UseTypeVisibilityMaskItem2IdsAndUnsignedDyeData()
    {
        byte[] saveData = WritePacket(writer =>
        {
            writer.Write(2);
            writer.Write(CostumeType.Personal, 2u);
            writer.Write(0x0102030405060708ul);
            for (int i = 0; i < NetworkCostume.MaxCostumeItems; i++)
            {
                writer.Write((uint)(0x1000 + i), 18u);
                writer.Write((uint)(0x2000 + i));
                writer.Write((uint)(0x3000 + i));
                writer.Write((uint)(0x4000 + i));
            }
            writer.Write(0x7Fu);
            writer.Write(true);
        });

        using (var reader = new GamePacketReader(new MemoryStream(saveData)))
        {
            var packet = new ClientCostumeSave();
            packet.Read(reader);

            Assert.Equal(2, packet.Index);
            Assert.Equal(CostumeType.Personal, packet.Type);
            Assert.Equal(0x0102030405060708ul, packet.MannequinDecorId);
            Assert.Equal(0x7Fu, packet.VisibilityMask);
            Assert.True(packet.UserServiceToken);
            Assert.Equal(0x1000u, packet.Items[0].Item2Id);
            Assert.Equal(0x2000u, packet.Items[0].DyeColorRampIds[0]);
        }

        byte[] costumeData = WritePacket(new NetworkCostume
        {
            Index          = 2u,
            Type           = CostumeType.Mannequin,
            VisibilityMask = 0x7Fu,
            Item2Ids       = [1u, 2u, 3u, 4u, 5u, 6u, 7u],
            DyeData        = [0xFFFFFFFFu, 2u, 3u, 4u, 5u, 6u, 7u]
        });

        using (var reader = new GamePacketReader(new MemoryStream(costumeData)))
        {
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(0x7Fu, reader.ReadUInt());
            Assert.Equal(CostumeType.Mannequin, reader.ReadEnum<CostumeType>(2u));
            Assert.Equal(1u, reader.ReadUInt());
            for (int i = 0; i < NetworkCostume.MaxCostumeItems - 1; i++)
                reader.ReadUInt();
            Assert.Equal(0xFFFFFFFFu, reader.ReadUInt());
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
