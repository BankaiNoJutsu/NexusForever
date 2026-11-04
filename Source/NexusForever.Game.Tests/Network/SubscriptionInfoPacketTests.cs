using System.Reflection;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.Game.Tests.Network;

public sealed class SubscriptionInfoPacketTests
{
    [Fact]
    public void SubscriptionInfoWritesFlagsThenOneExpiryBitThenFloatHours()
    {
        Assert.Equal(0x033Du, (uint)typeof(ServerSubscriptionInfo).GetCustomAttribute<MessageAttribute>()!.Opcode);

        var packet = new ServerSubscriptionInfo
        {
            Flags = ServerSubscriptionInfo.RealmTransferFlags.FreeRealmTransfer,
            SubscriptionExpired = true,
            GameTimeHoursRemaining = 12.5f
        };
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();

        // 0x41480000 is the single-precision representation of 12.5 hours.
        byte[] expected = [0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x90, 0x82, 0x00];
        Assert.Equal(expected, stream.ToArray());
    }
}
