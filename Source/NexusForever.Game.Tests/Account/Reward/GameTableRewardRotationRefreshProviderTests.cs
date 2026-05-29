using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Account.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class GameTableRewardRotationRefreshProviderTests
{
    [Fact]
    public void Build_WithPersistedEntryState_SetsEntryStateCountAndSource()
    {
        var entryState = new ServerRewardRotationEntryStateArray
        {
            Entries =
            {
                RewardRotationEntryStateBuilder.CreateRow(
                    1u,
                    7u,
                    2u,
                    RewardRotationScheduleBuilder.RewardTypeEssence,
                    RewardRotationEntryStateBuilder.GrantFlagEssence)
            }
        };

        RewardRotationContentContextSources sources = CreateSources();
        RewardRotationRefresh refresh = BuildRefresh(1u, sources, entryState);

        Assert.Equal(1, refresh.EntryStateCount);
        Assert.Contains("entry-state", refresh.ResponseSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_PerContentCatalogSelection_UsesDistinctRowsPerContentId()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent,
            new RewardRotationContentEntry { Id = 1u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 2u, ContentTypeEnum = 0u });
        SetEntries(sources.RewardRotationItem,
            new RewardRotationItemEntry { Id = 10u, Count = 1u, MinPlayerLevel = 1u },
            new RewardRotationItemEntry { Id = 20u, Count = 2u, MinPlayerLevel = 1u });
        SetEntries(sources.RewardRotationEssence,
            new RewardRotationEssenceEntry { Id = 30u, MinPlayerLevel = 1u },
            new RewardRotationEssenceEntry { Id = 40u, MinPlayerLevel = 1u });
        SetEntries(sources.RewardRotationModifier,
            new RewardRotationModifierEntry { Id = 50u, ModifierValue = 1f, MinPlayerLevel = 1u },
            new RewardRotationModifierEntry { Id = 60u, ModifierValue = 2f, MinPlayerLevel = 1u });

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(
            sources,
            rewardRotationIndex: 0u,
            contentIds: new List<uint> { 1u, 2u });

        Assert.Equal(6, schedule.Entries.Count);
        Assert.NotEqual(
            schedule.Entries.Single(row => row.ContentId == 1u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeItem).RewardKeyId,
            schedule.Entries.Single(row => row.ContentId == 2u && row.RewardType == RewardRotationScheduleBuilder.RewardTypeItem).RewardKeyId);
    }

    private static RewardRotationRefresh BuildRefresh(
        uint rewardRotationIndex,
        RewardRotationContentContextSources sources,
        ServerRewardRotationEntryStateArray entryState)
    {
        List<ServerRewardRotationContentContext> contentContexts = RewardRotationContentContextBuilder.BuildContexts(sources, rewardRotationIndex);
        List<uint> contentIds = contentContexts.SelectMany(context => context.ContentIds).Distinct().OrderBy(id => id).ToList();
        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(sources, rewardRotationIndex, contentIds, gameTables: null);
        entryState ??= new ServerRewardRotationEntryStateArray();

        int entryStateCount = entryState.Entries.Count;
        string responseSource = contentIds.Count == 0
            ? entryStateCount > 0
                ? "game-table-entry-state-only"
                : "game-table-refresh-empty"
            : schedule.Entries.Count == 0
                ? entryStateCount > 0
                    ? "game-table-entry-state-only"
                    : "game-table-content-context-only"
                : entryStateCount > 0
                    ? "game-table-content-context-with-schedule-and-entry-state"
                    : "game-table-content-context-with-schedule";

        return new RewardRotationRefresh(
            rewardRotationIndex,
            contentContexts,
            schedule,
            entryState,
            responseSource);
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
