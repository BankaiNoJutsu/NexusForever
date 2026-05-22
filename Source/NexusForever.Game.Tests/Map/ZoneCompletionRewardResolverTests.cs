using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Map;

[Collection(LegacyServiceProviderCollection.Name)]
public class ZoneCompletionRewardResolverTests
{
    [Fact]
    public void GetExplorationOnlyTitleRewards_WithSingleExplorationOnlyReward_ReturnsTitle()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                CharacterTitleIdReward = 77u
            });
        LegacyServiceProvider.Provider = provider;

        try
        {
            IReadOnlyList<ushort> titleIds = ZoneCompletionRewardResolver.GetExplorationOnlyTitleRewards(10u);

            ushort titleId = Assert.Single(titleIds);
            Assert.Equal((ushort)77, titleId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GetExplorationOnlyTitleRewards_WithNonExplorationRequirements_ReturnsNoTitle()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                EpisodeQuestCount = 1u,
                CharacterTitleIdReward = 77u
            });
        LegacyServiceProvider.Provider = provider;

        try
        {
            Assert.Empty(ZoneCompletionRewardResolver.GetExplorationOnlyTitleRewards(10u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GetEntry_WithFactionRows_ReturnsMatchingFactionRow()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                CharacterTitleIdReward = 77u
            },
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Exile,
                CharacterTitleIdReward = 88u
            });
        LegacyServiceProvider.Provider = provider;

        try
        {
            ZoneCompletionEntry dominion = ZoneCompletionRewardResolver.GetEntry(10u, ZoneCompletionFaction.Dominion);
            ZoneCompletionEntry exile = ZoneCompletionRewardResolver.GetEntry(10u, ZoneCompletionFaction.Exile);

            Assert.Equal(77u, dominion.CharacterTitleIdReward);
            Assert.Equal(88u, exile.CharacterTitleIdReward);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void MeetsCategoryRequirements_WhenProgressMeetsThresholds_ReturnsTrue()
    {
        var entry = new ZoneCompletionEntry
        {
            EpisodeQuestCount = 2u,
            TaskQuestCount = 3u,
            ChallengeCount = 1u,
            DatacubeCount = 4u,
            TaleCount = 1u,
            JournalCount = 2u,
        };

        var progress = new ZoneCompletionProgress
        {
            EpisodeQuestCount = 2u,
            TaskQuestCount = 3u,
            ChallengeCount = 1u,
            DatacubeCount = 4u,
            TaleCount = 1u,
            JournalCount = 2u,
        };

        Assert.True(ZoneCompletionRewardResolver.MeetsCategoryRequirements(entry, progress));
    }

    [Fact]
    public void MeetsCategoryRequirements_WhenProgressBelowThreshold_ReturnsFalse()
    {
        var entry = new ZoneCompletionEntry
        {
            EpisodeQuestCount = 2u,
            TaskQuestCount = 3u,
        };

        var progress = new ZoneCompletionProgress
        {
            EpisodeQuestCount = 2u,
            TaskQuestCount = 2u,
        };

        Assert.False(ZoneCompletionRewardResolver.MeetsCategoryRequirements(entry, progress));
    }

    [Fact]
    public void TryGetTitleReward_WithExplorationOnlyRow_GrantsWithoutCategoryProgress()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion);

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                CharacterTitleIdReward = 77u
            });
        LegacyServiceProvider.Provider = provider;

        try
        {
            bool granted = ZoneCompletionRewardResolver.TryGetTitleReward(
                player,
                10u,
                explorationComplete: true,
                out ushort titleId);

            Assert.True(granted);
            Assert.Equal((ushort)77, titleId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryGetTitleReward_WithCategoryRequirements_BlockedUntilProgressMet()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Exile,
                EpisodeQuestCount = 1u,
                CharacterTitleIdReward = 88u
            });
        LegacyServiceProvider.Provider = provider;

        try
        {
            Assert.False(ZoneCompletionRewardResolver.TryGetTitleReward(
                player,
                10u,
                explorationComplete: true,
                out _));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GetPlayerZoneCompletionFaction_MapsDominionAndExile()
    {
        Assert.Equal(ZoneCompletionFaction.Dominion, ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.Dominion));
        Assert.Equal(ZoneCompletionFaction.Exile, ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.Exile));
        Assert.Null(ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.MatchingTeam1));
    }

    private static ServiceProvider BuildProvider(params ZoneCompletionEntry[] entries)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.ZoneCompletion), CreateGameTable(entries));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable<Quest2Entry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.EpisodeQuest), CreateGameTable<EpisodeQuestEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Datacube), CreateGameTable<DatacubeEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.DatacubeVolume), CreateGameTable<DatacubeVolumeEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), CreateGameTable<WorldZoneEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZone), CreateGameTable<MapZoneEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable<ChallengeEntry>());

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
