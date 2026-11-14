using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

// Native 0x0755 and 0x089A register reader 14007c340: UInt32 + one bit.
public sealed class UnitStateFlagPacketTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InUseAndCombatPacketsKeepTheMappedUnitAndSingleBit(bool combatPacket, bool flag)
    {
        IWritable packet = combatPacket
            ? new ServerUnitEnteredCombat { UnitId = 0x78563412u, InCombat = flag }
            : new ServerUnitInUse { UnitId = 0x78563412u, InUse = flag };
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        byte[] data = stream.ToArray();

        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78, flag ? (byte)1 : (byte)0 }, data);
        using var reader = new GamePacketReader(new MemoryStream(data));
        Assert.Equal(0x78563412u, reader.ReadUInt());
        Assert.Equal(flag, reader.ReadBit());
    }
}
