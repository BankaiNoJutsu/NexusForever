using System.Reflection;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.CREDDExchange;

namespace NexusForever.Game.Tests.Network;

public sealed class CreddRedeemPacketTests
{
    [Fact]
    public void AccountInventoryRedeemHasItsOwnOpcodeAndConsumesNoAmount()
    {
        Assert.Equal(0x0770u, (uint)typeof(ClientAccountCREDDRedeem).GetCustomAttribute<MessageAttribute>()!.Opcode);
        Assert.Equal(0x0268u, (uint)typeof(ClientCREDDRedeem).GetCustomAttribute<MessageAttribute>()!.Opcode);

        using (var empty = new GamePacketReader(new MemoryStream()))
            new ClientAccountCREDDRedeem().Read(empty);

        const ulong amount = 0x0123456789ABCDEFul;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(amount);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        new ClientAccountCREDDRedeem().Read(reader);
        var legacy = new ClientCREDDRedeem();
        legacy.Read(reader);
        Assert.Equal(amount, legacy.CreddAmount);
    }
}
