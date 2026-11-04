using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public class PlayerPacketShapeTests
{
    [Fact]
    public void ServerPlayerCreateFactionReputation_WritesReputationAmount()
    {
        var packet = new ServerPlayerCreate.Faction.FactionReputation
        {
            FactionId = (Faction)0x1234,
            ReputationAmount = 12.5f
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal((Faction)0x1234, reader.ReadEnum<Faction>(14u));
        Assert.Equal(12.5f, reader.ReadSingle());
    }

    [Fact]
    public void PlayerCreateRejectsAnInlaidAugmentationListLargerThanTheClientBuffer()
    {
        var packet = new ServerPlayerCreate { FactionData = new() };
        packet.KnownDyes.AddRange(Enumerable.Range(300, 55).Select(id => (ushort)id));

        Assert.Throws<InvalidOperationException>(() => WritePacket(packet));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(54)]
    public void PlayerCreateWritesInlaidAugmentationIdsBeforeTradeskillMaterials(int count)
    {
        var packet = new ServerPlayerCreate { FactionData = new(), UnknownC8 = 0x98765432 };
        var ids = Enumerable.Range(300, count).Select(id => (ushort)id).ToArray();
        packet.UnlockedInlaidAugmentationIds.AddRange(ids);
        Assert.Same(packet.UnlockedInlaidAugmentationIds, packet.KnownDyes);
        packet.TradeskillMaterials[0] = 0xBEEF;

        using var reader = new GamePacketReader(new MemoryStream(WritePacket(packet)));
        Assert.Equal(0u, reader.ReadUInt()); // Empty inventory.
        for (int i = 0; i < 16; i++) reader.ReadULong();
        for (int i = 0; i < 5; i++) reader.ReadUInt(); // XP, rest XP, proficiency and both elder point fields.
        reader.ReadUInt(3u); // Active spec.
        reader.ReadUShort(); // Bonus power.
        reader.ReadUInt();
        reader.ReadUInt(14u); // Faction.
        Assert.Equal((ushort)0, reader.ReadUShort()); // No reputations.
        Assert.Equal(0u, reader.ReadUInt()); // No pets.
        reader.ReadUInt(); // Input key set.
        reader.ReadUShort(); // Bind point.
        reader.ReadUInt(); // Costume.
        reader.ReadUInt(); // UnknownC4.
        Assert.Equal(packet.UnknownC8, reader.ReadUInt());
        Assert.Equal((uint)count, reader.ReadUInt(6u));
        foreach (ushort id in ids)
            Assert.Equal(id, reader.ReadUShort());
        Assert.Equal((ushort)0xBEEF, reader.ReadUShort());
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
