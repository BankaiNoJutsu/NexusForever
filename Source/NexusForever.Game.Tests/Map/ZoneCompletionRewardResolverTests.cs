using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game.Map;
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
    public void GetExplorationOnlyTitleRewards_WithAmbiguousDistinctRewards_ReturnsNoTitle()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildProvider(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                CharacterTitleIdReward = 77u
            },
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = 1u,
                CharacterTitleIdReward = 88u
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

    private static ServiceProvider BuildProvider(params ZoneCompletionEntry[] entries)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.ZoneCompletion), CreateGameTable(entries));

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
