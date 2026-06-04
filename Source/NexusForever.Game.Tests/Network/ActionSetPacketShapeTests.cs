using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Network;

public class ActionSetPacketShapeTests
{
    [Fact]
    public void ClientRequestActionSetChanges_ReadsMappedActionSetIndexAndTierRows()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(2u, 4u);
            writer.Write(100u);
            writer.Write(200u);
            writer.Write(1u, 3u);
            writer.Write(1u, 5u);
            writer.Write(7116u, 18u);
            writer.Write((byte)3);
            writer.Write(2u, 7u);
            writer.Write((ushort)10);
            writer.Write((ushort)20);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRequestActionSetChanges();
        packet.Read(reader);

        Assert.Equal([100u, 200u], packet.Actions);
        Assert.Equal(1, packet.ActionSetIndex);
        Assert.Collection(
            packet.ActionTiers,
            tier =>
            {
                Assert.Equal(7116u, tier.Action);
                Assert.Equal((byte)3, tier.Tier);
            });
        Assert.Equal([(ushort)10, (ushort)20], packet.Amps);
    }

    [Fact]
    public void ClientNonSpellActionSetChanges_ReadsMappedShortcutPayload()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(UILocation.PathAbility, 6u);
            writer.Write(ShortcutType.GameCommand, 4u);
            writer.Write(0x01020304u);
            writer.Write((byte)2, 4u);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientNonSpellActionSetChanges();
        packet.Read(reader);

        Assert.Equal(UILocation.PathAbility, packet.ActionBarIndex);
        Assert.Equal(ShortcutType.GameCommand, packet.ShortcutType);
        Assert.Equal(0x01020304u, packet.ObjectId);
        Assert.Equal((byte)2, packet.SpecIndex);
    }

    [Fact]
    public void ClientCommitAmpSpec_ReadsCountedAmpIds()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(2u, 7u);
            writer.Write((ushort)100);
            writer.Write((ushort)200);
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCommitAmpSpec();
        packet.Read(reader);

        Assert.Equal([(ushort)100, (ushort)200], packet.Amps);
    }

    [Fact]
    public void ServerActionSet_WritesSpecResultAndShortcutRows()
    {
        var packet = new ServerActionSet
        {
            SpecIndex = 2,
            Unlocked  = 1,
            Result    = LimitedActionSetResult.Ok,
            Actions =
            [
                new ServerActionSet.Action
                {
                    ShortcutType = ShortcutType.SpellbookItem,
                    Location = new NexusForever.Network.World.Message.Model.Shared.ItemLocation
                    {
                        Location = InventoryLocation.Ability,
                        BagIndex = 6u
                    },
                    ObjectId = 0x12345678u
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal((byte)2, reader.ReadByte(3u));
        Assert.Equal((byte)1, reader.ReadByte(2u));
        Assert.Equal(LimitedActionSetResult.Ok, reader.ReadEnum<LimitedActionSetResult>(6u));
        Assert.Equal(1u, reader.ReadUInt(6u));
        Assert.Equal(ShortcutType.SpellbookItem, reader.ReadEnum<ShortcutType>(4u));
        Assert.Equal(InventoryLocation.Ability, reader.ReadEnum<InventoryLocation>(9u));
        Assert.Equal(6u, reader.ReadUInt());
        Assert.Equal(0x12345678u, reader.ReadUInt());
    }

    [Fact]
    public void ServerActionBarSet_WritesShortcutSetShortcutSetIdAndAssociatedUnit()
    {
        byte[] packetData = WritePacket(new ServerActionBarSet
        {
            ShortcutSet            = ShortcutSet.FloatingDynamicSpellBar,
            ActionBarShortcutSetId = 0x1234,
            AssociatedUnitId       = 0x89ABCDEFu
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(ShortcutSet.FloatingDynamicSpellBar, reader.ReadEnum<ShortcutSet>(4u));
        Assert.Equal(0x1234u, reader.ReadUInt(14u));
        Assert.Equal(0x89ABCDEFu, reader.ReadUInt());
    }

    [Fact]
    public void ServerAmpRespecResult_WritesSpecIndicesThenResults()
    {
        var packet = new ServerAmpRespecResult
        {
            Results =
            [
                new ServerAmpRespecResult.AmpResult
                {
                    SpecIndex = 1,
                    Result    = LimitedActionSetResult.Ok
                },
                new ServerAmpRespecResult.AmpResult
                {
                    SpecIndex = 2,
                    Result    = LimitedActionSetResult.EldanAugmentationInvalidId
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(2u, reader.ReadUInt(7u));
        Assert.Equal((ushort)1, reader.ReadUShort(3u));
        Assert.Equal((ushort)2, reader.ReadUShort(3u));
        Assert.Equal(LimitedActionSetResult.Ok, reader.ReadEnum<LimitedActionSetResult>(6u));
        Assert.Equal(LimitedActionSetResult.EldanAugmentationInvalidId, reader.ReadEnum<LimitedActionSetResult>(6u));
    }

    [Fact]
    public void ServerSpecChangedAndClearCache_WriteMappedFields()
    {
        byte[] specChangedData = WritePacket(new ServerSpecChanged
        {
            ActionSetIndex = 3,
            SpecError      = SpecError.InCombat
        });
        byte[] clearCacheData = WritePacket(new ServerActionSetClearCache
        {
            GenerateChatLogMessage = false
        });

        using (var reader = new GamePacketReader(new MemoryStream(specChangedData)))
        {
            Assert.Equal((byte)3, reader.ReadByte(3u));
            Assert.Equal(SpecError.InCombat, reader.ReadEnum<SpecError>(32u));
        }

        using (var reader = new GamePacketReader(new MemoryStream(clearCacheData)))
        {
            Assert.False(reader.ReadBit());
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
