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
}
