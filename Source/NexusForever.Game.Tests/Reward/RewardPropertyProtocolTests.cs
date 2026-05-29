using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Reward;

public class RewardPropertyProtocolTests
{
    [Fact]
    public void ServerRewardPropertySet_WritesModifierSpecificValueEncodings()
    {
        var packet = new ServerRewardPropertySet
        {
            Properties =
            {
                new ServerRewardPropertySet.RewardProperty
                {
                    Id = RewardPropertyType.XP,
                    Data = 0x10203040u,
                    Type = RewardPropertyModifierValueType.AdditiveScalar,
                    Value = 1.5f,
                    SubRewardProperties =
                    {
                        new ServerRewardPropertySet.RewardProperty.SubRewardProperty
                        {
                            Id = 0xA,
                            Data = 0x01020304u,
                            Type = RewardPropertyModifierValueType.MultiplicativeScalar,
                            Value = 2.5f
                        }
                    }
                },
                new ServerRewardPropertySet.RewardProperty
                {
                    Id = RewardPropertyType.CommodityOrders,
                    Data = 0x50607080u,
                    Type = RewardPropertyModifierValueType.Discrete,
                    Value = 12f
                },
                new ServerRewardPropertySet.RewardProperty
                {
                    Id = RewardPropertyType.PurchaseDiscount,
                    Data = 0x90A0B0C0u,
                    Type = RewardPropertyModifierValueType.MultiplicativeScalar,
                    Value = 2.25f
                }
            }
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal((byte)3, reader.ReadByte());

        Assert.Equal(RewardPropertyType.XP, reader.ReadEnum<RewardPropertyType>(6u));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(RewardPropertyModifierValueType.AdditiveScalar, reader.ReadEnum<RewardPropertyModifierValueType>(2u));
        Assert.Equal(1.5f, reader.ReadSingle());
        Assert.Equal(1u, reader.ReadUInt(8u));
        Assert.Equal(0xAu, reader.ReadUInt(4u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(RewardPropertyModifierValueType.MultiplicativeScalar, reader.ReadEnum<RewardPropertyModifierValueType>(2u));
        Assert.Equal(2.5f, reader.ReadSingle());

        Assert.Equal(RewardPropertyType.CommodityOrders, reader.ReadEnum<RewardPropertyType>(6u));
        Assert.Equal(0x50607080u, reader.ReadUInt());
        Assert.Equal(RewardPropertyModifierValueType.Discrete, reader.ReadEnum<RewardPropertyModifierValueType>(2u));
        Assert.Equal(12u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt(8u));

        Assert.Equal(RewardPropertyType.PurchaseDiscount, reader.ReadEnum<RewardPropertyType>(6u));
        Assert.Equal(0x90A0B0C0u, reader.ReadUInt());
        Assert.Equal(RewardPropertyModifierValueType.MultiplicativeScalar, reader.ReadEnum<RewardPropertyModifierValueType>(2u));
        Assert.Equal(2.25f, reader.ReadSingle());
        Assert.Equal(0u, reader.ReadUInt(8u));
    }

    private static byte[] WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
