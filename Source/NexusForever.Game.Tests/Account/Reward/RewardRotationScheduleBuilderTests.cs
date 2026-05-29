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
        Assert.Equal(6000u, content12Item.Value);

        ServerRewardRotationScheduleArray.ScheduleRow content13Modifier = schedule.Entries.Single(row =>
            row.ContentId == 13u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeModifier);
        Assert.Equal(12u, content13Modifier.RewardKeyId);
        Assert.Equal(15u, content13Modifier.Value);
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
