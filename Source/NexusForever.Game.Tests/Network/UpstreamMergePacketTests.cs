using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.AccountInventory;
using NexusForever.Network.World.Message;
using NexusForever.Network.World.Message.Model.AccountInventory;
using NexusForever.Network.World.Message.Model.Housing;
using NexusForever.Network.World.Message.Model.Spell;

namespace NexusForever.Game.Tests.Network;

public class UpstreamMergePacketTests
{
    [Theory]
    [InlineData(0x04E6, typeof(ClientHousingPlaceDecor))]
    [InlineData(0x077D, typeof(ServerMissileActorRemove))]
    [InlineData(0x0784, typeof(ServerSpellVisualEffectRemove))]
    [InlineData(0x0785, typeof(ServerSpellVisualEffectRemoveFromUnit))]
    [InlineData(0x079E, typeof(ClientPremiumLockboxKeyRequest))]
    [InlineData(0x080E, typeof(ServerSpellDisableCooldowns))]
    [InlineData(0x0832, typeof(ServerAnnounceCommandOnCooldown))]
    [InlineData(0x096F, typeof(ServerOmnibitsWeeklyBonus))]
    public void IncomingOpcodeHasExactlyOneModel(int opcode, Type expected)
    {
        Type registered = Assert.Single(expected.Assembly.GetTypes(),
            type => type.GetCustomAttribute<MessageAttribute>()?.Opcode == (GameMessageOpcode)opcode);
        Assert.Equal(expected, registered);
        var registry = new MessageManager(NullLogger<MessageManager>.Instance);
        registry.RegisterNetworkManagerWorldMessages();
        if (typeof(IReadable).IsAssignableFrom(expected))
            Assert.Equal(expected, registry.GetMessageType((GameMessageOpcode)opcode));
        else
            Assert.Equal((GameMessageOpcode)opcode, registry.GetOpcode((IWritable)Activator.CreateInstance(expected)));
    }

    [Fact]
    public void HousingPlaceDecorConsumesTheCompleteUpstreamPayload()
    {
        byte[] bytes = Write(writer =>
        {
            writer.Write((ushort)0x1234, 14u);
            writer.Write(0xFEDCBA9876543210UL);
            writer.Write(0x1122334455667788UL);
            writer.Write(0x99887766u);
            writer.Write(DecorOperation.DetachFromProp, 3u);
            writer.Write(1.25f); writer.Write(-2.5f); writer.Write(3.75f);
            writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
            writer.Write((byte)0x15, 5u);
        });
        using var reader = new GamePacketReader(new MemoryStream(bytes));
        var message = new ClientHousingPlaceDecor();
        message.Read(reader);
        Assert.Equal((ushort)0x1234, message.ResidenceIdentity.RealmId);
        Assert.Equal(0xFEDCBA9876543210UL, message.ResidenceIdentity.Id);
        Assert.Equal(0x1122334455667788UL, message.DecorId);
        Assert.Equal(0x99887766u, message.HousingDecorInfoId);
        Assert.Equal(DecorOperation.DetachFromProp, message.Operation);
        Assert.Equal(new Vector3(1.25f, -2.5f, 3.75f), message.Position);
        Assert.Equal(Quaternion.Identity, message.Rotation);
        Assert.Equal((byte)0x15, reader.ReadByte(5u));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisableCooldownsPreservesTheFollowingBits(bool disabled)
    {
        byte[] bytes = Write(writer =>
        {
            new ServerSpellDisableCooldowns { DisableCooldowns = disabled }.Write(writer);
            writer.Write((byte)0x55, 7u);
        });
        Assert.Single(bytes);
        using var reader = new GamePacketReader(new MemoryStream(bytes));
        Assert.Equal(disabled, reader.ReadBit());
        Assert.Equal((byte)0x55, reader.ReadByte(7u));
    }

    [Fact]
    public void VisualRemovalAndScalarPacketsRetainUpstreamWidths()
    {
        Assert.Equal(3, Write(new ServerSpellVisualEffectRemove { SpellVisualEffectUniqueId = 0x3FFFFu }.Write).Length);
        Assert.Equal(3, Write(new ServerSpellVisualEffectRemoveFromUnit { TargetUnitId = 0x3FFFFu }.Write).Length);
        Assert.Equal(new byte[] { 0xEF, 0xCD, 0xAB, 0x89 }, Write(new ServerMissileActorRemove { MissileActorId = 0x89ABCDEFu }.Write));
        Assert.Equal(new byte[] { 0xEF, 0xCD, 0xAB, 0x89 }, Write(new ServerAnnounceCommandOnCooldown { CooldownTime = 0x89ABCDEFu }.Write));
        Assert.Equal(new byte[] { 0xEF, 0xCD, 0xAB, 0x89 }, Write(new ServerOmnibitsWeeklyBonus { OmnibitsWeeklylBonusEarned = 0x89ABCDEFu }.Write));
        using var reader = new GamePacketReader(new MemoryStream(new byte[] { 0xA5 }));
        new ClientPremiumLockboxKeyRequest().Read(reader);
        Assert.Equal((byte)0xA5, reader.ReadByte());
    }

    private static byte[] Write(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
