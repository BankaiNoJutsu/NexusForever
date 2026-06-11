using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Map;

public class ZoneCompletionRewardResolverTests
{
    [Fact]
    public void GetExplorationOnlyTitleRewards_WithSingleExplorationOnlyReward_ReturnsTitle()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                CharacterTitleIdReward = 77u
            });

        IReadOnlyList<ushort> titleIds = ZoneCompletionRewardResolver.GetExplorationOnlyTitleRewards(10u, gameTableManager);

        ushort titleId = Assert.Single(titleIds);
        Assert.Equal((ushort)77, titleId);
    }

    [Fact]
    public void GetExplorationOnlyTitleRewards_WithNonExplorationRequirements_ReturnsNoTitle()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                EpisodeQuestCount = 1u,
                CharacterTitleIdReward = 77u
            });

        Assert.Empty(ZoneCompletionRewardResolver.GetExplorationOnlyTitleRewards(10u, gameTableManager));
    }

    [Fact]
    public void GetEntry_WithFactionRows_ReturnsMatchingFactionRow()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
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

        ZoneCompletionEntry dominion = ZoneCompletionRewardResolver.GetEntry(10u, ZoneCompletionFaction.Dominion, gameTableManager);
        ZoneCompletionEntry exile = ZoneCompletionRewardResolver.GetEntry(10u, ZoneCompletionFaction.Exile, gameTableManager);

        Assert.Equal(77u, dominion.CharacterTitleIdReward);
        Assert.Equal(88u, exile.CharacterTitleIdReward);
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

        GameTableManager gameTableManager = CreateGameTableManager(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                CharacterTitleIdReward = 77u
            });

        bool granted = ZoneCompletionRewardResolver.TryGetTitleReward(
            player,
            10u,
            explorationComplete: true,
            out ushort titleId,
            gameTableManager);

        Assert.True(granted);
        Assert.Equal((ushort)77, titleId);
    }

    [Fact]
    public void TryGetTitleReward_WithCategoryRequirements_BlockedUntilProgressMet()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);

        GameTableManager gameTableManager = CreateGameTableManager(
            new ZoneCompletionEntry
            {
                MapZoneId = 10u,
                ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Exile,
                EpisodeQuestCount = 1u,
                CharacterTitleIdReward = 88u
            });

        Assert.False(ZoneCompletionRewardResolver.TryGetTitleReward(
            player,
            10u,
            explorationComplete: true,
            out _,
            gameTableManager));
    }

    [Fact]
    public void TryGetTitleReward_WhenProgressTablesMissing_ReturnsFalse()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion);

        GameTableManager gameTableManager = CreateGameTableManagerWithTables(
            zoneCompletionEntries:
            [
                new ZoneCompletionEntry
                {
                    MapZoneId = 20u,
                    ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                    DatacubeCount = 1u,
                    CharacterTitleIdReward = 77u
                }
            ],
            mapZoneEntries:
            [
                new MapZoneEntry
                {
                    Id = 20u,
                    WorldZoneId = 200u
                }
            ],
            worldZoneEntries:
            [
                new WorldZoneEntry
                {
                    Id = 200u
                }
            ]);

        Assert.False(ZoneCompletionRewardResolver.TryGetTitleReward(
            player,
            20u,
            explorationComplete: true,
            out _,
            gameTableManager));
    }

    [Fact]
    public void TryGetTitleReward_RechecksMapZoneTablesAfterPreviousMissingLookup()
    {
        const uint mapZoneId = 60u;
        const uint worldZoneId = 600u;

        IPlayer player = CreateDominionPlayerWithDatacube();

        GameTableManager missingMapGameTableManager = CreateGameTableManagerWithTables(
            zoneCompletionEntries:
            [
                new ZoneCompletionEntry
                {
                    MapZoneId = mapZoneId,
                    ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                    DatacubeCount = 1u,
                    CharacterTitleIdReward = 77u
                }
            ]);

        Assert.False(ZoneCompletionRewardResolver.TryGetTitleReward(
            player,
            mapZoneId,
            explorationComplete: true,
            out _,
            missingMapGameTableManager));

        GameTableManager completeGameTableManager = CreateGameTableManagerWithTables(
            zoneCompletionEntries:
            [
                new ZoneCompletionEntry
                {
                    MapZoneId = mapZoneId,
                    ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Dominion,
                    DatacubeCount = 1u,
                    CharacterTitleIdReward = 77u
                }
            ],
            mapZoneEntries:
            [
                new MapZoneEntry
                {
                    Id = mapZoneId,
                    WorldZoneId = worldZoneId
                }
            ],
            worldZoneEntries:
            [
                new WorldZoneEntry
                {
                    Id = worldZoneId
                }
            ],
            datacubeEntries:
            [
                new DatacubeEntry
                {
                    Id = 61u,
                    WorldZoneId = worldZoneId,
                    DatacubeTypeEnum = 0u,
                    DatacubeFactionEnum = 1u
                }
            ]);

        bool granted = ZoneCompletionRewardResolver.TryGetTitleReward(
            player,
            mapZoneId,
            explorationComplete: true,
            out ushort titleId,
            completeGameTableManager);

        Assert.True(granted);
        Assert.Equal((ushort)77, titleId);
    }

    [Fact]
    public void ZoneMap_WhenHexTablesMissing_IsIncompleteWithZeroExploredPercent()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithTables();

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var zoneMap = new ZoneMap(new MapZoneEntry
        {
            Id = 30u,
            HexMinX = 0u,
            HexMinY = 0u,
            HexLimX = 1u,
            HexLimY = 1u
        }, player, gameTableManager);

        zoneMap.AddHexGroup(1, sendUpdate: false);

        Assert.False(zoneMap.IsComplete);
        Assert.False(zoneMap.HasHexGroup(1));
        Assert.Equal(0f, zoneMap.GetExploredPercent());
    }

    [Fact]
    public void ZoneMapManager_WhenPersistedMapZoneMissing_SkipsHexGroup()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithTables();

        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var model = new CharacterModel();
        model.ZonemapHexgroup.Add(new CharacterZonemapHexgroupModel
        {
            ZoneMap = 40,
            HexGroup = 1
        });

        var manager = new ZoneMapManager(player, model, gameTableManager);

        Assert.Equal(0, manager.GetMapZoneExploredPercent(40));
    }

    [Fact]
    public void GetPlayerZoneCompletionFaction_MapsDominionAndExile()
    {
        Assert.Equal(ZoneCompletionFaction.Dominion, ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.Dominion));
        Assert.Equal(ZoneCompletionFaction.Exile, ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.Exile));
        Assert.Null(ZoneCompletionRewardResolver.GetPlayerZoneCompletionFaction(Faction.MatchingTeam1));
    }

    private static GameTableManager CreateGameTableManager(params ZoneCompletionEntry[] entries)
    {
        return CreateGameTableManagerWithTables(
            zoneCompletionEntries: entries,
            questEntries: [],
            episodeQuestEntries: [],
            datacubeEntries: [],
            datacubeVolumeEntries: [],
            worldZoneEntries: [],
            mapZoneEntries: [],
            challengeEntries: []);
    }

    private static GameTableManager CreateGameTableManagerWithTables(
        ZoneCompletionEntry[] zoneCompletionEntries = null,
        Quest2Entry[] questEntries = null,
        EpisodeQuestEntry[] episodeQuestEntries = null,
        DatacubeEntry[] datacubeEntries = null,
        DatacubeVolumeEntry[] datacubeVolumeEntries = null,
        WorldZoneEntry[] worldZoneEntries = null,
        MapZoneEntry[] mapZoneEntries = null,
        ChallengeEntry[] challengeEntries = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (zoneCompletionEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ZoneCompletion), CreateGameTable(zoneCompletionEntries));
        if (questEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable(questEntries));
        if (episodeQuestEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.EpisodeQuest), CreateGameTable(episodeQuestEntries));
        if (datacubeEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Datacube), CreateGameTable(datacubeEntries));
        if (datacubeVolumeEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.DatacubeVolume), CreateGameTable(datacubeVolumeEntries));
        if (worldZoneEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), CreateGameTable(worldZoneEntries));
        if (mapZoneEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZone), CreateGameTable(mapZoneEntries));
        if (challengeEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(challengeEntries));

        return gameTableManager;
    }

    private static IPlayer CreateDominionPlayerWithDatacube()
    {
        IDatacube datacube = RecordingDispatchProxy<IDatacube>.Create(out _);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out RecordingDispatchProxy<IDatacubeManager> datacubeManagerProxy);
        datacubeManagerProxy.SetMethodReturn(nameof(IDatacubeManager.GetDatacube), datacube);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);
        return player;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
