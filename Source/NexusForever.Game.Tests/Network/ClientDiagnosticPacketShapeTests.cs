using NexusForever.Game.Static.Pregame;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using MatchType = NexusForever.Game.Static.Matching.MatchType;
using NetworkMessage = NexusForever.Network.Message.Model.Shared.Message;

namespace NexusForever.Game.Tests.Network;

public class ClientDiagnosticPacketShapeTests
{
    [Fact]
    public void Client0x003D_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x2345u, 14u);
            writer.Write(0x11223344u);
            writer.WriteStringWide("realm-row-label");
            writer.Write(0xAABBCCDDEEFF0011ul);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x003D();

        packet.Read(reader);

        Assert.Equal((ushort)0x2345, packet.LeadingValue);
        Assert.Equal(0x11223344u, packet.TrailingValue);
        Assert.Equal("realm-row-label", packet.Text);
        Assert.Equal(0xAABBCCDDEEFF0011ul, packet.FinalValue);
    }

    [Fact]
    public void Client0x00C8_ReadsMappedMatchTypePayload()
    {
        byte[] packetData = WritePacket(writer => writer.Write((uint)MatchType.Dungeon, 5u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x00C8();

        packet.Read(reader);

        Assert.Equal(MatchType.Dungeon, packet.MatchType);
    }

    [Fact]
    public void Client0x00ED_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x0102030405060708ul);
            writer.Write(0x11223344u);
            writer.Write(0x5566778899AABBCCul);
            writer.Write(true);
            writer.Write(false);
            writer.Write(true);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x00ED();

        packet.Read(reader);

        Assert.Equal(0x0102030405060708ul, packet.Value0);
        Assert.Equal(0x11223344u, packet.Value1);
        Assert.Equal(0x5566778899AABBCCul, packet.Value2);
        Assert.True(packet.Value3);
        Assert.False(packet.Value4);
        Assert.True(packet.Value5);
    }

    [Fact]
    public void Client0x011B_ReadsMappedEmptyPayload()
    {
        byte[] packetData = Array.Empty<byte>();

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x011B();

        packet.Read(reader);
    }

    [Fact]
    public void Client0x011D_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x10203040u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x011D();

        packet.Read(reader);

        Assert.Equal(0x10203040u, packet.Value);
    }

    [Fact]
    public void Client0x012D_ReadsMappedWideStringPayload()
    {
        byte[] packetData = WritePacket(writer => writer.WriteStringWide("quest-owner-blocked"));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x012D();

        packet.Read(reader);

        Assert.Equal("quest-owner-blocked", packet.Text);
    }

    [Fact]
    public void ClientRealmTransfer_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x1122334455667788ul);
            writer.Write(0x2345u, 14u);
            writer.Write(true);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRealmTransfer();

        packet.Read(reader);

        Assert.Equal(0x1122334455667788ul, packet.CharacterId);
        Assert.Equal((ushort)0x2345, packet.TargetRealmId);
        Assert.True(packet.TransferFlag);
    }

    [Fact]
    public void Client0x0550_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x55667788u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0550();

        packet.Read(reader);

        Assert.Equal(0x55667788u, packet.Value);
    }

    [Fact]
    public void Client0x062A_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x22334455u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x062A();

        packet.Read(reader);

        Assert.Equal(0x22334455u, packet.Value);
    }

    [Fact]
    public void Client0x0634_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x33445566u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0634();

        packet.Read(reader);

        Assert.Equal(0x33445566u, packet.Value);
    }

    [Fact]
    public void ClientMovementControlAck_ReadsMappedUInt32Ticket()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x44556677u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientMovementControlAck();

        packet.Read(reader);

        Assert.Equal(0x44556677u, packet.Ticket);
    }

    [Fact]
    public void ClientPlayerMovementSpeedUpdate_ReadsMappedUInt32Scalar()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x55667788u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientPlayerMovementSpeedUpdate();

        packet.Read(reader);

        Assert.Equal(0x55667788u, packet.Value);
    }

    [Fact]
    public void Client0x063E_ReadsMappedWideStringPayload()
    {
        byte[] packetData = WritePacket(writer => writer.WriteStringWide("diagnostic text"));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x063E();

        packet.Read(reader);

        Assert.Equal("diagnostic text", packet.Text);
    }

    [Fact]
    public void Client0x0701_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x2u, 2u);
            writer.Write(0x44332211u, 32u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0701();

        packet.Read(reader);

        Assert.Equal(0x2u, packet.LeadingBits);
        Assert.Equal(0x44332211u, packet.TrailingValue);
    }

    [Fact]
    public void Client0x0760_ReadsServerRealmListRealmRowShape()
    {
        var realm = new RealmInfo
        {
            RealmId           = 1001u,
            RealmName         = "Test Realm",
            RealmNoteStringId = 42u,
            Flags             = default(RealmFlag),
            Type              = RealmType.PVE,
            Status            = RealmStatus.Up,
            Population        = RealmPopulation.Medium,
            Unused1           = 7u,
            Unused2           = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
            AccountRealmInfo  = new RealmInfo.AccountRealmData
            {
                RealmId               = 14,
                CharacterCount        = 2u,
                LastPlayedCharacter   = "Hero",
                LastPlayedTime        = 0x0102030405060708ul
            },
            Unused3 = 1,
            Unused4 = 2,
            Unused5 = 3,
            Unused6 = 4
        };

        byte[] packetData = WritePacket(realm.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0760();
        packet.Read(reader);

        Assert.Equal(realm.RealmId, packet.Realm.RealmId);
        Assert.Equal(realm.RealmName, packet.Realm.RealmName);
        Assert.Equal(realm.RealmNoteStringId, packet.Realm.RealmNoteStringId);
        Assert.Equal(realm.Flags, packet.Realm.Flags);
        Assert.Equal(realm.Type, packet.Realm.Type);
        Assert.Equal(realm.Status, packet.Realm.Status);
        Assert.Equal(realm.Population, packet.Realm.Population);
        Assert.Equal(realm.Unused1, packet.Realm.Unused1);
        Assert.Equal(realm.Unused2, packet.Realm.Unused2);
        Assert.Equal(realm.AccountRealmInfo.RealmId, packet.Realm.AccountRealmInfo.RealmId);
        Assert.Equal(realm.AccountRealmInfo.CharacterCount, packet.Realm.AccountRealmInfo.CharacterCount);
        Assert.Equal(realm.AccountRealmInfo.LastPlayedCharacter, packet.Realm.AccountRealmInfo.LastPlayedCharacter);
        Assert.Equal(realm.AccountRealmInfo.LastPlayedTime, packet.Realm.AccountRealmInfo.LastPlayedTime);
        Assert.Equal(realm.Unused3, packet.Realm.Unused3);
        Assert.Equal(realm.Unused4, packet.Realm.Unused4);
        Assert.Equal(realm.Unused5, packet.Realm.Unused5);
        Assert.Equal(realm.Unused6, packet.Realm.Unused6);
    }

    [Fact]
    public void Client0x0762_ReadsServerRealmListMessageRowShape()
    {
        var messageRow = new NetworkMessage
        {
            Index    = 3u,
            Messages = ["line-one", "line-two"]
        };

        byte[] packetData = WritePacket(messageRow.Write);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0762();
        packet.Read(reader);

        Assert.Equal(messageRow.Index, packet.MessageRow.Index);
        Assert.Equal(messageRow.Messages, packet.MessageRow.Messages);
    }

    [Fact]
    public void ClientAddonModuleList_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0x01020304u);
            writer.Write(0x11121314u);
            writer.Write(0x21222324u);
            writer.Write(0x31323334u);
            writer.Write(2u);

            writer.Write(0xAu, 4u);
            writer.Write(true);
            writer.Write((byte)0x55);
            writer.WriteStringWide("alpha");

            writer.Write(0x5u, 4u);
            writer.Write(false);
            writer.Write((byte)0x66);
            writer.WriteStringWide("beta");
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientAddonModuleList();

        packet.Read(reader);

        Assert.Equal(0x01020304u, packet.HeaderValue0);
        Assert.Equal(0x11121314u, packet.HeaderValue1);
        Assert.Equal(0x21222324u, packet.HeaderValue2);
        Assert.Equal(0x31323334u, packet.HeaderValue3);
        Assert.Equal(2u, packet.ModuleCount);
        Assert.Equal(2, packet.Modules.Count);

        Assert.Equal(0xAu, packet.Modules[0].ModuleNibble);
        Assert.True(packet.Modules[0].ModuleFlag);
        Assert.Equal((byte)0x55, packet.Modules[0].ModuleByte);
        Assert.Equal("alpha", packet.Modules[0].ModuleName);

        Assert.Equal(0x5u, packet.Modules[1].ModuleNibble);
        Assert.False(packet.Modules[1].ModuleFlag);
        Assert.Equal((byte)0x66, packet.Modules[1].ModuleByte);
        Assert.Equal("beta", packet.Modules[1].ModuleName);
    }

    [Fact]
    public void Client0x07E3_ReadsMappedUInt32Payload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(0x44556677u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x07E3();

        packet.Read(reader);

        Assert.Equal(0x44556677u, packet.Value);
    }

    [Fact]
    public void Client0x0928_ReadsMappedStructuredPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(0xAABBCCDDu, 32u);
            writer.Write(0x1Au, 5u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new Client0x0928();

        packet.Read(reader);

        Assert.Equal(0xAABBCCDDu, packet.LeadingValue);
        Assert.Equal(0x1Au, packet.TrailingBits);
    }

    private static void ReadPacket(IReadable packet, byte[] packetData)
    {
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        packet.Read(reader);
    }

    private static byte[] BuildPayload(int length)
    {
        return Enumerable.Range(0, length)
            .Select(i => (byte)(i + 1))
            .ToArray();
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
