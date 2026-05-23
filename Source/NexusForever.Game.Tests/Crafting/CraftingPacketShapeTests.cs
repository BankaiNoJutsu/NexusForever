using System.Numerics;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingPacketShapeTests
{
    [Fact]
    public void ClientCraftingSimpleCraft_ReadsContextStationAndSchematic()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x10203040u);
            writer.Write(0x50607080u);
            writer.Write(0x90A0B0C0u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCraftingSimpleCraft();

        packet.Read(reader);

        Assert.Equal(0x10203040u, packet.ContextToken);
        Assert.Equal(0x50607080u, packet.CraftingStationUnitId);
        Assert.Equal(0x90A0B0C0u, packet.TradeskillSchematic2Id);
    }

    [Fact]
    public void ClientCraftingCraftItem_ReadsCatalystAsEighteenBitItemId()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(1u);
            writer.Write(2u);
            writer.Write(3u);
            writer.Write(4u);
            writer.Write(0x12345u, 18u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCraftingCraftItem();

        packet.Read(reader);

        Assert.Equal(1u, packet.ContextToken);
        Assert.Equal(2u, packet.CraftingStationUnitId);
        Assert.Equal(3u, packet.TradeskillSchematic2Id);
        Assert.Equal(4u, packet.SchematicCount);
        Assert.Equal(0x12345u, packet.CatalystItem2Id);
    }

    [Fact]
    public void ClientCraftingCraftItemAutoCraft_ReadsContextStationSchematicAndCount()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(11u);
            writer.Write(22u);
            writer.Write(33u);
            writer.Write(44u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCraftingCraftItemAutoCraft();

        packet.Read(reader);

        Assert.Equal(11u, packet.ContextToken);
        Assert.Equal(22u, packet.CraftingStationUnitId);
        Assert.Equal(33u, packet.TradeskillSchematic2Id);
        Assert.Equal(44u, packet.SchematicCount);
    }

    [Fact]
    public void ClientCraftingAdditive_ReadsAdditiveAndCatalystAsEighteenBitItemIds()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x11223344u);
            writer.Write(0x12345u, 18u);
            writer.Write(0x23456u, 18u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCraftingAdditive();

        packet.Read(reader);

        Assert.Equal(0x11223344u, packet.CraftingStationUnitId);
        Assert.Equal(0x12345u, packet.AdditiveItem2Id);
        Assert.Equal(0x23456u, packet.CatalystItem2Id);
    }

    [Fact]
    public void ClientCraftingComplexCraft_ReadsStatsPowerCoreAndCharges()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(100u);
            writer.Write(200u);
            writer.Write(300u);
            WriteCraftStatsPayload(writer);
            writer.Write(0x34567u, 18u);
            writer.Write(400u);
            writer.Write(2u, 3u);
            writer.Write(-1);
            writer.Write(5);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCraftingComplexCraft();

        packet.Read(reader);

        Assert.Equal(100u, packet.ContextToken);
        Assert.Equal(200u, packet.CraftingStationUnitId);
        Assert.Equal(300u, packet.TradeskillSchematic2Id);
        Assert.Equal(Property.AssaultRating, packet.CraftStats.StatType[0]);
        Assert.Equal(Property.SupportRating, packet.CraftStats.StatType[1]);
        Assert.Equal((byte)7, packet.CraftStats.Unknown);
        Assert.Equal((byte)8, packet.CraftStats.ApSpSplit);
        Assert.Equal(0u, packet.CraftStats.CircuitComplete);
        Assert.Equal(0x34567u, packet.PowerCoreItem2Id);
        Assert.Equal(400u, packet.ApSpSplitDelta);
        Assert.Equal([-1, 5], packet.ChargeCounts);
    }

    [Fact]
    public void ServerCraftingFinish_WritesDiscoveryDirectionXpAndReturnedMaterials()
    {
        var packet = new ServerCraftingFinish
        {
            Pass = true,
            TradeskillSchematic2IdCrafted = 0x1234u,
            Item2IdCrafted = 0x23456u,
            Unused = 0x10203040u,
            HotOrCold = CraftingDiscovery.Hot,
            Direction = CraftingDirection.SW,
            EarnedXp = 55u,
            MaterialReturnedIds = [9u, 10u]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.True(reader.ReadBit());
        Assert.Equal(0x1234u, reader.ReadUInt(15u));
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(CraftingDiscovery.Hot, reader.ReadEnum<CraftingDiscovery>(32u));
        Assert.Equal(CraftingDirection.SW, reader.ReadEnum<CraftingDirection>(4u));
        Assert.Equal(55u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(9u, reader.ReadUInt());
        Assert.Equal(10u, reader.ReadUInt());
    }

    [Fact]
    public void ServerCraftingCurrentCraft_WritesDiscoveryFields()
    {
        var packet = new ServerCraftingCurrentCraft
        {
            TradeskillSchematic2Id = 0x1234u,
            CraftStats = CreateCraftStats(),
            GlyphData = 0x01020304u,
            SchematicCount = 2u,
            AdditiveCount = 3u,
            Unused = 4u,
            Item2IdModifiers = [11u, 12u, 13u, 14u, 15u],
            Item2Id = 0x23456u,
            DiscoveryCoordinates = new Vector2(1.25f, 2.5f),
            DiscoveryVectorMultiplier = new Vector2(3.75f, 4.5f),
            DiscoveryRadiusMultiplier = 5.25f
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x1234u, reader.ReadUInt(15u));
        reader.ReadULong();
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal([11u, 12u, 13u, 14u, 15u], Enumerable.Range(0, 5).Select(_ => reader.ReadUInt()).ToArray());
        Assert.Equal(0x23456u, reader.ReadUInt(18u));
        Assert.Equal(1.25f, reader.ReadSingle());
        Assert.Equal(2.5f, reader.ReadSingle());
        Assert.Equal(3.75f, reader.ReadSingle());
        Assert.Equal(4.5f, reader.ReadSingle());
        Assert.Equal(5.25f, reader.ReadSingle());
    }

    [Fact]
    public void ServerCraftingAuxSixUInt32_WritesMappedFields()
    {
        var packet = new ServerCraftingAuxSixUInt32
        {
            Value0 = 0x01020304u,
            Value1 = 0x50607080u,
            Value2 = 0x90A0B0C0u,
            Value3 = 0xDEADBEEFu,
            Value4 = 0xCAFEBABEu,
            Value5 = 0x11223344u,
        };

        byte[] packetData = WritePacket(packet);
        Assert.Equal(0x18, packetData.Length);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(0xDEADBEEFu, reader.ReadUInt());
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
        Assert.Equal(0x11223344u, reader.ReadUInt());
    }

    [Fact]
    public void ServerCraftingAuxThreeUInt32_WritesMappedFields()
    {
        var packet = new ServerCraftingAuxThreeUInt32
        {
            Value0 = 0xAABBCCDDu,
            Value1 = 0x01020304u,
            Value2 = 0x50607080u,
        };

        byte[] packetData = WritePacket(packet);
        Assert.Equal(0xC, packetData.Length);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x50607080u, reader.ReadUInt());
    }

    [Fact]
    public void ServerTradeskillSigilResult_WritesResultAsUInt32()
    {
        var packet = new ServerTradeskillSigilResult
        {
            TradeskillSigilResult = TradeskillResult.DuplicateRune
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(TradeskillResult.DuplicateRune, reader.ReadEnum<TradeskillResult>(32u));
    }

    private static CraftStats CreateCraftStats()
    {
        return new CraftStats
        {
            StatType =
            [
                Property.AssaultRating,
                Property.SupportRating,
                Property.RatingCritSeverityIncrease,
                Property.RatingArmorPierce,
                Property.RatingAvoidIncrease
            ],
            Unknown = 7,
            ApSpSplit = 8,
            CircuitComplete = 9
        };
    }

    private static void WriteCraftStatsPayload(GamePacketWriter writer)
    {
        ulong packed = 0ul;
        packed |= (ulong)Property.AssaultRating;
        packed |= (ulong)Property.SupportRating << 8;
        packed |= (ulong)Property.RatingCritSeverityIncrease << 16;
        packed |= (ulong)Property.RatingArmorPierce << 24;
        packed |= (ulong)Property.RatingAvoidIncrease << 32;
        packed |= 7ul << 48;
        packed |= 8ul << 56;

        writer.Write(packed);
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
