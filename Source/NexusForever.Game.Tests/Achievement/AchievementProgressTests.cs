using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Achievement;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Achievement;

public class AchievementProgressTests
{
    [Fact]
    public void IsComplete_AllowsProgressBeyondRequiredValue()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 10,
                Value = 3
            }))
        {
            Data0 = 5
        };

        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void IsComplete_TreatsZeroValueAsSingleEventRequirement()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 11,
                Value = 0
            }))
        {
            Data0 = 1
        };

        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void IsComplete_DoesNotCompleteZeroValueBeforeProgress()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 12,
                Value = 0
            }));

        Assert.False(achievement.IsComplete());
    }

    private sealed class TestAchievementInfo : IAchievementInfo
    {
        public TestAchievementInfo(AchievementEntry entry)
        {
            Entry = entry;
        }

        public ushort Id => (ushort)Entry.Id;
        public AchievementEntry Entry { get; }
        public List<AchievementChecklistEntry> ChecklistEntries { get; } = [];
        public bool IsPlayerAchievement => true;
        public bool IsRealmFirst => false;
    }
}
