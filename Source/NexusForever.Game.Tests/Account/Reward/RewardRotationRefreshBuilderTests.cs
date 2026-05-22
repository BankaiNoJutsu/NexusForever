using NexusForever.Game.Account.Reward;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationRefreshBuilderTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(3u)]
    [InlineData(RewardRotationRefreshBuilder.ContentTypeCount - 1u)]
    public void Build_SupportedIndex_ReturnsEmptyPlaceholderRefresh(uint rewardRotationIndex)
    {
        RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(rewardRotationIndex);

        Assert.NotNull(refresh);
        Assert.Equal(rewardRotationIndex, refresh.RewardRotationIndex);
        Assert.Empty(refresh.ScheduleArray.Entries);
        Assert.Empty(refresh.EntryStateArray.Entries);
        Assert.Equal(0, refresh.ScheduleEntryCount);
        Assert.Equal(0, refresh.EntryStateCount);
        Assert.True(refresh.IsPlaceholder);
    }

    [Fact]
    public void Build_OutOfRangeIndex_ReturnsNull()
    {
        RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(RewardRotationRefreshBuilder.ContentTypeCount);

        Assert.Null(refresh);
    }

    [Theory]
    [InlineData(0u, true)]
    [InlineData(RewardRotationRefreshBuilder.ContentTypeCount - 1u, true)]
    [InlineData(RewardRotationRefreshBuilder.ContentTypeCount, false)]
    public void IsSupported_MatchesEvidenceBackedIndexRange(uint rewardRotationIndex, bool expected)
    {
        bool isSupported = RewardRotationRefreshBuilder.IsSupported(rewardRotationIndex);

        Assert.Equal(expected, isSupported);
    }

    [Fact]
    public void Build_CustomProvider_PreservesObservableRowsAndResponseSource()
    {
        RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(2u, new TestRewardRotationRefreshProvider());

        Assert.Equal("test-provider", refresh.ResponseSource);
        Assert.False(refresh.IsPlaceholder);
        Assert.Single(refresh.ScheduleArray.Entries);
        Assert.Single(refresh.EntryStateArray.Entries);
    }

    private sealed class TestRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return new RewardRotationRefresh(
                rewardRotationIndex,
                contentContext: null,
                new ServerRewardRotationScheduleArray
                {
                    Entries =
                    {
                        new ServerRewardRotationScheduleArray.ScheduleRow
                        {
                            RewardKeyId = 10u,
                            ContentId = 11u,
                            Duration = 12f,
                            RewardType = 1,
                            Value = 13u
                        }
                    }
                },
                new ServerRewardRotationEntryStateArray
                {
                    Entries =
                    {
                        new ServerRewardRotationEntryStateArray.EntryStateRow
                        {
                            TypeId = 2u,
                            ContentId = 21u,
                            RewardTypeId = 22u,
                            State = 3,
                            Value = 23u
                        }
                    }
                },
                "test-provider");
        }
    }
}
