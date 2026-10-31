using NexusForever.Game.Static.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.Game.Tests.Network;

public class UpstreamMiniPetBarPacketTests
{
    [Theory]
    [InlineData("0200000000000000A5", 2u, 0u)]
    [InlineData("EFCDAB8910325476A5", 0x89ABCDEFu, 0x76543210u)]
    [InlineData("FFFFFFFFFFFFFFFFA5", uint.MaxValue, uint.MaxValue)]
    public void NativePairIsTwoFullWidthFieldsFollowedBySentinel(string raw, uint set, uint slot)
    {
        Assert.Equal(0x068A, (int)GameMessageOpcode.ClientPetMiniCommand);
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString(raw)));
        var message = new ClientPetMiniCommand(); message.Read(reader);
        Assert.Equal((ShortcutSet)set, message.ShortcutSet); Assert.Equal(slot, message.SlotIndex);
        Assert.Equal((byte)0xA5, reader.ReadByte());
        Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(4)] [InlineData(7)]
    public void TruncatedNativePairIsRejected(int count)
    {
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString("0200000000000000")[..count]));
        Assert.Throws<EndOfStreamException>(() => new ClientPetMiniCommand().Read(reader));
    }
}
