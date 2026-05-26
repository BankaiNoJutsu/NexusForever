using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class PathManagerTests
{
    [Fact]
    public void AddXp_AwardsInitialPathLevelRewardFromLevelRewardedState()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 2u, PathXP = 100u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    ObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Soldier, 1u),
                    Item2Id = 9001u
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                totalXp: 0u,
                levelRewarded: 0,
                out IPlayer player,
                out var playerProxy,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            manager.AddXp(1u);

            RecordingDispatchProxy<IInventory>.Invocation inventoryCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, inventoryCall.Arguments[0]);
            Assert.Equal(9001u, inventoryCall.Arguments[1]);
            Assert.Equal(1u, inventoryCall.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, inventoryCall.Arguments[3]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievementCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
            Assert.Same(player, achievementCall.Arguments[0]);
            Assert.Equal(AchievementType.PathLevel, achievementCall.Arguments[1]);
            Assert.Equal((uint)Path.Soldier, achievementCall.Arguments[2]);
            Assert.Equal(0u, achievementCall.Arguments[3]);
            Assert.Equal(1u, achievementCall.Arguments[4]);

            RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
                Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            var update = Assert.IsType<ServerPathUpdateXP>(sessionCall.Arguments[0]);
            Assert.Equal(1u, update.TotalXP);

            RecordingDispatchProxy<IPlayer>.Invocation castSpellCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
            Assert.Equal(53234u, castSpellCall.Arguments[0]);

            IPathEntry pathEntry = Assert.Single(manager, entry => entry.Path == Path.Soldier);
            Assert.Equal(1u, pathEntry.TotalXp);
            Assert.Equal(1, pathEntry.LevelRewarded);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddLevels_AtMaxPathLevelStillAwardsOutstandingCapReward()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 30u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 30u, PathXP = 3000u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 30u,
                    ObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Soldier, 30u),
                    Item2Id = 9030u
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                totalXp: 3000u,
                levelRewarded: 29,
                out IPlayer player,
                out var playerProxy,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            manager.AddLevels(1u);

            RecordingDispatchProxy<IInventory>.Invocation inventoryCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9030u, inventoryCall.Arguments[1]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation progressCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
            Assert.Equal(30u, progressCall.Arguments[4]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation guildCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
            Assert.Same(player, guildCall.Arguments[0]);
            Assert.Equal(AchievementType.GuildMaxPathLevel, guildCall.Arguments[1]);
            Assert.Equal(0u, guildCall.Arguments[2]);

            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

            RecordingDispatchProxy<IPlayer>.Invocation castSpellCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
            Assert.Equal(53234u, castSpellCall.Arguments[0]);

            IPathEntry pathEntry = Assert.Single(manager, entry => entry.Path == Path.Soldier);
            Assert.Equal(3000u, pathEntry.TotalXp);
            Assert.Equal(30, pathEntry.LevelRewarded);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ActivateMissions_SendsEpisodeProgressAndMissionActivate()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 25,
                [36] = 25
            });

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(9, currentEpisode.PathEpisodeId);

            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.Equal(9, episodeProgress.EpisodeId);
            Assert.Equal([35u, 36u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());

            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Assert.Equal([35u, 36u], missionActivate.Missions.Select(m => m.PathMissionId).ToArray());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMission_UpdatesMissionAndAwardsConfiguredPathXp()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 2u, PathXP = 100u }
            ],
            []);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 25
            });

            Assert.True(manager.CompleteMission(35));

            Assert.True(manager.IsMissionComplete(35));

            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal(35, advanced.PathMissionId);

            ServerPathMissionUpdate missionUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Single();
            Assert.True(missionUpdate.Mission.Completed);
            Assert.Equal(1u, missionUpdate.Mission.ObjectiveCompletionFlags);

            ServerPathUpdateXP xpUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathUpdateXP>()
                .Single();
            Assert.Equal(25u, xpUpdate.TotalXP);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionByObjectId_CompletesMatchingActiveMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, ObjectId = 1u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out _,
                out _,
                out _);
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [1254] = 0
            });

            Assert.True(manager.CompleteMissionByObjectId(1u));

            Assert.True(manager.IsMissionComplete(1254u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionBySoldierTowerDefenseId_CompletesMatchingEventMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 156u, PathEpisodeId = 8u, ObjectId = 2u }
            ],
            [
                new PathSoldierTowerDefenseEntry { Id = 2u, PathSoldierEventId = 2u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out _,
                out _,
                out _);
            manager.ActivateMissions(8, new Dictionary<ushort, uint>
            {
                [156] = 0
            });

            Assert.True(manager.CompleteMissionBySoldierTowerDefenseId(2u));

            Assert.True(manager.IsMissionComplete(156u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_CompletesMatchingHubMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, ObjectId = 46u }
            ],
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out _,
                out _,
                out _);
            manager.ActivateMissions(82, new Dictionary<ushort, uint>
            {
                [650] = 0
            });

            Assert.True(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.True(manager.IsMissionComplete(650u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static PathManager CreateManager(
        uint totalXp,
        byte levelRewarded,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        return CreateManager(
            Path.Soldier,
            totalXp,
            levelRewarded,
            out player,
            out playerProxy,
            out sessionProxy,
            out achievementManagerProxy,
            out inventoryProxy);
    }

    private static PathManager CreateManager(
        Path path,
        uint totalXp,
        byte levelRewarded,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementManagerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);

        playerProxy.SetProperty(nameof(IPlayer.Path), path);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        return new PathManager(player, new CharacterModel
        {
            Id = 42ul,
            Path =
            [
                new CharacterPathModel
                {
                    Id = 42ul,
                    Path = (byte)path,
                    Unlocked = 1,
                    TotalXp = totalXp,
                    LevelRewarded = levelRewarded
                }
            ]
        });
    }

    private static IServiceProvider BuildGameTableProvider(
        IEnumerable<PathLevelEntry> pathLevels,
        IEnumerable<PathRewardEntry> pathRewards,
        IEnumerable<PathMissionEntry> pathMissions = null,
        IEnumerable<PathSoldierTowerDefenseEntry> soldierTowerDefense = null,
        IEnumerable<PathSettlerImprovementGroupEntry> settlerImprovementGroups = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathLevel), CreateGameTable(pathLevels.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathReward), CreateGameTable(pathRewards.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathMission), CreateGameTable((pathMissions ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSoldierTowerDefense), CreateGameTable((soldierTowerDefense ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSettlerImprovementGroup), CreateGameTable((settlerImprovementGroups ?? []).ToArray()));

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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
