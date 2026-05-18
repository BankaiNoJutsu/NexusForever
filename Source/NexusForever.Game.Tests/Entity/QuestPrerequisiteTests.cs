using NexusForever.Game.Entity;
using NexusForever.Game.Static.Reputation;

namespace NexusForever.Game.Tests.Entity;

public class QuestPrerequisiteTests
{
    [Fact]
    public void MeetsFactionLevelRequirement_CompClearRequiresAtLeastRequiredLevel()
    {
        Assert.True(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Accepted, (uint)FactionLevel.Liked, false));
        Assert.True(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Liked, (uint)FactionLevel.Liked, false));
        Assert.False(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Neutral, (uint)FactionLevel.Liked, false));
    }

    [Fact]
    public void MeetsFactionLevelRequirement_CompSetRequiresNoHigherThanRequiredLevel()
    {
        Assert.True(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Wary, (uint)FactionLevel.Neutral, true));
        Assert.True(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Neutral, (uint)FactionLevel.Neutral, true));
        Assert.False(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Liked, (uint)FactionLevel.Neutral, true));
    }

    [Fact]
    public void MeetsFactionLevelRequirement_InvalidRequiredLevelFailsClosed()
    {
        Assert.False(QuestManager.MeetsFactionLevelRequirement(FactionLevel.Beloved, (uint)FactionLevel.Beloved + 1u, false));
    }

    [Theory]
    [InlineData(39, 40u, true)]
    [InlineData(40, 40u, false)]
    [InlineData(41, 40u, false)]
    public void HasActiveQuestCapacity_RejectsWhenCountReachesClientLimit(int activeQuestCount, uint maximumActiveQuests, bool expected)
    {
        Assert.Equal(expected, QuestManager.HasActiveQuestCapacity(activeQuestCount, maximumActiveQuests));
    }
}
