using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Achievement;

[Collection(LegacyServiceProviderCollection.Name)]
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
            ProgressCount = 5
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
            ProgressCount = 1
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
            ProgressCount = 2
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
            Assert.Equal(1u, manager.Get(info.Id).ProgressCount);
            Assert.Equal(1u, manager.Get(info.Id).CreditedChecklistMask);

            manager.Check(info, 101u);

            Assert.True(manager.Get(info.Id).IsComplete());
            Assert.Equal(2u, manager.Get(info.Id).ProgressCount);
            Assert.Equal(3u, manager.Get(info.Id).CreditedChecklistMask);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void KillCreatureGroup_DoesNotTreatZeroObjectAsWildcard()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 15,
                AchievementTypeId = (uint)AchievementType.KillCreatureGroup,
                ObjectId          = 0,
                Value             = 1
            });

        var manager = new TestAchievementManager(info);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildDisableProvider();

        try
        {
            manager.Check(info, 1463u);

            IAchievement achievement = manager.Get(info.Id);
            Assert.False(achievement.IsComplete());
            Assert.Equal(0u, achievement.ProgressCount);
            Assert.Empty(manager.SentUpdates);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void KillCreatureGroup_UpdatesWhenObjectMatches()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 16,
                AchievementTypeId = (uint)AchievementType.KillCreatureGroup,
                ObjectId          = 1463,
                Value             = 1
            });

        var manager = new TestAchievementManager(info);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildDisableProvider();

        try
        {
            manager.Check(info, 1463u);

            IAchievement achievement = manager.Get(info.Id);
            Assert.True(achievement.IsComplete());
            Assert.Equal(1u, achievement.ProgressCount);
            Assert.Single(manager.SentUpdates);
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
        private readonly List<IAchievement> sentUpdates = new();

        protected override ulong OwnerId => 1ul;

        public IReadOnlyList<IAchievement> SentUpdates => sentUpdates;

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
            sentUpdates.AddRange(updates);
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
