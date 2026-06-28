using System.Collections.Immutable;
using System.Reflection;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Achievement;

public class AchievementProgressTests
{
    [Fact]
    public void IsComplete_AllowsProgressBeyondRequiredProgress()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 10,
                RequiredProgress = 3
            }))
        {
            ProgressCount = 5
        };

        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void IsComplete_TreatsZeroRequiredProgressAsSingleEventRequirement()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 11,
                RequiredProgress = 0
            }))
        {
            ProgressCount = 1
        };

        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void IsComplete_DoesNotCompleteZeroRequiredProgressBeforeProgress()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(new AchievementEntry
            {
                Id = 12,
                RequiredProgress = 0
            }));

        Assert.False(achievement.IsComplete());
    }

    [Fact]
    public void IsComplete_UsesRequiredProgressForQuestCompleteChecklistCount()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                = 13,
                    AchievementTypeId = (uint)AchievementType.QuestCompleteChecklistCount,
                    RequiredProgress  = 2
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
    public void IsComplete_DoesNotAliasChecklistBit32ToBit0()
    {
        var achievement = new Achievement<CharacterAchievementModel>(
            1u,
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                = 17,
                    AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist
                },
                new AchievementChecklistEntry { Bit = 32u, ObjectId = 100u }))
        {
            CompletedChecklistMask = 1u
        };

        Assert.False(achievement.IsComplete());
    }

    [Fact]
    public void QuestCompleteChecklistCount_OnlyCountsDistinctChecklistEntries()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 14,
                AchievementTypeId = (uint)AchievementType.QuestCompleteChecklistCount,
                RequiredProgress  = 2
            },
            new AchievementChecklistEntry { Bit = 0u, ObjectId = 100u },
            new AchievementChecklistEntry { Bit = 1u, ObjectId = 101u });

        var manager = new TestAchievementManager(info);

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

    [Fact]
    public void QuestCompleteChecklist_Q3486CreditsArrivalAchievement3469Rows()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 3469,
                AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist
            },
            new AchievementChecklistEntry { Id = 4245u, AchievementId = 3469u, Bit = 0u, ObjectId = 3486u },
            new AchievementChecklistEntry { Id = 4246u, AchievementId = 3469u, Bit = 1u, ObjectId = 3667u });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 3486u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.False(achievement.IsComplete());
        Assert.Equal(1u, achievement.CompletedChecklistMask);
        Assert.Single(manager.SentUpdates);

        manager.Check(info, 3667u);

        Assert.True(achievement.IsComplete());
        Assert.Equal(3u, achievement.CompletedChecklistMask);
        Assert.Equal(2, manager.SentUpdates.Count);
    }

    [Fact]
    public void QuestCompleteChecklist_Q3486CreditsArrivalAchievement5327Rows()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 5327,
                AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist
            },
            new AchievementChecklistEntry { Id = 6907u, AchievementId = 5327u, Bit = 0u, ObjectId = 3486u },
            new AchievementChecklistEntry { Id = 6908u, AchievementId = 5327u, Bit = 1u, ObjectId = 3667u },
            new AchievementChecklistEntry { Id = 6910u, AchievementId = 5327u, Bit = 2u, ObjectId = 3480u });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 3486u);
        manager.Check(info, 3667u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.False(achievement.IsComplete());
        Assert.Equal(3u, achievement.CompletedChecklistMask);
        Assert.Equal(2, manager.SentUpdates.Count);

        manager.Check(info, 3480u);

        Assert.True(achievement.IsComplete());
        Assert.Equal(7u, achievement.CompletedChecklistMask);
        Assert.Equal(3, manager.SentUpdates.Count);
    }

    [Fact]
    public void QuestCompleteChecklist_Q5597CreditsBloodstoneAchievement4134Rows()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                   = 4134,
                AchievementTypeId    = (uint)AchievementType.QuestCompleteChecklist,
                PrerequisiteId       = 18u,
                PrerequisiteIdServer = 18u
            },
            new AchievementChecklistEntry { Id = 5494u, AchievementId = 4134u, Bit = 0u, ObjectId = 5596u },
            new AchievementChecklistEntry { Id = 5495u, AchievementId = 4134u, Bit = 1u, ObjectId = 5597u },
            new AchievementChecklistEntry { Id = 5496u, AchievementId = 4134u, Bit = 2u, ObjectId = 5604u });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 5596u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.False(achievement.IsComplete());
        Assert.Equal(1u, achievement.CompletedChecklistMask);
        Assert.Single(manager.SentUpdates);

        manager.Check(info, 5597u);

        Assert.False(achievement.IsComplete());
        Assert.Equal(3u, achievement.CompletedChecklistMask);
        Assert.Equal(2, manager.SentUpdates.Count);

        manager.Check(info, 5604u);

        Assert.True(achievement.IsComplete());
        Assert.Equal(7u, achievement.CompletedChecklistMask);
        Assert.Equal(3, manager.SentUpdates.Count);
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
                RequiredProgress  = 1
            });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 1463u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.False(achievement.IsComplete());
        Assert.Equal(0u, achievement.ProgressCount);
        Assert.Empty(manager.SentUpdates);
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
                RequiredProgress  = 1
            });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 1463u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.True(achievement.IsComplete());
        Assert.Equal(1u, achievement.ProgressCount);
        Assert.Single(manager.SentUpdates);
    }

    [Fact]
    public void QuestCompleteChecklistCount_IgnoresChecklistBitsOutsideMask()
    {
        var info = new TestAchievementInfo(
            new AchievementEntry
            {
                Id                = 18,
                AchievementTypeId = (uint)AchievementType.QuestCompleteChecklistCount,
                RequiredProgress  = 1
            },
            new AchievementChecklistEntry { Bit = 32u, ObjectId = 100u });

        var manager = new TestAchievementManager(info);

        manager.Check(info, 100u);

        IAchievement achievement = manager.Get(info.Id);
        Assert.False(achievement.IsComplete());
        Assert.Equal(0u, achievement.ProgressCount);
        Assert.Equal(0u, achievement.CreditedChecklistMask);
        Assert.Empty(manager.SentUpdates);
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
            : base(CreateDisableManager())
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

    private static DisableManager CreateDisableManager()
    {
        var disableManager = new DisableManager();
        typeof(DisableManager)
            .GetField("disables", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(disableManager, ImmutableDictionary<ulong, Disable>.Empty);

        return disableManager;
    }
}
