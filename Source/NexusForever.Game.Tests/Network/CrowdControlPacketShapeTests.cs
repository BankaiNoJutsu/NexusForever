using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.Game.Tests.Network;

public class CrowdControlPacketShapeTests
{
    [Fact]
    public void ClientCCStateStunUpdate_ReadsPressedAndHeldDirectionBytes()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(CCStateStunVictimGameplay.Left, 8u);
            writer.Write(CCStateStunVictimGameplay.Right, 8u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCCStateStunUpdate();

        packet.Read(reader);

        Assert.Equal(CCStateStunVictimGameplay.Left, packet.InputPressed);
        Assert.Equal(CCStateStunVictimGameplay.Right, packet.InputHeld);
    }

    [Fact]
    public void ClientCCStateKnockdownBreak_ReadsThreeBitDashDirection()
    {
        byte[] packetData = WritePacket(writer => writer.Write(DashDirection.Back, 3u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCCStateKnockdownBreak();

        packet.Read(reader);

        Assert.Equal(DashDirection.Back, packet.Direction);
    }

    [Fact]
    public void ServerCCStateStunDirection_WritesDirectionByte()
    {
        var packet = new ServerCCStateStunDirection
        {
            Direction = CCStateStunVictimGameplay.Forward
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(CCStateStunVictimGameplay.Forward, reader.ReadEnum<CCStateStunVictimGameplay>(8u));
    }

    [Fact]
    public void ServerEntityCCStateSet_WritesUnitStateAndEffectId()
    {
        var packet = new ServerEntityCCStateSet
        {
            UnitId = 0x11223344u,
            CCType = CCState.Knockdown,
            SpellEffectUniqueId = 0x55667788u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(CCState.Knockdown, reader.ReadEnum<CCState>(5u));
        Assert.Equal(0x55667788u, reader.ReadUInt());
    }

    [Fact]
    public void ServerEntityCCStateRemove_WritesUnitStateCastEffectAndRemovedFlag()
    {
        var packet = new ServerEntityCCStateRemove
        {
            UnitId = 0x01020304u,
            CCType = CCState.Stun,
            SpellCastUniqueId = 0x11111111u,
            SpellEffectUniqueId = 0x22222222u,
            Removed = true
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(CCState.Stun, reader.ReadEnum<CCState>(5u));
        Assert.Equal(0x11111111u, reader.ReadUInt());
        Assert.Equal(0x22222222u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ServerEntityCCTetherUnit_WritesUnitIdOnly()
    {
        var packet = new ServerEntityCCTetherUnit
        {
            UnitId = 0x99887766u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x99887766u, reader.ReadUInt());
    }

    [Fact]
    public void ServerSpellBuffRemove_WritesCastingAndCasterIds()
    {
        var packet = new ServerSpellBuffRemove
        {
            CastingId = 0x12345678u,
            CasterId = 0x90ABCDEFu
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x12345678u, reader.ReadUInt());
        Assert.Equal(0x90ABCDEFu, reader.ReadUInt());
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
