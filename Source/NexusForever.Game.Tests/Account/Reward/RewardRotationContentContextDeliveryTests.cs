using NexusForever.Game.Account.Reward;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationContentContextDeliveryTests
{
    [Fact]
    public void CreatePackets_SingleDiagnosticRow_DoesNotEmitRewardTrackOpcode()
    {
        var refresh = new RewardRotationRefresh(
            2u,
            new ServerRewardRotationContentContext
            {
                RewardRotationIndex = 2u,
                ContentIds = { 1u, 2u }
            },
            new ServerRewardRotationScheduleArray(),
            new ServerRewardRotationEntryStateArray());

        Assert.Empty(refresh.GetContentContextPackets());
        Assert.Equal(0, refresh.ContentContextPacketCount);
        Assert.Equal(2, refresh.ContentContextIdCount);
    }

    [Fact]
    public void CreatePackets_MultipleDiagnosticRows_DoNotEmitRewardTrackReload()
    {
        var refresh = new RewardRotationRefresh(
            1u,
            new[]
            {
                new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = 1u,
                    ContentIds = { 1u, 2u, 3u, 4u, 5u }
                },
                new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = 1u,
                    ContentIds = { 6u, 7u }
                }
            },
            new ServerRewardRotationScheduleArray(),
            new ServerRewardRotationEntryStateArray());

        Assert.Empty(refresh.GetContentContextPackets());
        Assert.Equal(0, refresh.ContentContextPacketCount);
        Assert.Equal(2, refresh.ContentContexts.Count);
        Assert.Equal(new[] { 6u, 7u }, refresh.ContentContexts[1].ContentIds);
    }
}
