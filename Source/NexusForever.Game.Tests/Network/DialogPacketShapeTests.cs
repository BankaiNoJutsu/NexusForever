using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class DialogPacketShapeTests
{
    [Fact]
    public void ServerDialogEnd_WritesDialogUnitIdAsUInt32()
    {
        byte[] data = WritePacket(new ServerDialogEnd
        {
            DialogUnitId = 790u
        });

        Assert.Equal([0x16, 0x03, 0x00, 0x00], data);
    }

    private static byte[] WritePacket(ServerDialogEnd packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            packet.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}
