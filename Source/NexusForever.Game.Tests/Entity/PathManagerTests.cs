using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
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
    public void Constructor_WithPersistedCompletedMission_RestoresCompletionState()
    {
        PathManager manager = CreateManager(
            Path.Explorer,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out _,
            out _,
            out _,
            out _,
            pathMissionModels:
            [
                new CharacterPathMissionModel
                {
                    Id            = 42ul,
                    PathMissionId = 35,
                    PathEpisodeId = 9,
                    State         = (byte)PathMissionState.Complete,
                    Completed     = 1,
                    ProgressCount = 1
                }
            ]);

        Assert.True(manager.IsMissionComplete(35));
    }

    [Fact]
    public void Save_WithActivatedMission_CreatesPersistedMissionState()
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
            [35] = 25
        });

        using CharacterContext context = CreateCharacterContext();
        manager.Save(context);

        EntityEntry<CharacterPathMissionModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterPathMissionModel>());
        Assert.Equal(EntityState.Added, entry.State);
        CharacterPathMissionModel model = entry.Entity;
        Assert.Equal(42ul, model.Id);
        Assert.Equal(35, model.PathMissionId);
        Assert.Equal(9, model.PathEpisodeId);
        Assert.Equal((byte)PathMissionState.Started, model.State);
        Assert.Equal(0, model.Completed);
        Assert.Equal(25u, model.Xp);
    }

    [Fact]
    public void Save_WithCompletedPersistedMission_UpdatesPersistedMissionState()
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
                out _,
                out _,
                out _,
                pathMissionModels:
                [
                    new CharacterPathMissionModel
                    {
                        Id            = 42ul,
                        PathMissionId = 35,
                        PathEpisodeId = 9,
                        State         = (byte)PathMissionState.Started,
                        ProgressData  = 1
                    }
                ]);

            Assert.True(manager.CompleteActiveMission(35));

            using CharacterContext context = CreateCharacterContext();
            manager.Save(context);

            EntityEntry<CharacterPathMissionModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterPathMissionModel>());
            Assert.Equal(EntityState.Modified, entry.State);
            Assert.True(entry.Property(p => p.Completed).IsModified);
            Assert.True(entry.Property(p => p.ProgressCount).IsModified);
            Assert.True(entry.Property(p => p.ProgressData).IsModified);
            Assert.True(entry.Property(p => p.State).IsModified);
            Assert.Equal(1, entry.Entity.Completed);
            Assert.Equal(1u, entry.Entity.ProgressCount);
            Assert.Equal((byte)PathMissionState.Complete, entry.Entity.State);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedActiveMission_ReplaysEpisodeState()
    {
        PathManager manager = CreateManager(
            Path.Explorer,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out _,
            out var sessionProxy,
            out _,
            out _,
            pathMissionModels:
            [
                new CharacterPathMissionModel
                {
                    Id            = 42ul,
                    PathMissionId = 35,
                    PathEpisodeId = 9,
                    State         = (byte)PathMissionState.Started,
                    ProgressCount = 2,
                    ProgressData  = 1,
                    Xp            = 25
                }
            ]);

        manager.SendInitialPackets();

        IReadOnlyList<object> messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .ToList();

        Assert.IsType<ServerPathInitialise>(messages[0]);
        ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[1]);
        Assert.Equal(9, currentEpisode.PathEpisodeId);
        ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[2]);
        Mission mission = Assert.Single(episodeProgress.Missions);
        Assert.Equal(35u, mission.PathMissionId);
        Assert.Equal(2u, mission.ProgressCount);
        Assert.Equal(1u, mission.ProgressData);
        ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[3]);
        Assert.Equal(35u, Assert.Single(missionActivate.Missions).PathMissionId);
    }

    [Fact]
    public void SendInitialPackets_WithPersistedCompletedMission_DoesNotReplayEpisodeState()
    {
        PathManager manager = CreateManager(
            Path.Explorer,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out _,
            out var sessionProxy,
            out _,
            out _,
            pathMissionModels:
            [
                new CharacterPathMissionModel
                {
                    Id            = 42ul,
                    PathMissionId = 35,
                    PathEpisodeId = 9,
                    State         = (byte)PathMissionState.Complete,
                    Completed     = 1,
                    ProgressCount = 1
                }
            ]);

        manager.SendInitialPackets();

        object message = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0]));
        Assert.IsType<ServerPathInitialise>(message);
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithMatchingRootZone_ActivatesFactionFilteredMissions()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u },
                new PathMissionEntry { Id = 651u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 1u },
                new PathMissionEntry { Id = 652u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 2u },
                new PathMissionEntry { Id = 653u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Soldier, PathMissionFactionEnum = 0u }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 82u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Settler }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u },
                new WorldZoneEntry { Id = 11u, ParentZoneId = 10u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 11u, ParentZoneId = 10u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(82u, currentEpisode.PathEpisodeId);

            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.Equal(82u, episodeProgress.EpisodeId);
            Assert.Equal([650u, 651u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());

            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Assert.Equal([650u, 651u], missionActivate.Missions.Select(m => m.PathMissionId).ToArray());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithPersistedActiveEpisode_DoesNotDuplicateActivationPackets()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 82u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Settler }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _,
                pathMissionModels:
                [
                    new CharacterPathMissionModel
                    {
                        Id            = 42ul,
                        PathMissionId = 650,
                        PathEpisodeId = 82,
                        State         = (byte)PathMissionState.Started
                    }
                ]);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 10u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithMissionPrerequisites_ActivatesOnlyEligibleMissions()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u, PrerequisiteId = 700u },
                new PathMissionEntry { Id = 651u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u, PrerequisiteId = 701u },
                new PathMissionEntry { Id = 652u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 82u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Settler }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u }
            ],
            prerequisites:
            [
                CreatePathPrerequisite(700u, Path.Settler),
                CreatePathPrerequisite(701u, Path.Soldier)
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 10u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            ServerPathEpisodeProgress episodeProgress = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathEpisodeProgress>()
                .Single();
            Assert.Equal([650u, 652u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithoutMatchingEpisode_DoesNotSendPackets()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 82u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Settler }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 10u });

            Assert.False(manager.TryActivateCurrentZoneEpisode());
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
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
            Assert.Equal(1u, missionUpdate.Mission.ProgressCount);

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
    public void CompleteMission_WithKnownMissionAndNoConfiguredXp_AwardsGameFormulaPathXp()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 2u, PathXP = 100u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ],
            gameFormulas:
            [
                new GameFormulaEntry
                {
                    Id = 0x017Au,
                    Dataint0 = 25u
                }
            ]);

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

            Assert.True(manager.CompleteMission(35));

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
    public void CompleteMission_WithKnownMissionAndNoFormulaXp_AwardsClientFallbackPathXp()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 2u, PathXP = 100u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

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

            Assert.True(manager.CompleteMission(35));

            ServerPathUpdateXP xpUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathUpdateXP>()
                .Single();
            Assert.Equal(50u, xpUpdate.TotalXP);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMission_WithMismatchedMissionPathAndNoConfiguredXp_DoesNotAwardFallbackXp()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 2u, PathXP = 100u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);

            Assert.True(manager.CompleteMission(35));

            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathUpdateXP>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMission_WithKnownMissionRow_UpdatesPathMissionAchievements()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out IPlayer player,
                out _,
                out _,
                out var achievementManagerProxy,
                out _);
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementCalls =
                achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation pathMissionCall = achievementCalls
                .Single(c => (AchievementType)c.Arguments[1] == AchievementType.PathMission);
            Assert.Same(player, pathMissionCall.Arguments[0]);
            Assert.Equal(35u, pathMissionCall.Arguments[2]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation pathMissionTypeCall = achievementCalls
                .Single(c => (AchievementType)c.Arguments[1] == AchievementType.PathMissionType);
            Assert.Same(player, pathMissionTypeCall.Arguments[0]);
            Assert.Equal(0x000Fu, pathMissionTypeCall.Arguments[2]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMission_WithSupportedMissionRewards_GrantsUnflaggedRewardRows()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 35u,
                    Item2Id = 9001u,
                    Count = 3u,
                    Spell4Id = 99u,
                    CharacterTitleId = 18u,
                    PathScientistScanBotProfileId = 6u
                },
                new PathRewardEntry
                {
                    Id = 2u,
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 35u,
                    Item2Id = 9002u,
                    PathRewardFlags = 1u
                },
                new PathRewardEntry
                {
                    Id = 3u,
                    PathRewardTypeEnum = 2u,
                    ObjectId = 35u,
                    Item2Id = 9003u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ],
            spell4Entries:
            [
                new Spell4Entry { Id = 99u, Spell4BaseIdBaseSpell = 77u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out _,
                out _,
                out var inventoryProxy);

            ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
            ITitleManager titleManager = RecordingDispatchProxy<ITitleManager>.Create(out RecordingDispatchProxy<ITitleManager> titleManagerProxy);
            IPetCustomisationManager petCustomisationManager = RecordingDispatchProxy<IPetCustomisationManager>.Create(out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy);
            playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
            playerProxy.SetProperty(nameof(IPlayer.TitleManager), titleManager);
            playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreate.Arguments[0]);
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Equal(3u, itemCreate.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreate.Arguments[3]);

            RecordingDispatchProxy<ISpellManager>.Invocation addSpell =
                Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
            Assert.Equal(77u, addSpell.Arguments[0]);

            RecordingDispatchProxy<ITitleManager>.Invocation addTitle =
                Assert.Single(titleManagerProxy.GetInvocations(nameof(ITitleManager.AddTitle)));
            Assert.Equal((ushort)18, addTitle.Arguments[0]);

            RecordingDispatchProxy<IPetCustomisationManager>.Invocation unlockScanBotProfile =
                Assert.Single(petCustomisationManagerProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockScanBotProfile)));
            Assert.Equal(6u, unlockScanBotProfile.Arguments[0]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteActiveMission_WithoutActivatedMission_DoesNotCreateMission()
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

            Assert.False(manager.CompleteActiveMission(35));

            Assert.False(manager.IsMissionComplete(35));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteActiveMission_WithActivatedMission_CompletesMission()
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
                [35] = 0
            });

            Assert.True(manager.CompleteActiveMission(35));

            Assert.True(manager.IsMissionComplete(35));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal(35, advanced.PathMissionId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithActiveVistaAndNode_CompletesMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu,
                    ObjectId = 123u
                }
            ],
            pathExplorerNodes:
            [
                new PathExplorerNodeEntry { Id = 1u, PathExplorerAreaId = 123u }
            ]);

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
                [35] = 0
            });

            Assert.True(manager.CompleteExplorerProgressMission(35, 0u));

            Assert.True(manager.IsMissionComplete(35));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal(35, advanced.PathMissionId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithActiveNonExplorerMission_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x000Fu,
                    ObjectId = 123u
                }
            ],
            pathExplorerNodes:
            [
                new PathExplorerNodeEntry { Id = 1u, PathExplorerAreaId = 123u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.False(manager.CompleteExplorerProgressMission(35, 0u));

            Assert.False(manager.IsMissionComplete(35));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithoutMatchingExplorerNode_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu,
                    ObjectId = 123u
                }
            ],
            pathExplorerNodes:
            [
                new PathExplorerNodeEntry { Id = 1u, PathExplorerAreaId = 456u }
            ]);

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
                [35] = 0
            });

            Assert.False(manager.CompleteExplorerProgressMission(35, 0u));

            Assert.False(manager.IsMissionComplete(35));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithActivePowerMapMission_CompletesMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 40u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x0012u,
                    ObjectId = 77u
                }
            ],
            pathExplorerPowerMaps:
            [
                new PathExplorerPowerMapEntry { Id = 77u }
            ]);

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
                [40] = 0
            });

            Assert.True(manager.CompleteExplorerPowerMapMission(77u));

            Assert.True(manager.IsMissionComplete(40));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal(40, advanced.PathMissionId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithWrongMissionType_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 40u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu,
                    ObjectId = 77u
                }
            ],
            pathExplorerPowerMaps:
            [
                new PathExplorerPowerMapEntry { Id = 77u }
            ]);

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
                [40] = 0
            });

            Assert.False(manager.CompleteExplorerPowerMapMission(77u));

            Assert.False(manager.IsMissionComplete(40));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithoutPowerMapRow_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 40u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x0012u,
                    ObjectId = 77u
                }
            ]);

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
                [40] = 0
            });

            Assert.False(manager.CompleteExplorerPowerMapMission(77u));

            Assert.False(manager.IsMissionComplete(40));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithActiveMissionAndParentMappedZone_CompletesMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 41u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x0010u,
                    ObjectId = 700u
                }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u },
                new WorldZoneEntry { Id = 11u, ParentZoneId = 10u }
            ],
            mapZones:
            [
                new MapZoneEntry { Id = 700u, WorldZoneId = 10u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 11u, ParentZoneId = 10u });
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [41] = 0
            });

            Assert.True(manager.CompleteCurrentExplorerExploreZoneMission());

            Assert.True(manager.IsMissionComplete(41u));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal(41, advanced.PathMissionId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithWorldJoinFallback_CompletesMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 42u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x0010u,
                    ObjectId = 701u
                }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 99u }
            ],
            mapZoneWorldJoins:
            [
                new MapZoneWorldJoinEntry { Id = 1u, WorldId = 51u, MapZoneId = 701u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out _,
                out _,
                out _);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 99u });
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [42] = 0
            });

            Assert.True(manager.CompleteCurrentExplorerExploreZoneMission());

            Assert.True(manager.IsMissionComplete(42u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithWrongMissionType_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 41u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu,
                    ObjectId = 700u
                }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 10u }
            ],
            mapZones:
            [
                new MapZoneEntry { Id = 700u, WorldZoneId = 10u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 10u });
            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [41] = 0
            });

            Assert.False(manager.CompleteCurrentExplorerExploreZoneMission());

            Assert.False(manager.IsMissionComplete(41u));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
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
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer, ObjectId = 1u }
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
    public void CompleteMissionByObjectId_WithMismatchedPathMission_DoesNotCompleteMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Soldier, ObjectId = 1u }
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

            Assert.False(manager.CompleteMissionByObjectId(1u));
            Assert.False(manager.IsMissionComplete(1254u));
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
                new PathMissionEntry { Id = 156u, PathEpisodeId = 8u, PathTypeEnum = (uint)Path.Soldier, ObjectId = 2u }
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
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithCreatureMatch_CompletesAfterRequiredCount()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x0004u,
                    ObjectId = 12u
                }
            ],
            soldierAssassinate:
            [
                new PathSoldierAssassinateEntry { Id = 12u, Creature2Id = 9001u, Count = 2u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);
            manager.ActivateMissions(8, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.ProgressSoldierAssassinateMissionForCreatureKill(9001u, []));

            Assert.False(manager.IsMissionComplete(35u));
            ServerPathMissionUpdate partialUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Single();
            Assert.False(partialUpdate.Mission.Completed);
            Assert.Equal(1u, partialUpdate.Mission.ProgressCount);
            Assert.Equal(1u, partialUpdate.Mission.ProgressData);

            Assert.True(manager.ProgressSoldierAssassinateMissionForCreatureKill(9001u, []));

            Assert.True(manager.IsMissionComplete(35u));
            ServerPathMissionUpdate completeUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Last();
            Assert.True(completeUpdate.Mission.Completed);
            Assert.Equal(2u, completeUpdate.Mission.ProgressCount);
            Assert.Equal(0u, completeUpdate.Mission.ProgressData);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithTargetGroupMatch_CompletesMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 36u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x0004u,
                    ObjectId = 13u
                }
            ],
            soldierAssassinate:
            [
                new PathSoldierAssassinateEntry { Id = 13u, TargetGroupId = 77u, Count = 1u }
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
                [36] = 0
            });

            Assert.True(manager.ProgressSoldierAssassinateMissionForCreatureKill(9002u, [77u]));

            Assert.True(manager.IsMissionComplete(36u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithNonSoldierPlayer_DoesNotProgressMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 37u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x0004u,
                    ObjectId = 14u
                }
            ],
            soldierAssassinate:
            [
                new PathSoldierAssassinateEntry { Id = 14u, Creature2Id = 9003u, Count = 1u }
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
            manager.ActivateMissions(8, new Dictionary<ushort, uint>
            {
                [37] = 0
            });

            Assert.False(manager.ProgressSoldierAssassinateMissionForCreatureKill(9003u, []));

            Assert.False(manager.IsMissionComplete(37u));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithHubMissionCount_CompletesAfterRequiredBuilds()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 650u,
                    PathEpisodeId = 82u,
                    PathTypeEnum = (uint)Path.Settler,
                    PathMissionTypeEnum = 0x0013u,
                    ObjectId = 46u
                }
            ],
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 2u }
            ]);

        try
        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out _);
            manager.ActivateMissions(82, new Dictionary<ushort, uint>
            {
                [650] = 0
            });

            Assert.True(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.False(manager.IsMissionComplete(650u));
            ServerPathMissionUpdate partialUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Single();
            Assert.False(partialUpdate.Mission.Completed);
            Assert.Equal(1u, partialUpdate.Mission.ProgressCount);
            Assert.Equal(1u, partialUpdate.Mission.ProgressData);

            Assert.True(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.True(manager.IsMissionComplete(650u));
            ServerPathMissionUpdate completeUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Last();
            Assert.True(completeUpdate.Mission.Completed);
            Assert.Equal(2u, completeUpdate.Mission.ProgressCount);
            Assert.Equal(0u, completeUpdate.Mission.ProgressData);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithPersistedPartialHubProgress_CompletesAfterReload()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 650u,
                    PathEpisodeId = 82u,
                    PathTypeEnum = (uint)Path.Settler,
                    PathMissionTypeEnum = 0x0013u,
                    ObjectId = 46u
                }
            ],
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 2u }
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
                out _,
                pathMissionModels:
                [
                    new CharacterPathMissionModel
                    {
                        Id            = 42ul,
                        PathMissionId = 650,
                        PathEpisodeId = 82,
                        State         = (byte)PathMissionState.Started,
                        ProgressCount = 1u,
                        ProgressData  = 1u
                    }
                ]);

            Assert.True(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.True(manager.IsMissionComplete(650u));

            using CharacterContext context = CreateCharacterContext();
            manager.Save(context);

            EntityEntry<CharacterPathMissionModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterPathMissionModel>());
            Assert.Equal(EntityState.Modified, entry.State);
            Assert.True(entry.Property(p => p.Completed).IsModified);
            Assert.True(entry.Property(p => p.ProgressCount).IsModified);
            Assert.True(entry.Property(p => p.ProgressData).IsModified);
            Assert.Equal(1, entry.Entity.Completed);
            Assert.Equal(2u, entry.Entity.ProgressCount);
            Assert.Equal(0u, entry.Entity.ProgressData);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithNonSettlerPlayer_DoesNotProgressMission()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 651u,
                    PathEpisodeId = 82u,
                    PathTypeEnum = (uint)Path.Settler,
                    PathMissionTypeEnum = 0x0013u,
                    ObjectId = 46u
                }
            ],
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 1u }
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
            manager.ActivateMissions(82, new Dictionary<ushort, uint>
            {
                [651] = 0
            });

            Assert.False(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.False(manager.IsMissionComplete(651u));
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
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        IEnumerable<CharacterPathMissionModel> pathMissionModels = null)
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
        playerProxy.SetProperty("Faction1", Faction.Exile);

        var manager = new PathManager(player, new CharacterModel
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
            ],
            PathMission = pathMissionModels?.ToList() ?? []
        });
        playerProxy.SetProperty(nameof(IPlayer.PathManager), manager);

        return manager;
    }

    private static CharacterContext CreateCharacterContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
    }

    private static IServiceProvider BuildGameTableProvider(
        IEnumerable<PathLevelEntry> pathLevels,
        IEnumerable<PathRewardEntry> pathRewards,
        IEnumerable<PathMissionEntry> pathMissions = null,
        IEnumerable<PathSoldierTowerDefenseEntry> soldierTowerDefense = null,
        IEnumerable<PathSoldierAssassinateEntry> soldierAssassinate = null,
        IEnumerable<PathSettlerImprovementGroupEntry> settlerImprovementGroups = null,
        IEnumerable<PathSettlerHubEntry> settlerHubs = null,
        IEnumerable<PathExplorerNodeEntry> pathExplorerNodes = null,
        IEnumerable<PathExplorerPowerMapEntry> pathExplorerPowerMaps = null,
        IEnumerable<PathEpisodeEntry> pathEpisodes = null,
        IEnumerable<WorldZoneEntry> worldZones = null,
        IEnumerable<MapZoneEntry> mapZones = null,
        IEnumerable<MapZoneWorldJoinEntry> mapZoneWorldJoins = null,
        IEnumerable<Spell4Entry> spell4Entries = null,
        IEnumerable<PrerequisiteEntry> prerequisites = null,
        IEnumerable<GameFormulaEntry> gameFormulas = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathLevel), CreateGameTable(pathLevels.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathReward), CreateGameTable(pathRewards.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathMission), CreateGameTable((pathMissions ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSoldierTowerDefense), CreateGameTable((soldierTowerDefense ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSoldierAssassinate), CreateGameTable((soldierAssassinate ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSettlerImprovementGroup), CreateGameTable((settlerImprovementGroups ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSettlerHub), CreateGameTable((settlerHubs ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathExplorerNode), CreateGameTable((pathExplorerNodes ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathExplorerPowerMap), CreateGameTable((pathExplorerPowerMaps ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathEpisode), CreateGameTable((pathEpisodes ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), CreateGameTable((worldZones ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZone), CreateGameTable((mapZones ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZoneWorldJoin), CreateGameTable((mapZoneWorldJoins ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable((spell4Entries ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Prerequisite), CreateGameTable((prerequisites ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.GameFormula), CreateGameTable((gameFormulas ?? []).ToArray()));

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(gameTableManager)
            .AddSingleton<IGameTableManager>(gameTableManager);
        services.AddGamePrerequisite();
        return services.BuildServiceProvider();
    }

    private static PrerequisiteEntry CreatePathPrerequisite(uint id, Path path)
    {
        return new PrerequisiteEntry
        {
            Id = id,
            Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.Path, PrerequisiteType.None, PrerequisiteType.None],
            PrerequisiteComparisonId = [PrerequisiteComparison.Equal, default, default],
            ObjectId = [0u, 0u, 0u],
            Value = [(uint)path, 0u, 0u]
        };
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

    private static IBaseMap CreateMap(uint worldId)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = worldId });
        return map;
    }
}
