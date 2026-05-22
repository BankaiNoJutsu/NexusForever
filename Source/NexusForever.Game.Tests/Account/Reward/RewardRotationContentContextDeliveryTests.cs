using NexusForever.Game.Account.Reward;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationContentContextDeliveryTests
{
    [Fact]
    public void CreatePackets_SingleRow_UsesSingleContentContextOpcode()
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

        IWritable packet = Assert.Single(refresh.GetContentContextPackets());

        Assert.IsType<ServerRewardRotationContentContext>(packet);
    }

    [Fact]
    public void CreatePackets_MultipleRows_UsesContentContextArrayOpcode()
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

        var packet = Assert.IsType<ServerRewardRotationContentContextArray>(Assert.Single(refresh.GetContentContextPackets()));

        Assert.Equal(2, packet.Entries.Count);
        Assert.Equal(5, packet.Entries[0].ContentIds.Count);
        Assert.Equal(new[] { 6u, 7u }, packet.Entries[1].ContentIds);
    }
}
