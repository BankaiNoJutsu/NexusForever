using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

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

    [Fact]
    public void IsComplete_UsesValueForQuestCompleteChecklistCount()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                = 13,
                    AchievementTypeId = (uint)AchievementType.QuestCompleteChecklistCount,
                    Value             = 2
                },
                new AchievementChecklistEntry { Bit = 0u, ObjectId = 100u },
                new AchievementChecklistEntry { Bit = 1u, ObjectId = 101u },
                new AchievementChecklistEntry { Bit = 2u, ObjectId = 102u }))
        {
            Data0 = 2
        };

        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void QuestCompleteChecklistCount_OnlyCountsDistinctChecklistEntries()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 14,
                AchievementTypeId = (uint)AchievementType.QuestCompleteChecklistCount,
                Value             = 2
            },
            new AchievementChecklistEntry { Bit = 0u, ObjectId = 100u },
            new AchievementChecklistEntry { Bit = 1u, ObjectId = 101u });

        var manager = new TestAchievementManager(info);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildDisableProvider();

        try
        {
            manager.Check(info, 100u);
            manager.Check(info, 100u);

            Assert.False(manager.Get(info.Id).IsComplete());
            Assert.Equal(1u, manager.Get(info.Id).Data0);
            Assert.Equal(1u, manager.Get(info.Id).Data1);

            manager.Check(info, 101u);

            Assert.True(manager.Get(info.Id).IsComplete());
            Assert.Equal(2u, manager.Get(info.Id).Data0);
            Assert.Equal(3u, manager.Get(info.Id).Data1);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private sealed class TestAchievementInfo : IAchievementInfo
    {
        public TestAchievementInfo(AchievementEntry entry, params AchievementChecklistEntry[] checklistEntries)
        {
            Entry            = entry;
            ChecklistEntries = checklistEntries.ToList();
        }

        public ushort Id => (ushort)Entry.Id;
        public AchievementEntry Entry { get; }
        public List<AchievementChecklistEntry> ChecklistEntries { get; }
        public bool IsPlayerAchievement => true;
        public bool IsRealmFirst => false;
    }

    private sealed class TestAchievementManager : BaseAchievementManager<CharacterAchievementModel>
    {
        protected override ulong OwnerId => 1ul;

        public TestAchievementManager(IAchievementInfo info)
        {
            achievements.Add(info.Id, new Achievement<CharacterAchievementModel>(OwnerId, info));
        }

        public IAchievement Get(ushort id)
        {
            return achievements[id];
        }

        public override void CheckAchievements(NexusForever.Game.Abstract.Entity.IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0u, uint count = 1u)
        {
            throw new NotSupportedException();
        }

        public void Check(IAchievementInfo info, uint objectId)
        {
            CheckAchievements(null, new[] { info }, objectId, 0u, 1u);
        }

        public override void SetAchievementProgress(NexusForever.Game.Abstract.Entity.IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
        {
            throw new NotSupportedException();
        }

        protected override void SendAchievementUpdate(IEnumerable<IAchievement> updates)
        {
        }
    }

    private static IServiceProvider BuildDisableProvider()
    {
        var disableManager = new DisableManager();
        typeof(DisableManager)
            .GetField("disables", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(disableManager, ImmutableDictionary<ulong, Disable>.Empty);

        return new ServiceCollection()
            .AddSingleton(disableManager)
            .BuildServiceProvider();
    }
}
