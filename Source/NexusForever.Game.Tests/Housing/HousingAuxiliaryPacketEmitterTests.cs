using NexusForever.Game.Housing;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Housing;

public class HousingAuxiliaryPacketEmitterTests
{
    [Fact]
    public void BuildResidenceSessionPackets_ReturnsSevenMappedHousingAuxPackets()
    {
        IReadOnlyList<IWritable> packets = HousingAuxiliaryPacketEmitter.BuildResidenceSessionPackets(0x0102030405060708ul);

        Assert.Equal(7, packets.Count);
        Assert.IsType<ServerHousingResidenceEmpty>(packets[0]);
        Assert.IsType<ServerHousingResidenceUInt15>(packets[1]);
        Assert.IsType<ServerHousingResidenceUInt15Alt>(packets[2]);
        Assert.IsType<ServerHousingResidenceWideString>(packets[3]);
        Assert.IsType<ServerHousingResidenceEmptyFollowUp>(packets[4]);
        Assert.IsType<ServerHousingBasicsEmpty>(packets[5]);
        Assert.IsType<ServerHousingBasicsFollowup>(packets[6]);
    }
}
