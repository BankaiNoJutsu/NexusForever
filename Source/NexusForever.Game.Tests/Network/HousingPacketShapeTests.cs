using NexusForever.Network;
using NexusForever.Game.Static.Housing;
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
    public void ServerHousingCommunityDonateUpdate_WriteSerializesParallelUint32Arrays()
    {
        var message = new ServerHousingCommunityDonateUpdate();
        message.Entries.Add(new ServerHousingCommunityDonateUpdate.Entry { Value0 = 10u, Value1 = 20u });
        message.Entries.Add(new ServerHousingCommunityDonateUpdate.Entry { Value0 = 30u, Value1 = 40u });

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(2, reader.ReadInt());
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
        Assert.Equal(20u, reader.ReadUInt());
        Assert.Equal(40u, reader.ReadUInt());
    }

    [Fact]
    public void ClientHousingCommunityDonate_ReadsDecorRows()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(1u);
            WriteDecorInfo(writer, 0x1000u, 0x2000ul, 3u, 0x3000u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingCommunityDonate();

        packet.Read(reader);

        DecorInfo decor = Assert.Single(packet.Decor);
        Assert.Equal(0x1000u, decor.TargetResidence.RealmId);
        Assert.Equal(0x2000ul, decor.TargetResidence.ResidenceId);
        Assert.Equal(DecorType.InteriorWallpaper, decor.DecorType);
        Assert.Equal(3u, decor.HookIndex);
        Assert.Equal(0x3000u, decor.DecorInfoId);
    }

    [Fact]
    public void ClientHousingDecorUpdate_ReadsTrailingFlagPerDecorRow()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(DecorUpdateOperation.Move, 3u);
            writer.Write(2u);
            WriteDecorInfo(writer, 0x1000u, 0x2000ul, 3u, 0x3000u);
            WriteDecorInfo(writer, 0x1001u, 0x2001ul, 4u, 0x3001u);
            writer.Write(true);
            writer.Write(false);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingDecorUpdate();

        packet.Read(reader);

        Assert.Equal(DecorUpdateOperation.Move, packet.Operation);
        Assert.Equal(2, packet.DecorUpdates.Count);
        Assert.Equal(2, packet.TrailingFlags.Count);
        Assert.True(packet.TrailingFlags[0]);
        Assert.False(packet.TrailingFlags[1]);
    }

    [Fact]
    public void ServerHousingVendorList_WriteSerializesRowsAndListType()
    {
        var message = new ServerHousingVendorList
        {
            ListType = 2
        };
        message.PlugItems.Add(new ServerHousingVendorList.PlugItem
        {
            SourceId = 0x0102030405060708ul,
            PlugItemId = 0x11121314u,
            Cost = 0x21222324u,
            PlugItemFlags = 0x31323334u
        });

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt(2u));
    }

    [Fact]
    public void ClientHousingInteriorWallpaperUpdate_ReadsNativeSixSlotPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            for (uint i = 0u; i < ClientHousingInteriorWallpaperUpdate.SlotCount; i++)
                writer.Write(i % 2u);

            for (uint i = 0u; i < ClientHousingInteriorWallpaperUpdate.SlotCount; i++)
                WriteDecorInfo(writer, 0x1000u + i, 0x2000ul + i, i + 1u, 0x3000u + i);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientHousingInteriorWallpaperUpdate();

        packet.Read(reader);

        Assert.Equal(ClientHousingInteriorWallpaperUpdate.SlotCount, packet.ExistingDecorFlags.Count);
        Assert.Equal(1u, packet.ExistingDecorFlags[1]);
        Assert.Equal(ClientHousingInteriorWallpaperUpdate.SlotCount, packet.DecorUpdates.Count);
        Assert.Equal(0x1002u, packet.DecorUpdates[2].TargetResidence.RealmId);
        Assert.Equal(0x2002ul, packet.DecorUpdates[2].TargetResidence.ResidenceId);
        Assert.Equal(DecorType.InteriorWallpaper, packet.DecorUpdates[2].DecorType);
        Assert.Equal(3u, packet.DecorUpdates[2].HookIndex);
        Assert.Equal(0x3002u, packet.DecorUpdates[2].DecorInfoId);
    }

    [Fact]
    public void ServerHousingResidenceDecor_WriteSerializesNativeDecorState()
    {
        var message = new ServerHousingResidenceDecor
        {
            Operation = 0x11223344u
        };
        message.DecorData.Add(new ServerHousingResidenceDecor.Decor
        {
            RealmId = 0x1234,
            ResidenceId = 0x0102030405060708ul,
            DecorId = 0x1112131415161718ul,
            DecorType = DecorType.InteriorWallpaper,
            DecorData = 0x21222324u,
            HookBagIndex = 0x31323334u,
            HookIndex = 0x41424344u,
            PlotIndex = 0x51525354u,
            Scale = 1.25f,
            Position = new(2f, 3f, 4f),
            Rotation = new(5f, 6f, 7f, 8f),
            DecorInfoId = 0x61626364u,
            ActivePropUnitId = 0x71727374u,
            ParentDecorId = 0x8182838485868788ul,
            ColourShift = 0x1234
        });

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal((uint)DecorType.InteriorWallpaper, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
        Assert.Equal(0x51525354u, reader.ReadUInt());
        Assert.Equal(1.25f, reader.ReadSingle());
        Assert.Equal(2f, reader.ReadSingle());
        Assert.Equal(3f, reader.ReadSingle());
        Assert.Equal(4f, reader.ReadSingle());
        Assert.Equal(5f, reader.ReadSingle());
        Assert.Equal(6f, reader.ReadSingle());
        Assert.Equal(7f, reader.ReadSingle());
        Assert.Equal(8f, reader.ReadSingle());
        Assert.Equal(0x61626364u, reader.ReadUInt());
        Assert.Equal(0x71727374u, reader.ReadUInt());
        Assert.Equal(0x8182838485868788ul, reader.ReadULong());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
    }

    [Fact]
    public void ServerHousingNeighbors_WriteSerializesNeighborRows()
    {
        var message = new ServerHousingNeighbors();
        message.Neighbors.Add(new ServerHousingNeighbors.Neighbor
        {
            CharacterId = 0x1112131415161718ul,
            Reserved = 0x2122232425262728ul,
            Permission = 2u
        });
        message.Neighbors[0].TargetResidence.RealmId     = 0x1234;
        message.Neighbors[0].TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(1, reader.ReadInt());
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(2u, reader.ReadUInt());
    }

    [Fact]
    public void ServerHousingNeighborInvitePrompt_WriteSerializesTargetAndName()
    {
        var message = new ServerHousingNeighborInvitePrompt
        {
            PlayerName = "Inviter"
        };
        message.TargetResidence.RealmId     = 0x1234;
        message.TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal("Inviter", reader.ReadWideString());
    }

    [Fact]
    public void ServerHousingNeighborInviteResult_WriteSerializesMappedRowAndTrailingValue()
    {
        var message = new ServerHousingNeighborInviteResult
        {
            Result = (HousingResult)0x45u
        };
        PopulateNeighborRow(message.Neighbor);

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        AssertNeighborRow(reader);
        Assert.Equal(0x45u, reader.ReadUInt(7u));
    }

    [Fact]
    public void ServerHousingNeighborUpdate_WriteSerializesMappedRowAndTrailingValue()
    {
        var message = new ServerHousingNeighborUpdate
        {
            UpdateType = (HousingNeighborUpdateType)0x5u
        };
        PopulateNeighborRow(message.Neighbor);

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        AssertNeighborRow(reader);
        Assert.Equal(0x5u, reader.ReadUInt(3u));
    }

    [Fact]
    public void ServerHousingCommunityPlotReservation_WriteSerializesMappedFields()
    {
        var message = new ServerHousingCommunityPlotReservation
        {
            PlotIndex = uint.MaxValue
        };
        message.TargetResidence.RealmId     = 0x1234;
        message.TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(uint.MaxValue, reader.ReadUInt());
    }

    [Fact]
    public void ServerHousingNeighborhoodEntry_WriteSerializesNativeRowShape()
    {
        var message = CreateNeighborhoodEntry();

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        AssertNeighborhoodEntry(reader);
    }

    [Fact]
    public void ServerHousingNeighborhoodList_WriteSerializesRealmAndRows()
    {
        var message = new ServerHousingNeighborhoodList
        {
            RealmId = 0x1234
        };
        message.Neighborhoods.Add(CreateNeighborhoodEntry());

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(1u, reader.ReadUInt());
        AssertNeighborhoodEntry(reader);
    }

    [Fact]
    public void ServerHousingCommunityPlacement_WriteSerializesNativeFields()
    {
        var message = new ServerHousingCommunityPlacement
        {
            PlacedResidenceId = 0x1112131415161718ul,
            PropertyIndex = 0x21222324u
        };
        message.TargetResidence.RealmId     = 0x1234;
        message.TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(0x21222324u, reader.ReadUInt());
    }

    [Fact]
    public void ServerHousingCommunityPrivacyLevelUpdate_WriteSerializesNativeFields()
    {
        var message = new ServerHousingCommunityPrivacyLevelUpdate
        {
            EntryType = 0x11121314u,
            Flags = 0x21222324u,
            PrivacyLevel = 5u
        };
        message.TargetResidence.RealmId     = 0x1234;
        message.TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt(3u));
    }

    [Fact]
    public void ServerHousingCommunityRenameResult_WriteSerializesResultAndReservedFields()
    {
        var message = new ServerHousingCommunityRenameResult
        {
            Result = HousingResult.InvalidResidenceName,
            Reserved0 = 0x01020304u,
            Reserved1 = 0x11121314u,
            Reserved2 = 0x21222324u
        };

        byte[] packetData = WritePacket(message.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal((uint)HousingResult.InvalidResidenceName, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11121314u, reader.ReadUInt());
        Assert.Equal(0x21222324u, reader.ReadUInt());
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

    private static void WriteDecorInfo(GamePacketWriter writer, uint realmId, ulong residenceId, uint hookIndex, uint decorInfoId)
    {
        writer.Write(realmId, 14u);
        writer.Write(residenceId);
        writer.Write(0x0101010101010101ul + hookIndex);
        writer.Write(DecorType.InteriorWallpaper, 32u);
        writer.Write(0x22222222u + hookIndex);
        writer.Write(0x33333333u + hookIndex);
        writer.Write(hookIndex);
        writer.Write(0u);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(1f);
        writer.Write(decorInfoId);
        writer.Write(0u);
        writer.Write(0ul);
        writer.Write(0u, 14u);
    }

    private static void PopulateNeighborRow(ServerHousingNeighbors.Neighbor neighbor)
    {
        neighbor.CharacterId = 0x1112131415161718ul;
        neighbor.Reserved = 0x2122232425262728ul;
        neighbor.TargetResidence.RealmId     = 0x1234;
        neighbor.TargetResidence.ResidenceId = 0x0102030405060708ul;
        neighbor.Permission = 2u;
    }

    private static void AssertNeighborRow(GamePacketReader reader)
    {
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal(0x2122232425262728ul, reader.ReadULong());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(2u, reader.ReadUInt());
    }

    private static ServerHousingNeighborhoodEntry CreateNeighborhoodEntry()
    {
        return new ServerHousingNeighborhoodEntry
        {
            NeighborhoodId = 0x0102030405060708ul,
            RealmId0       = 0x1234,
            RealmId1       = 0x2345,
            Value0         = 0x1112131415161718ul,
            Name           = "Neighborhood",
            Value1         = 0x21222324u,
            Value2         = 0x31323334u,
            Value3         = 0x41424344u
        };
    }

    private static void AssertNeighborhoodEntry(GamePacketReader reader)
    {
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x2345u, reader.ReadUInt(14u));
        Assert.Equal(0x1112131415161718ul, reader.ReadULong());
        Assert.Equal("Neighborhood", reader.ReadWideString());
        Assert.Equal(0x21222324u, reader.ReadUInt());
        Assert.Equal(0x31323334u, reader.ReadUInt());
        Assert.Equal(0x41424344u, reader.ReadUInt());
    }
}
