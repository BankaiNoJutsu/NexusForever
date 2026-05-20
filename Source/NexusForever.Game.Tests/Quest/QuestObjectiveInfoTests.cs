using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class QuestObjectiveInfoTests
{
    [Fact]
    public void DisablesDynamicProgress_ReturnsTrueWhenFlagSet()
    {
        var entry = new QuestObjectiveEntry
        {
            Flags = (uint)QuestObjectiveFlags.DisablesDynamicProgress
        };

        var info = new QuestObjectiveInfo(entry);

        Assert.True(info.DisablesDynamicProgress());
    }

    [Fact]
    public void DisablesDynamicProgress_ReturnsFalseWhenFlagNotSet()
    {
        var info = new QuestObjectiveInfo(new QuestObjectiveEntry());

        Assert.False(info.DisablesDynamicProgress());
    }
}