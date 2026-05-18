using NexusForever.Game.Entity;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public class GalacticArchiveUnlockRuleTests
{
    [Fact]
    public void IsSatisfied_TypeZeroUsesAchievementCompletion()
    {
        var rule = new ArchiveEntryUnlockRuleEntry
        {
            ArchiveEntryUnlockRuleEnum = 0,
            Object00 = 101
        };

        bool result = GalacticArchiveUnlockRule.IsSatisfied(
            rule,
            achievementId => achievementId == 101u,
            _ => false,
            _ => false);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_TypeTwoUsesQuestCompletionOnly()
    {
        var rule = new ArchiveEntryUnlockRuleEntry
        {
            ArchiveEntryUnlockRuleEnum = 2,
            Object00 = 101
        };

        bool result = GalacticArchiveUnlockRule.IsSatisfied(
            rule,
            achievementId => achievementId == 101u,
            _ => false,
            _ => false);

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfied_TypeOneUsesPathMissionCompletion()
    {
        var rule = new ArchiveEntryUnlockRuleEntry
        {
            ArchiveEntryUnlockRuleEnum = 1,
            Object00 = 811
        };

        bool result = GalacticArchiveUnlockRule.IsSatisfied(
            rule,
            _ => false,
            _ => false,
            pathMissionId => pathMissionId == 811u);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_FlagOneRequiresAllObjectsComplete()
    {
        var rule = new ArchiveEntryUnlockRuleEntry
        {
            ArchiveEntryUnlockRuleEnum = 0,
            ArchiveEntryUnlockRuleFlags = 1,
            Object00 = 101,
            Object01 = 102
        };

        bool result = GalacticArchiveUnlockRule.IsSatisfied(
            rule,
            achievementId => achievementId == 101u,
            _ => false,
            _ => false);

        Assert.False(result);
    }
}
