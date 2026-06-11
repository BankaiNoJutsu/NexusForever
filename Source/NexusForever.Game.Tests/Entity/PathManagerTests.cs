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
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using DatacubeType = NexusForever.Game.Static.Entity.DatacubeType;
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
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class PathManagerTests
{
    private GameTableManager configuredGameTableManager;
    private IPrerequisiteManager configuredPrerequisiteManager;

    [Fact]
    public void AddXp_AwardsInitialPathLevelRewardFromLevelRewardedState()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void AddLevels_AtMaxPathLevelStillAwardsOutstandingCapReward()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void AddLevels_WithMissingTargetPathLevelRow_DoesNotMutateXp()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            []);

        {
            PathManager manager = CreateManager(
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            manager.AddLevels(1u);

            IPathEntry pathEntry = Assert.Single(manager, entry => entry.Path == Path.Soldier);
            Assert.Equal(0u, pathEntry.TotalXp);
            Assert.Equal(1, pathEntry.LevelRewarded);
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        }
    }

    [Fact]
    public void ActivateMissions_SendsEpisodeProgressAndMissionActivate()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
    public void Constructor_WithCompletedScientistScanMission_HydratesScanCredit()
    {
        PathManager manager = CreateManager(
            Path.Scientist,
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
                    PathMissionId = 42,
                    PathEpisodeId = 1,
                    State         = (byte)PathMissionState.Complete,
                    Completed     = 1,
                    ProgressCount = 1
                }
            ]);

        Assert.True(manager.HasScannedScientistCreature(34u));
    }

    [Fact]
    public void MarkScientistCreatureScanned_PersistsViaDatacubeManager()
    {
        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _);

        manager.MarkScientistCreatureScanned(130u);
        Assert.True(manager.HasScannedScientistCreature(130u));

        using CharacterContext context = CreateCharacterContext();
        player.DatacubeManager.Save(context);

        EntityEntry<CharacterDatacubeModel> entry = Assert.Single(
            context.ChangeTracker.Entries<CharacterDatacubeModel>());
        Assert.Equal((byte)DatacubeType.ScientistCreatureScan, entry.Entity.Type);
        Assert.Equal(130, entry.Entity.Datacube);
        Assert.Equal(1u, entry.Entity.Progress);
    }

    [Fact]
    public void MarkScientistCreatureScanned_WithInjectedGameTableManager_PersistsChecklistMask()
    {
        IGameTableManager gameTableManager = CreateScientistCreatureInfoGameTableManager(new PathScientistCreatureInfoEntry
        {
            Id             = 130,
            ChecklistCount = 3u
        });
        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _,
            gameTableManager: gameTableManager);

        manager.MarkScientistCreatureScanned(130u);
        Assert.True(manager.HasScannedScientistCreature(130u));
        Assert.Equal(7u, player.DatacubeManager.GetScientistCreatureScanProgress(130));

        using CharacterContext context = CreateCharacterContext();
        player.DatacubeManager.Save(context);

        EntityEntry<CharacterDatacubeModel> entry = Assert.Single(
            context.ChangeTracker.Entries<CharacterDatacubeModel>());
        Assert.Equal((byte)DatacubeType.ScientistCreatureScan, entry.Entity.Type);
        Assert.Equal(130, entry.Entity.Datacube);
        Assert.Equal(7u, entry.Entity.Progress);
    }

    [Fact]
    public void DatacubeManager_SendInitialPackets_ReplaysVolumeArchivesAndSkipsScientistScanState()
    {
        _ = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            datacubeModels:
            [
                new CharacterDatacubeModel
                {
                    Id       = 42ul,
                    Datacube = 11,
                    Type     = (byte)DatacubeType.Chronicle,
                    Progress = 2u
                },
                new CharacterDatacubeModel
                {
                    Id       = 42ul,
                    Datacube = 12,
                    Type     = (byte)DatacubeType.Journal,
                    Progress = 4u
                },
                new CharacterDatacubeModel
                {
                    Id       = 42ul,
                    Datacube = 130,
                    Type     = (byte)DatacubeType.ScientistCreatureScan,
                    Progress = 1u
                }
            ]);

        player.DatacubeManager.SendInitialPackets();

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<NexusForever.Network.World.Message.Model.ServerDatacubeUpdateList>(
            sessionCall.Arguments[0]);
        Assert.Empty(update.DatacubeData);
        Assert.Equal([11, 12], update.DatacubeVolumeData.Select(d => d.DatacubeId).ToArray());
        Assert.Equal([2u, 4u], update.DatacubeVolumeData.Select(d => d.Progress).ToArray());
    }

    [Fact]
    public void DatacubeManager_Constructor_MergesDuplicatePersistedRows()
    {
        _ = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _,
            datacubeModels:
            [
                new CharacterDatacubeModel
                {
                    Id       = 42ul,
                    Datacube = 11,
                    Type     = (byte)DatacubeType.Datacube,
                    Progress = 1u
                },
                new CharacterDatacubeModel
                {
                    Id       = 42ul,
                    Datacube = 11,
                    Type     = (byte)DatacubeType.Datacube,
                    Progress = 2u
                }
            ]);

        IDatacube datacube = player.DatacubeManager.GetDatacube(11, DatacubeType.Datacube);
        Assert.NotNull(datacube);
        Assert.Equal(3u, datacube.Progress);
    }

    [Fact]
    public void DatacubeManager_AddDatacube_WhenTableMissingRejectsWithoutPacket()
    {
        _ = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            gameTableManager: CreateEmptyGameTableManager());

        Assert.Throws<ArgumentException>(() => player.DatacubeManager.AddDatacube(11, 1u));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void DatacubeManager_AddDatacubeVolume_WhenTableMissingRejectsWithoutPacket()
    {
        _ = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            gameTableManager: CreateEmptyGameTableManager());

        Assert.Throws<ArgumentException>(() => player.DatacubeManager.AddDatacubeVolume(12, 1u));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void CompleteMission_MarksMappedScientistScanCreatureInfo()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [new PathMissionEntry { Id = 42u, PathEpisodeId = 1u }]);

        {
            PathManager manager = CreateManager(
                Path.Scientist,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out _,
                out _,
                out _);

            Assert.True(manager.CompleteMission(42));
            Assert.True(manager.HasScannedScientistCreature(34u));
        }
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
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
    }

    [Fact]
    public void SendInitialPackets_WithNewlyActivatedMission_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
            manager.SendInitialPackets();

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.Equal(4, messages.Count);
            Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Assert.IsType<ServerPathInitialise>(messages[3]);
            Assert.Single(messages.OfType<ServerPathEpisodeProgress>());
            Assert.Single(messages.OfType<ServerPathMissionActivate>());
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedActiveMission_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 9u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Explorer }
            ]);

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

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedActiveMission_SendsPathLogEachCallWithoutReplay()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 9u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Explorer }
            ]);

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
            int firstSendCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            manager.SendInitialPackets();

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.Equal(1, firstSendCount);
            Assert.Equal(2, messages.Count);
            Assert.IsType<ServerPathInitialise>(messages[0]);
            Assert.IsType<ServerPathInitialise>(messages[firstSendCount]);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedMissionForMissingEpisode_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer }
            ]);

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
                        State         = (byte)PathMissionState.Started
                    }
                ]);

            manager.SendInitialPackets();

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedMissionForMismatchedEpisodePath_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 9u, WorldId = 51u, WorldZoneId = 10u, PathTypeEnum = (uint)Path.Soldier }
            ]);

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
                        State         = (byte)PathMissionState.Started
                    }
                ]);

            manager.SendInitialPackets();

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedMissionForDifferentPath_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Soldier,
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
                        State         = (byte)PathMissionState.Started
                    }
                ]);

            manager.SendInitialPackets();

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedMissionForWrongFaction_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 2u }
            ]);

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

            manager.SendInitialPackets();

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
    }

    [Fact]
    public void SendInitialPackets_WithPersistedMissionForUnmetPrerequisite_DoesNotReplayEpisodeState()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 650u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Settler, PathMissionFactionEnum = 0u, PrerequisiteId = 701u }
            ],
            prerequisites:
            [
                CreatePathPrerequisite(701u, Path.Soldier)
            ]);

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

            manager.SendInitialPackets();

            object message = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(message);
        }
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
        BuildGameTableProvider(
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
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithPersistedActiveEpisode_DoesNotReemitActivationPackets()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithPersistedDifferentPathEpisode_AllowsCurrentPathActivation()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 35u, PathEpisodeId = 82u, PathTypeEnum = (uint)Path.Explorer, PathMissionFactionEnum = 0u },
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
                        PathMissionId = 35,
                        PathEpisodeId = 82,
                        State         = (byte)PathMissionState.Started
                    }
                ]);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(51u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 10u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.Equal([650u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Assert.Equal([650u], missionActivate.Missions.Select(m => m.PathMissionId).ToArray());
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithMissionPrerequisites_ActivatesOnlyEligibleMissions()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithoutMatchingEpisode_DoesNotSendPackets()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMission_UpdatesMissionAndAwardsConfiguredPathXp()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 2u, PathXP = 100u }
            ],
            []);

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
    }

    [Fact]
    public void CompleteMission_WithKnownMissionAndNoConfiguredXp_AwardsGameFormulaPathXp()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMission_WithKnownMissionAndNoFormulaXp_AwardsClientFallbackPathXp()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMission_WithMissingPathLevelRows_SkipsXpAndKeepsMissionReward()
    {
        BuildGameTableProvider(
            [],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 35u,
                    Item2Id = 9001u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out _,
                out var inventoryProxy);

            Assert.True(manager.CompleteMission(35));
            Assert.True(manager.IsMissionComplete(35));

            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathUpdateXP>());

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreate.Arguments[0]);
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Equal(1u, itemCreate.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreate.Arguments[3]);
        }
    }

    [Fact]
    public void CompleteMission_WithMissingPathMissionTable_CompletesWithoutMissionMetadata()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 35u,
                    Item2Id = 9001u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(gameTableManager, nameof(GameTableManager.PathMission), null);

            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            Assert.True(manager.CompleteMission(35));
            Assert.True(manager.IsMissionComplete(35));

            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathUpdateXP>());
            Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreate.Arguments[0]);
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Equal(1u, itemCreate.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreate.Arguments[3]);
        }
    }

    [Fact]
    public void CompleteMission_WithMismatchedMissionPathAndNoConfiguredXp_DoesNotAwardFallbackXp()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMission_WithKnownMissionRow_UpdatesPathMissionAchievements()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMission_WithSupportedMissionRewards_GrantsUnflaggedRewardRows()
    {
        BuildGameTableProvider(
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
            ],
            characterTitles:
            [
                new CharacterTitleEntry { Id = 18u }
            ],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 6u }
            ]);

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
    }

    [Fact]
    public void CompleteMission_WithMissingSpellRewardEntry_SkipsSpellRewardWithoutThrowing()
    {
        MissingGameDataDiagnostics.ResetForTests();
        BuildGameTableProvider(
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
                    Spell4Id = 99u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

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
            playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
            AssertSkippedGrantDiagnostic("Spell4.tbl", 99u, "Path spell reward");
        }
            MissingGameDataDiagnostics.ResetForTests();
    }

    [Fact]
    public void CompleteMission_WithMissingSpellRewardTable_SkipsSpellRewardWithoutThrowing()
    {
        MissingGameDataDiagnostics.ResetForTests();
        BuildGameTableProvider(
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
                    Spell4Id = 99u
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
            includeSpell4Table: false);

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
            playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
            AssertSkippedGrantDiagnostic("Spell4.tbl", 99u, "Path spell reward");
        }
            MissingGameDataDiagnostics.ResetForTests();
    }

    [Fact]
    public void CompleteMission_WithMissingTitleRewardEntry_SkipsTitleRewardWithoutThrowing()
    {
        MissingGameDataDiagnostics.ResetForTests();
        BuildGameTableProvider(
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
                    CharacterTitleId = 18u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

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

            ITitleManager titleManager = RecordingDispatchProxy<ITitleManager>.Create(out RecordingDispatchProxy<ITitleManager> titleManagerProxy);
            playerProxy.SetProperty(nameof(IPlayer.TitleManager), titleManager);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Empty(titleManagerProxy.GetInvocations(nameof(ITitleManager.AddTitle)));
            AssertSkippedGrantDiagnostic("CharacterTitle.tbl", 18u, "Path title reward");
        }
            MissingGameDataDiagnostics.ResetForTests();
    }

    [Fact]
    public void CompleteMission_WithMissingScanBotProfileRewardTable_SkipsScanBotProfileRewardWithoutThrowing()
    {
        MissingGameDataDiagnostics.ResetForTests();
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 35u,
                    Item2Id = 9001u,
                    PathScientistScanBotProfileId = 6u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x002Au
                }
            ],
            includeScanBotProfileTable: false);

        {
            PathManager manager = CreateManager(
                Path.Scientist,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out _,
                out _,
                out var inventoryProxy);

            IPetCustomisationManager petCustomisationManager = RecordingDispatchProxy<IPetCustomisationManager>.Create(out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy);
            playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Empty(petCustomisationManagerProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockScanBotProfile)));
            AssertSkippedGrantDiagnostic("PathScientistScanBotProfile.tbl", 6u, "Path scanbot profile reward");
        }
            MissingGameDataDiagnostics.ResetForTests();
    }

    [Fact]
    public void CompleteMission_WithZeroCountMissionItemReward_GrantsSingleItem()
    {
        BuildGameTableProvider(
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
                    Count = 0u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 35u,
                    PathTypeEnum = (uint)Path.Explorer,
                    PathMissionTypeEnum = 0x000Fu
                }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Explorer,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out _,
                out _,
                out _,
                out var inventoryProxy);

            manager.ActivateMissions(9, new Dictionary<ushort, uint>
            {
                [35] = 0
            });

            Assert.True(manager.CompleteMission(35));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreate.Arguments[0]);
            Assert.Equal(9001u, itemCreate.Arguments[1]);
            Assert.Equal(1u, itemCreate.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreate.Arguments[3]);
        }
    }

    [Fact]
    public void CompleteActiveMission_WithoutActivatedMission_DoesNotCreateMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
    }

    [Fact]
    public void CompleteActiveMission_WithActivatedMission_CompletesMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithActiveVistaAndNode_CompletesMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithActiveNonExplorerMission_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteExplorerProgressMission_WithoutMatchingExplorerNode_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("explorer-node")]
    public void CompleteExplorerProgressMission_WithMissingStaticTable_DoesNotCompleteMission(string missingTable)
    {
        BuildGameTableProvider(
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

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(
                gameTableManager,
                missingTable == "path-mission" ? nameof(GameTableManager.PathMission) : nameof(GameTableManager.PathExplorerNode),
                null);

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
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithActivePowerMapMission_CompletesMission()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("power-map")]
    public void CompleteExplorerPowerMapMission_WithMissingStaticTable_DoesNotCompleteMission(string missingTable)
    {
        BuildGameTableProvider(
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

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(
                gameTableManager,
                missingTable == "path-mission" ? nameof(GameTableManager.PathMission) : nameof(GameTableManager.PathExplorerPowerMap),
                null);

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
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithWrongMissionType_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteExplorerPowerMapMission_WithoutPowerMapRow_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithActiveMissionAndParentMappedZone_CompletesMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithWorldJoinFallback_CompletesMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithWrongMissionType_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteCurrentExplorerExploreZoneMission_WithMissingPathMissionTable_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
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
                new WorldZoneEntry { Id = 10u }
            ],
            mapZones:
            [
                new MapZoneEntry { Id = 700u, WorldZoneId = 10u }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(gameTableManager, nameof(GameTableManager.PathMission), null);

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
    }

    [Fact]
    public void CompleteMissionByObjectId_CompletesMatchingActiveMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer, ObjectId = 1u }
            ]);

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
    }

    [Fact]
    public void CompleteMissionByObjectId_WithMismatchedPathMission_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Soldier, ObjectId = 1u }
            ]);

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
    }

    [Fact]
    public void CompleteMissionByObjectId_WithMissingPathMissionTable_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry { Id = 1254u, PathEpisodeId = 9u, PathTypeEnum = (uint)Path.Explorer, ObjectId = 1u }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(gameTableManager, nameof(GameTableManager.PathMission), null);

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
    }

    [Fact]
    public void CompleteMissionBySoldierTowerDefenseId_CompletesMatchingEventMission()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("tower-defense")]
    public void CompleteMissionBySoldierTowerDefenseId_WithMissingStaticTable_DoesNotCompleteMission(string missingTable)
    {
        BuildGameTableProvider(
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

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(
                gameTableManager,
                missingTable == "path-mission" ? nameof(GameTableManager.PathMission) : nameof(GameTableManager.PathSoldierTowerDefense),
                null);

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

            Assert.False(manager.CompleteMissionBySoldierTowerDefenseId(2u));
            Assert.False(manager.IsMissionComplete(156u));
        }
    }

    [Fact]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithCreatureMatch_CompletesAfterRequiredCount()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithTargetGroupMatch_CompletesMission()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithNonSoldierPlayer_DoesNotProgressMission()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("soldier-assassinate")]
    public void ProgressSoldierAssassinateMissionForCreatureKill_WithMissingStaticTable_DoesNotProgressMission(string missingTable)
    {
        BuildGameTableProvider(
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
                new PathSoldierAssassinateEntry { Id = 12u, Creature2Id = 9001u, Count = 1u }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(
                gameTableManager,
                missingTable == "path-mission" ? nameof(GameTableManager.PathMission) : nameof(GameTableManager.PathSoldierAssassinate),
                null);

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

            Assert.False(manager.ProgressSoldierAssassinateMissionForCreatureKill(9001u, []));

            Assert.False(manager.IsMissionComplete(35u));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>());
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithHubMissionCount_CompletesAfterRequiredBuilds()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("improvement-group")]
    [InlineData("hub")]
    public void CompleteMissionBySettlerImprovementGroupId_WithMissingStaticTable_DoesNotProgressMission(string missingTable)
    {
        BuildGameTableProvider(
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
                new PathSettlerHubEntry { Id = 46u, MissionCount = 1u }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            string propertyName = missingTable switch
            {
                "path-mission"      => nameof(GameTableManager.PathMission),
                "improvement-group" => nameof(GameTableManager.PathSettlerImprovementGroup),
                _                   => nameof(GameTableManager.PathSettlerHub)
            };
            SetAutoProperty(gameTableManager, propertyName, null);

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
            int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            Assert.False(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.False(manager.IsMissionComplete(650u));
            Assert.Equal(beforeCount, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count);
        }
    }

    [Fact]
    public void GetSettlerHubBuildProgressPercent_UsesHubMissionProgressAndStoredTier()
    {
        BuildGameTableProvider(
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
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u, MaxBundleCount = 4u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 2u }
            ]);

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
            manager.ActivateMissions(82, new Dictionary<ushort, uint> { [650] = 0 });
            manager.CompleteMissionBySettlerImprovementGroupId(11u);

            Assert.Equal(50u, manager.GetSettlerHubBuildProgressPercent(46u));

            manager.ApplySettlerImprovementGroupStatus(11u, tier: 4, bundleCount: 1u);
            Assert.Equal(100u, manager.GetSettlerHubBuildProgressPercent(11u));
        }
    }

    [Fact]
    public void GetSettlerHubContributionProgressPercent_UsesBundleCountOverMaxBundles()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [],
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u, MaxBundleCount = 4u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 1u }
            ]);

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

            manager.ApplySettlerImprovementGroupStatus(11u, tier: 1, bundleCount: 2u);

            Assert.Equal(50u, manager.GetSettlerHubContributionProgressPercent(11u));
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithPersistedPartialHubProgress_CompletesAfterReload()
    {
        BuildGameTableProvider(
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
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithNonSettlerPlayer_DoesNotProgressMission()
    {
        BuildGameTableProvider(
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
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(99u)]
    public void CompleteMissionBySettlerImprovementGroupId_WithMissingGroup_DoesNotProgressMission(uint pathSettlerImprovementGroupId)
    {
        BuildGameTableProvider(
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
            ]);

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
            int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            Assert.False(manager.CompleteMissionBySettlerImprovementGroupId(pathSettlerImprovementGroupId));

            Assert.False(manager.IsMissionComplete(650u));
            Assert.Equal(beforeCount, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count);
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithMissingHub_DoesNotProgressMission()
    {
        BuildGameTableProvider(
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
            ]);

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
            int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            Assert.False(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.False(manager.IsMissionComplete(650u));
            Assert.Equal(beforeCount, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count);
        }
    }

    [Fact]
    public void CompleteMissionBySettlerImprovementGroupId_WithDifferentHub_DoesNotProgressMission()
    {
        BuildGameTableProvider(
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
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 47u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 47u, MissionCount = 1u }
            ]);

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
            int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            Assert.False(manager.CompleteMissionBySettlerImprovementGroupId(11u));

            Assert.False(manager.IsMissionComplete(650u));
            Assert.Equal(beforeCount, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count);
        }
    }

    private PathManager CreateManager(
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

    private PathManager CreateManager(
        Path path,
        uint totalXp,
        byte levelRewarded,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        IEnumerable<CharacterPathMissionModel> pathMissionModels = null,
        IEnumerable<CharacterDatacubeModel> datacubeModels = null,
        IGameTableManager gameTableManager = null)
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

        var characterModel = new CharacterModel
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
            PathMission = pathMissionModels?.ToList() ?? [],
            Datacube    = datacubeModels?.ToList() ?? []
        };

        gameTableManager ??= configuredGameTableManager;
        IPrerequisiteManager prerequisiteManager = configuredPrerequisiteManager;

        var datacubeManager = new DatacubeManager(player, characterModel, gameTableManager);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        var manager = new PathManager(player, characterModel, prerequisiteManager, gameTableManager);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), manager);

        return manager;
    }

    private static void AssertSkippedGrantDiagnostic(string tableName, uint staticId, string detail)
    {
        MissingGameDataDiagnostic diagnostic = Assert.Single(MissingGameDataDiagnostics.GetSnapshot(), d =>
            d.Kind == MissingGameDataDiagnosticKind.SkippedGrant
            && d.Severity == MissingGameDataSeverity.PlayerImpacting
            && d.TableName == tableName
            && d.StaticId == staticId.ToString()
            && d.Detail.Contains(detail));

        Assert.Equal(1, diagnostic.Count);
    }

    private static IGameTableManager CreateScientistCreatureInfoGameTableManager(params PathScientistCreatureInfoEntry[] entries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.PathScientistCreatureInfo), CreateGameTable(entries));
        return gameTableManager;
    }

    private static IGameTableManager CreateEmptyGameTableManager()
    {
        return RecordingDispatchProxy<IGameTableManager>.Create(out _);
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

    private void BuildGameTableProvider(
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
        IEnumerable<CharacterTitleEntry> characterTitles = null,
        IEnumerable<PathScientistScanBotProfileEntry> scanBotProfiles = null,
        IEnumerable<PrerequisiteEntry> prerequisites = null,
        IEnumerable<GameFormulaEntry> gameFormulas = null,
        bool includeSpell4Table = true,
        bool includeScanBotProfileTable = true)
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
        if (includeSpell4Table)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable((spell4Entries ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.CharacterTitle), CreateGameTable((characterTitles ?? []).ToArray()));
        if (includeScanBotProfileTable)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.PathScientistScanBotProfile), CreateGameTable((scanBotProfiles ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Prerequisite), CreateGameTable((prerequisites ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.GameFormula), CreateGameTable((gameFormulas ?? []).ToArray()));

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(gameTableManager)
            .AddSingleton<IGameTableManager>(gameTableManager);
        services.AddGamePrerequisite();
        IServiceProvider provider = services.BuildServiceProvider();
        configuredGameTableManager = gameTableManager;
        configuredPrerequisiteManager = provider.GetRequiredService<IPrerequisiteManager>();
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
