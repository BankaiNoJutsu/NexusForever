using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Account.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationScheduleBuilderTests
{
    [Fact]
    public void Build_EmitsItemEssenceAndModifierRowsPerContentId()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationItem, new RewardRotationItemEntry { Id = 3u, Count = 6000u });
        SetEntries(sources.RewardRotationEssence, new RewardRotationEssenceEntry { Id = 5u });
        SetEntries(sources.RewardRotationModifier, new RewardRotationModifierEntry { Id = 12u, ModifierValue = 15f });

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
            sources,
            rewardRotationIndex: 1u,
            contentIds: new List<uint> { 12u, 13u });

        Assert.Equal(6, schedule.Entries.Count);
        Assert.All(schedule.Entries, row => Assert.Equal(RewardRotationScheduleDefaults.FallbackRotationDurationDays, row.Duration));

        ServerRewardRotationScheduleArray.ScheduleRow content12Item = schedule.Entries.Single(row =>
            row.ContentId == 12u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeItem);
        Assert.Equal(3u, content12Item.RewardKeyId);
        Assert.Equal(3u, content12Item.Value);

        ServerRewardRotationScheduleArray.ScheduleRow content13Modifier = schedule.Entries.Single(row =>
            row.ContentId == 13u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeModifier);
        Assert.Equal(12u, content13Modifier.RewardKeyId);
        Assert.Equal(12u, content13Modifier.Value);
    }

    [Fact]
    public void Build_ReturnsEmptyScheduleWhenContentListIsEmpty()
    {
        RewardRotationContentContextSources sources = CreateSources();

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
            sources,
            rewardRotationIndex: 0u,
            contentIds: Array.Empty<uint>());

        Assert.Empty(schedule.Entries);
    }

    [Fact]
    public void Build_WithLevelFiftyKnownDifficultyContext_EmitsEligibleRewardRows()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationItem,
            new RewardRotationItemEntry
            {
                Id = 3u,
                Count = 6000u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagNormal
            });
        SetEntries(sources.RewardRotationEssence,
            new RewardRotationEssenceEntry
            {
                Id = 5u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagVeteran
            });
        SetEntries(sources.RewardRotationModifier,
            new RewardRotationModifierEntry
            {
                Id = 12u,
                ModifierValue = 15f,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagNormal
            });

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
            sources,
            rewardRotationIndex: 1u,
            contentIds: new List<uint> { 12u },
            playerLevel: 50u,
            worldDifficultyFlags: RewardRotationScheduleBuilder.KnownWorldDifficultyFlags);

        Assert.Equal(3, schedule.Entries.Count);
        Assert.Contains(schedule.Entries, row => row.RewardKeyId == 3u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeItem);
        Assert.Contains(schedule.Entries, row => row.RewardKeyId == 5u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeEssence);
        Assert.Contains(schedule.Entries, row => row.RewardKeyId == 12u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeModifier);
    }

    [Fact]
    public void Build_WithKnownDifficultyContext_EmitsNormalAndVeteranRowsForSameContent()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationItem,
            new RewardRotationItemEntry
            {
                Id = 3u,
                Count = 6000u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagNormal
            },
            new RewardRotationItemEntry
            {
                Id = 4u,
                Count = 5000u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagVeteran
            });
        SetEntries(sources.RewardRotationEssence,
            new RewardRotationEssenceEntry
            {
                Id = 5u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagNormal
            },
            new RewardRotationEssenceEntry
            {
                Id = 6u,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagVeteran
            });
        SetEntries(sources.RewardRotationModifier,
            new RewardRotationModifierEntry
            {
                Id = 12u,
                ModifierValue = 5f,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagNormal
            },
            new RewardRotationModifierEntry
            {
                Id = 13u,
                ModifierValue = 15f,
                MinPlayerLevel = 50u,
                WorldDifficultyFlags = RewardRotationScheduleBuilder.WorldDifficultyFlagVeteran
            });

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
            sources,
            rewardRotationIndex: 1u,
            contentIds: new List<uint> { 12u },
            playerLevel: 50u,
            worldDifficultyFlags: RewardRotationScheduleBuilder.KnownWorldDifficultyFlags);

        Assert.Equal(6, schedule.Entries.Count);
        Assert.Equal(2, schedule.Entries.Count(row => row.RewardType == RewardRotationScheduleBuilder.RewardTypeItem));
        Assert.Equal(2, schedule.Entries.Count(row => row.RewardType == RewardRotationScheduleBuilder.RewardTypeEssence));
        Assert.Equal(2, schedule.Entries.Count(row => row.RewardType == RewardRotationScheduleBuilder.RewardTypeModifier));
        Assert.Contains(schedule.Entries, row => row.RewardKeyId == 3u && row.Value == 3u);
        Assert.Contains(schedule.Entries, row => row.RewardKeyId == 4u && row.Value == 4u);
        Assert.Equal(schedule.Entries.Count, schedule.Entries.Select(row => (row.ContentId, row.RewardType, row.RewardKeyId)).Distinct().Count());
    }

    private static RewardRotationContentContextSources CreateSources()
    {
        return new RewardRotationContentContextSources
        {
            RewardRotationContent = CreateTable<RewardRotationContentEntry>(),
            RewardRotationItem = CreateTable<RewardRotationItemEntry>(),
            RewardRotationEssence = CreateTable<RewardRotationEssenceEntry>(),
            RewardRotationModifier = CreateTable<RewardRotationModifierEntry>(),
            WorldZone = CreateTable<WorldZoneEntry>(),
            World = CreateTable<WorldEntry>(),
            PublicEvent = CreateTable<PublicEventEntry>(),
            MatchTypeRewardRotationContent = CreateTable<MatchTypeRewardRotationContentEntry>()
        };
    }

    private static void SetEntries<T>(GameTable<T> table, params T[] entries) where T : class, new()
    {
        PropertyInfo property = typeof(GameTable<T>).GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public);
        property?.SetValue(table, entries);
    }

    private static GameTable<T> CreateTable<T>() where T : class, new()
    {
        return (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
    }
}
