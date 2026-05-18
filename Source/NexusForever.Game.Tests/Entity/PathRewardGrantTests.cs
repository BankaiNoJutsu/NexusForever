using NexusForever.Game.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.GameTable.Model;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public class PathRewardGrantTests
{
    [Theory]
    [InlineData(Path.Soldier, 1u, 6u)]
    [InlineData(Path.Soldier, 2u, 7u)]
    [InlineData(Path.Soldier, 30u, 35u)]
    [InlineData(Path.Settler, 1u, 36u)]
    [InlineData(Path.Settler, 2u, 37u)]
    [InlineData(Path.Settler, 30u, 65u)]
    [InlineData(Path.Scientist, 1u, 66u)]
    [InlineData(Path.Scientist, 2u, 67u)]
    [InlineData(Path.Scientist, 30u, 95u)]
    [InlineData(Path.Explorer, 1u, 96u)]
    [InlineData(Path.Explorer, 2u, 97u)]
    [InlineData(Path.Explorer, 30u, 125u)]
    public void GetLevelRewardObjectId_ReturnsClientRewardObject(Path path, uint level, uint expectedObjectId)
    {
        Assert.Equal(expectedObjectId, PathRewardGrant.GetLevelRewardObjectId(path, level));
    }

    [Theory]
    [InlineData(nameof(PathRewardEntry.Item2Id), 12886u)]
    [InlineData(nameof(PathRewardEntry.Spell4Id), 69661u)]
    [InlineData(nameof(PathRewardEntry.CharacterTitleId), 18u)]
    [InlineData(nameof(PathRewardEntry.PathScientistScanBotProfileId), 6u)]
    public void IsGrantableLevelReward_AllowsSupportedLevelRewardKinds(string field, uint value)
    {
        var entry = new PathRewardEntry();
        typeof(PathRewardEntry).GetField(field)!.SetValue(entry, value);

        Assert.True(PathRewardGrant.IsGrantableLevelReward(entry));
    }

    [Fact]
    public void IsGrantableLevelReward_RejectsNonLevelRewards()
    {
        var entry = new PathRewardEntry
        {
            Item2Id = 12886,
            PathRewardTypeEnum = 1
        };

        Assert.False(PathRewardGrant.IsGrantableLevelReward(entry));
    }

    [Fact]
    public void IsGrantableLevelReward_RejectsFlaggedRewards()
    {
        var entry = new PathRewardEntry
        {
            Item2Id = 12886,
            PathRewardFlags = 1
        };

        Assert.False(PathRewardGrant.IsGrantableLevelReward(entry));
    }

    [Fact]
    public void IsGrantableLevelReward_RejectsEmptyRewardRows()
    {
        Assert.False(PathRewardGrant.IsGrantableLevelReward(new PathRewardEntry()));
    }
}
