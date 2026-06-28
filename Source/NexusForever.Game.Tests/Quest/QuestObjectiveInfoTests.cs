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

    [Fact]
    public void UsesDynamicProgress_ReturnsTrueWhenFlagSet()
    {
        var entry = new QuestObjectiveEntry
        {
            Flags = (uint)QuestObjectiveFlags.UsesDynamicProgress
        };

        var info = new QuestObjectiveInfo(entry);

        Assert.True(info.UsesDynamicProgress());
    }

    [Fact]
    public void UsesDynamicProgress_ReturnsFalseWhenFlagNotSet()
    {
        var info = new QuestObjectiveInfo(new QuestObjectiveEntry());

        Assert.False(info.UsesDynamicProgress());
    }
}
