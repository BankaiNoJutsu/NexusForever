using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
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

    public static IEnumerable<object[]> NorthernWildsDataMappingEpisodeCases()
    {
        yield return [Path.Soldier, (ushort)8, Array.Empty<ushort>()];
        yield return [Path.Explorer, (ushort)9, new ushort[] { 35, 36, 158, 1254 }];
        yield return [Path.Scientist, (ushort)28, new ushort[] { 42, 160, 648 }];
        yield return [Path.Settler, (ushort)82, new ushort[] { 650, 651, 652 }];
    }

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
    public void ActivateMissions_WithAdditionalMissionInSameEpisode_SendsNewMissionActivation()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            []);

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
                [156] = 0
            });
            int initialMessageCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            manager.ActivateMissions(8, new Dictionary<ushort, uint>
            {
                [33] = 0
            });

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(initialMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.DoesNotContain(messages, message => message is ServerPathSetCurrentEpisode);
            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[0]);
            Assert.Equal(8u, episodeProgress.EpisodeId);
            Assert.Equal([33u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());

            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[1]);
            Assert.Equal([33u], missionActivate.Missions.Select(m => m.PathMissionId).ToArray());
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
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 42u,
                    PathEpisodeId = 1u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0002u,
                    ObjectId = 34u
                }
            ],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [
                new PathScientistCreatureInfoEntry { Id = 34u }
            ]);

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
    public void TryDeployScientistScanbot_WithUnlockedProfileQueuesScannerAndEmitsStateAfterMapAdd()
    {
        BuildScanbotGameTableProvider();
        IScannerUnitEntity scanbot = CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(playerProxy, scanbot, profileUnlocked: true, out RecordingDispatchProxy<IBaseMap> mapProxy);

        Assert.True(manager.TryDeployScientistScanbot(77u));

        Assert.Single(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));

        RecordingDispatchProxy<IScannerUnitEntity>.Invocation initialise =
            Assert.Single(scanbotProxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(12664u, initialise.Arguments[0]);
        Assert.Equal((uint?)500u, scanbot.SummonerGuid);
        Assert.Equal(Faction.Exile, scanbot.Faction1);
        Assert.Equal(Faction.Exile, scanbot.Faction2);

        RecordingDispatchProxy<IBaseMap>.Invocation enqueueAdd =
            Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Same(scanbot, enqueueAdd.Arguments[0]);
        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(426u, position.Info.Entry.Id);

        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));

        scanbotProxy.SetProperty(nameof(IGridEntity.Guid), 9001u);
        Assert.True(manager.OnScientistScanbotSummoned(scanbot));

        ServerPathScientistScanbotState state =
            Assert.Single(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));
        Assert.Equal(9001u, state.ScanbotUnitId);
        Assert.Equal(0u, state.ScanbotCooldownMS);
    }

    [Fact]
    public void TryDeployScientistScanbot_WithEarnedProfileMissingUnlock_RecoversAndQueuesScanner()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 66u,
                    PathRewardTypeEnum = PathRewardGrant.LevelRewardType,
                    ObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Scientist, 1u),
                    PathScientistScanBotProfileId = 77u
                }
            ],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 77u, Creature2Id = 12664u }
            ],
            creature2Entries:
            [
                new Creature2Entry { Id = 12664u }
            ]);
        IScannerUnitEntity scanbot = CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out _);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 0,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(
            playerProxy,
            scanbot,
            profileUnlocked: false,
            out RecordingDispatchProxy<IBaseMap> mapProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy);

        Assert.True(manager.TryDeployScientistScanbot(77u));

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation unlock =
            Assert.Single(petCustomisationManagerProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockScanBotProfile)));
        Assert.Equal(77u, unlock.Arguments[0]);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));

        scanbotProxy.SetProperty(nameof(IGridEntity.Guid), 9001u);
        Assert.True(manager.OnScientistScanbotSummoned(scanbot));
        Assert.Equal(9001u, Assert.Single(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy)).ScanbotUnitId);
    }

    [Fact]
    public void TryDeployScientistScanbot_WithDefaultProfileMissingUnlock_RecoversAndQueuesScanner()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 1u, Creature2Id = 12664u }
            ],
            creature2Entries:
            [
                new Creature2Entry { Id = 12664u }
            ]);
        IScannerUnitEntity scanbot = CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out _);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 0,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(
            playerProxy,
            scanbot,
            profileUnlocked: false,
            out RecordingDispatchProxy<IBaseMap> mapProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy,
            scanBotProfileId: 1u);

        Assert.True(manager.TryDeployScientistScanbot(1u));

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation unlock =
            Assert.Single(petCustomisationManagerProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockScanBotProfile)));
        Assert.Equal(1u, unlock.Arguments[0]);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));

        scanbotProxy.SetProperty(nameof(IGridEntity.Guid), 9001u);
        Assert.True(manager.OnScientistScanbotSummoned(scanbot));
        Assert.Equal(9001u, Assert.Single(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy)).ScanbotUnitId);
    }

    [Fact]
    public void TryDeployScientistScanbot_WithZeroProfileUsesDefaultProfile()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 1u, Creature2Id = 12664u }
            ],
            creature2Entries:
            [
                new Creature2Entry { Id = 12664u }
            ]);
        IScannerUnitEntity scanbot = CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out _);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 0,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(
            playerProxy,
            scanbot,
            profileUnlocked: false,
            out RecordingDispatchProxy<IBaseMap> mapProxy,
            out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy,
            scanBotProfileId: 1u);

        Assert.True(manager.TryDeployScientistScanbot(0u));

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation unlock =
            Assert.Single(petCustomisationManagerProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockScanBotProfile)));
        Assert.Equal(1u, unlock.Arguments[0]);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));

        scanbotProxy.SetProperty(nameof(IGridEntity.Guid), 9001u);
        Assert.True(manager.OnScientistScanbotSummoned(scanbot));
        Assert.Equal(9001u, Assert.Single(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy)).ScanbotUnitId);
    }

    [Fact]
    public void TryDeployScientistScanbot_WithLockedProfileFailsClosed()
    {
        BuildScanbotGameTableProvider();
        IScannerUnitEntity scanbot = CreateScanbot(out _);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(playerProxy, scanbot, profileUnlocked: false, out RecordingDispatchProxy<IBaseMap> mapProxy);

        Assert.False(manager.TryDeployScientistScanbot(77u));

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));
    }

    [Fact]
    public void TryDeployScientistScanbot_WithMissingCreatureRowFailsClosed()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 77u, Creature2Id = 12664u }
            ]);
        IScannerUnitEntity scanbot = CreateScanbot(out _);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(playerProxy, scanbot, profileUnlocked: true, out RecordingDispatchProxy<IBaseMap> mapProxy);

        Assert.False(manager.TryDeployScientistScanbot(77u));

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));
    }

    [Fact]
    public void TryDeployScientistScanbot_WithNonScientistPathFailsClosed()
    {
        BuildScanbotGameTableProvider();
        IScannerUnitEntity scanbot = CreateScanbot(out _);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        PathManager manager = CreateManager(
            Path.Explorer,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(playerProxy, scanbot, profileUnlocked: true, out RecordingDispatchProxy<IBaseMap> mapProxy);

        Assert.False(manager.TryDeployScientistScanbot(77u));

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy));
    }

    [Fact]
    public void DismissScientistScanbot_WithActiveScannerQueuesRemoveAndSendsDespawnState()
    {
        BuildScanbotGameTableProvider();
        IScannerUnitEntity scanbot = CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy);
        IEntityFactory entityFactory = CreateEntityFactory(scanbot, out _);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            entityFactory: entityFactory);
        ConfigureScanbotPlayer(playerProxy, scanbot, profileUnlocked: true, out _);

        Assert.True(manager.TryDeployScientistScanbot(77u));
        scanbotProxy.SetProperty(nameof(IGridEntity.Guid), 9001u);
        scanbotProxy.SetProperty(nameof(IGridEntity.InWorld), true);
        Assert.True(manager.OnScientistScanbotSummoned(scanbot));

        Assert.True(manager.DismissScientistScanbot());

        RecordingDispatchProxy<IScannerUnitEntity>.Invocation remove =
            Assert.Single(scanbotProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Empty(remove.Arguments);

        IReadOnlyList<ServerPathScientistScanbotState> states =
            GetEncryptedMessages<ServerPathScientistScanbotState>(sessionProxy);
        Assert.Equal(2, states.Count);
        Assert.Equal(9001u, states[0].ScanbotUnitId);
        Assert.Equal(0u, states[1].ScanbotUnitId);
        Assert.Equal(0u, states[1].ScanbotCooldownMS);

        Assert.False(manager.TryDeployScientistScanbot(77u));
        Assert.True(manager.OnScientistScanbotUnsummoned(scanbot));
    }

    [Fact]
    public void ProgressScientistCreatureScanMission_WithChecklistCount_CompletesAfterRequiredScans()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 160u,
                    PathEpisodeId = 28u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0002u,
                    ObjectId = 130u
                }
            ],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [
                new PathScientistCreatureInfoEntry { Id = 130u, ChecklistCount = 3u }
            ]);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out IPlayer player,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _);
        manager.ActivateMissions(28, new Dictionary<ushort, uint>
        {
            [160] = 0u
        });

        Assert.True(manager.ProgressScientistCreatureScanMission(130u));
        Assert.False(manager.IsMissionComplete(160u));
        Assert.True(manager.ProgressScientistCreatureScanMission(130u));
        Assert.False(manager.IsMissionComplete(160u));
        Assert.True(manager.ProgressScientistCreatureScanMission(130u));

        Assert.True(manager.IsMissionComplete(160u));
        Assert.True(manager.HasScannedScientistCreature(130u));
        Assert.Equal(7u, player.DatacubeManager.GetScientistCreatureScanProgress(130));

        IReadOnlyList<ServerPathMissionUpdate> missionUpdates = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerPathMissionUpdate>()
            .ToList();

        Assert.Equal(3, missionUpdates.Count);
        Assert.False(missionUpdates[0].Mission.Completed);
        Assert.Equal(1u, missionUpdates[0].Mission.ProgressCount);
        Assert.Equal(1u, missionUpdates[0].Mission.ProgressData);
        Assert.False(missionUpdates[1].Mission.Completed);
        Assert.Equal(2u, missionUpdates[1].Mission.ProgressCount);
        Assert.Equal(1u, missionUpdates[1].Mission.ProgressData);
        Assert.True(missionUpdates[2].Mission.Completed);
        Assert.Equal(3u, missionUpdates[2].Mission.ProgressCount);
        Assert.Equal(0u, missionUpdates[2].Mission.ProgressData);
    }

    [Fact]
    public void CompleteCurrentScientistDatacubeDiscoveryMission_WithMatchingZone_CompletesActiveMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 1877u,
                    PathEpisodeId = 16u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0018u,
                    ObjectId = 3u
                }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 483u },
                new WorldZoneEntry { Id = 484u, ParentZoneId = 483u }
            ],
            pathScientistDatacubeDiscoveries:
            [
                new PathScientistDatacubeDiscoveryEntry { Id = 3u, WorldZoneId = 483u }
            ]);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out var playerProxy,
            out var sessionProxy,
            out _,
            out _);
        playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 484u, ParentZoneId = 483u });
        manager.ActivateMissions(16, new Dictionary<ushort, uint>
        {
            [1877] = 0u
        });

        Assert.True(manager.CompleteCurrentScientistDatacubeDiscoveryMission());

        Assert.True(manager.IsMissionComplete(1877u));
        ServerPathMissionAdvanced advanced = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerPathMissionAdvanced>()
            .Single();
        Assert.Equal(1877u, advanced.PathMissionId);
    }

    [Fact]
    public void CompleteCurrentScientistDatacubeDiscoveryMission_WithMismatchedZone_DoesNotCompleteMission()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 1877u,
                    PathEpisodeId = 16u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0018u,
                    ObjectId = 3u
                }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 483u },
                new WorldZoneEntry { Id = 999u }
            ],
            pathScientistDatacubeDiscoveries:
            [
                new PathScientistDatacubeDiscoveryEntry { Id = 3u, WorldZoneId = 483u }
            ]);

        PathManager manager = CreateManager(
            Path.Scientist,
            totalXp: 0u,
            levelRewarded: 1,
            out _,
            out var playerProxy,
            out var sessionProxy,
            out _,
            out _);
        playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 999u });
        manager.ActivateMissions(16, new Dictionary<ushort, uint>
        {
            [1877] = 0u
        });

        Assert.False(manager.CompleteCurrentScientistDatacubeDiscoveryMission());

        Assert.False(manager.IsMissionComplete(1877u));
        Assert.Empty(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerPathMissionAdvanced>());
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
            [
                new PathMissionEntry
                {
                    Id = 42u,
                    PathEpisodeId = 1u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0002u,
                    ObjectId = 34u
                }
            ],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [
                new PathScientistCreatureInfoEntry { Id = 34u }
            ]);

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
    public void CompleteMission_WithMissingScientistCreatureInfoRow_DoesNotMarkScan()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 42u,
                    PathEpisodeId = 1u,
                    PathTypeEnum = (uint)Path.Scientist,
                    PathMissionTypeEnum = 0x0002u,
                    ObjectId = 34u
                }
            ],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []);

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
            Assert.False(manager.HasScannedScientistCreature(34u));
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
    public void SendInitialPackets_WithNewlyActivatedMissionWithoutCurrentZone_DoesNotReplayEpisodeState()
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
    public void SendInitialPackets_WithPersistedActiveMissionWithoutCurrentZone_DoesNotReplayEpisodeState()
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
    public void SendInitialPackets_WithCurrentZoneSoldierHoldoutBeforeInitialise_ReplaysHoldoutMissionAfterPathLog()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            int preInitialiseMessageCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            manager.SendInitialPackets();

            IReadOnlyList<object> replayMessages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(preInitialiseMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.Equal(3, replayMessages.Count);
            Assert.IsType<ServerPathInitialise>(replayMessages[0]);
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(replayMessages[1]);
            Assert.Equal(8u, currentEpisode.PathEpisodeId);
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(replayMessages[2]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(replayMessages, message => message is ServerPathEpisodeProgress);

            int firstInitialiseMessageCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;
            manager.SendInitialPackets();

            object secondInitialiseMessage = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(firstInitialiseMessageCount)
                .Select(i => i.Arguments[0]));
            Assert.IsType<ServerPathInitialise>(secondInitialiseMessage);
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WhilePlayerLoading_DoesNotSendMissionActivation()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });
            playerProxy.SetProperty(nameof(IPlayer.IsLoading), true);

            Assert.False(manager.TryActivateCurrentZoneEpisode());
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
    }

    [Fact]
    public void SendInitialPackets_WhilePlayerLoading_ReplaysSoldierHoldoutEpisodeAfterPathLog()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });
            playerProxy.SetProperty(nameof(IPlayer.IsLoading), true);

            manager.SendInitialPackets();

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.Equal(3, messages.Count);
            Assert.IsType<ServerPathInitialise>(messages[0]);
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[1]);
            Assert.Equal(8u, currentEpisode.PathEpisodeId);
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(messages, message => message is ServerPathEpisodeProgress);
        }
    }

    [Fact]
    public void UnlockPath_WithCurrentZoneSoldierHoldoutBeforePathLog_ReplaysHoldoutMissionAfterPathLog()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            int preUnlockMessageCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;

            manager.UnlockPath(Path.Settler);

            IReadOnlyList<object> replayMessages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(preUnlockMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.Equal(4, replayMessages.Count);
            Assert.IsType<ServerPathUnlockResult>(replayMessages[0]);
            Assert.IsType<ServerPathInitialise>(replayMessages[1]);

            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(replayMessages[2]);
            Assert.Equal(8u, currentEpisode.PathEpisodeId);
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(replayMessages[3]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(replayMessages, message => message is ServerPathEpisodeProgress);
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
    public void TryActivateCurrentZoneEpisode_WithPersistedActiveEpisode_ReemitsActivationPacketsOnce()
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
            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(82u, currentEpisode.PathEpisodeId);

            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.Equal(82u, episodeProgress.EpisodeId);
            Assert.Equal([650u], episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());

            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(650u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.Equal(messages.Count, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count);
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

    [Theory]
    [MemberData(nameof(NorthernWildsDataMappingEpisodeCases))]
    public void TryActivateCurrentZoneEpisode_WithNorthernWildsRuntimeZoneBridge_SetsCurrentClientPathEpisode(
        Path path,
        ushort expectedEpisodeId,
        ushort[] expectedMissionIds)
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)path, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
            ]);

        {
            PathManager manager = CreateManager(
                path,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(expectedEpisodeId, currentEpisode.PathEpisodeId);

            if (expectedMissionIds.Length == 0)
            {
                Assert.Single(messages);
                return;
            }

            ServerPathEpisodeProgress episodeProgress = Assert.IsType<ServerPathEpisodeProgress>(messages[1]);
            Assert.Equal(expectedEpisodeId, episodeProgress.EpisodeId);
            Assert.Equal(expectedMissionIds.Select(m => (uint)m).ToArray(), episodeProgress.Missions.Select(m => m.PathMissionId).ToArray());
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[2]);
            Assert.Equal(expectedMissionIds.Select(m => (uint)m).ToArray(), missionActivate.Missions.Select(m => m.PathMissionId).ToArray());
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithNorthernWildsCurrentEpisodeThenYetiMission_DoesNotStartHoldoutMission()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            IReadOnlyList<object> initialMessages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(Assert.Single(initialMessages));
            Assert.Equal(8u, currentEpisode.PathEpisodeId);

            int initialMessageCount = initialMessages.Count;
            Assert.True(manager.TryActivateCurrentZoneEpisode(651u));

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(initialMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(Assert.Single(messages));
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.True(manager.IsMissionActiveByObjectId(2u));
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithPersistedNorthernWildsYetiMissionAfterCurrentEpisode_DoesNotReplayHoldoutProgress()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
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
                        PathMissionId = 156,
                        PathEpisodeId = 8,
                        State         = (byte)PathMissionState.Started
                    }
                ]);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            IReadOnlyList<object> initialMessages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(Assert.Single(initialMessages));
            Assert.Equal(8u, currentEpisode.PathEpisodeId);

            int initialMessageCount = initialMessages.Count;
            Assert.True(manager.TryActivateCurrentZoneEpisode(651u));

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(initialMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();

            Assert.True(manager.IsMissionActiveByObjectId(2u));
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(Assert.Single(messages));
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(messages, message => message is ServerPathEpisodeProgress);
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithNorthernWildsYetiMissionZone_SetsEpisodeWithoutStartingHoldout()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();
            Assert.Equal(2, messages.Count);
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(8u, currentEpisode.PathEpisodeId);
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[1]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(messages, message => message is ServerPathEpisodeProgress);
            Assert.True(manager.IsMissionActiveByObjectId(2u));
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithNorthernWildsLandingSiteZoneBridge_SetsEpisodeWithoutStartingHoldout()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 647u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();
            Assert.Equal(2, messages.Count);
            ServerPathSetCurrentEpisode currentEpisode = Assert.IsType<ServerPathSetCurrentEpisode>(messages[0]);
            Assert.Equal(8u, currentEpisode.PathEpisodeId);
            ServerPathMissionActivate missionActivate = Assert.IsType<ServerPathMissionActivate>(messages[1]);
            Mission mission = Assert.Single(missionActivate.Missions);
            Assert.Equal(156u, mission.PathMissionId);
            Assert.Equal(PathMissionState.Started, mission.State);
            Assert.DoesNotContain(messages, message => message is ServerPathEpisodeProgress);
            Assert.True(manager.IsMissionActiveByObjectId(2u));
        }
    }

    [Fact]
    public void TryActivateCurrentZoneEpisode_WithNorthernWildsRuntimeZoneBridgeMissingClientEpisode_DoesNotActivate()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries()
                .Where(m => m.PathEpisodeId == 28u)
                .ToArray(),
            pathEpisodes: [],
            worldZones:
            [
                new WorldZoneEntry { Id = 1u }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Scientist,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.False(manager.TryActivateCurrentZoneEpisode());
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
    }

    [Theory]
    [InlineData(158u, 26u)]
    [InlineData(35u, 37u)]
    [InlineData(36u, 38u)]
    public void NorthernWildsExplorerNodeMissions_WithRuntimeZoneBridgeAndExplorerNode_CompleteProgress(
        uint pathMissionId,
        uint pathExplorerAreaId)
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathExplorerNodes:
            [
                new PathExplorerNodeEntry { Id = 1u, PathExplorerAreaId = 26u },
                new PathExplorerNodeEntry { Id = 2u, PathExplorerAreaId = 37u },
                new PathExplorerNodeEntry { Id = 3u, PathExplorerAreaId = 38u }
            ],
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
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
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.Contains(configuredGameTableManager.PathExplorerNode.Entries, n => n.PathExplorerAreaId == pathExplorerAreaId);
            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.True(manager.CompleteExplorerProgressMission((ushort)pathMissionId, 0u));

            Assert.True(manager.IsMissionComplete(pathMissionId));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal((ushort)pathMissionId, advanced.PathMissionId);
        }
    }

    [Fact]
    public void NorthernWildsExplorerCartography_WithRuntimeZoneBridgeAndFullyExploredMap_CompletesSurvey()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Explorer, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
            ],
            mapZones:
            [
                new MapZoneEntry { Id = 1u, WorldZoneId = 35u }
            ],
            mapZoneWorldJoins:
            [
                new MapZoneWorldJoinEntry { Id = 1u, WorldId = 426u, MapZoneId = 1u }
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
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(100));

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.True(manager.CompleteCurrentExplorerExploreZoneMission());

            Assert.True(manager.IsMissionComplete(1254u));
            ServerPathMissionAdvanced advanced = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionAdvanced>()
                .Single();
            Assert.Equal((ushort)1254, advanced.PathMissionId);
        }
    }

    [Fact]
    public void NorthernWildsSoldierMission_WithRuntimeZoneBridgeAndTowerDefenseRow_CompletesProgress()
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            soldierTowerDefense:
            [
                new PathSoldierTowerDefenseEntry { Id = 2u, PathSoldierEventId = 2u }
            ],
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations());

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 651u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.True(manager.IsMissionActiveByObjectId(2u));

            int discoveryMessageCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;
            Assert.True(manager.TryActivateSoldierMissionByEventId(2u));
            IReadOnlyList<object> activationMessages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(discoveryMessageCount)
                .Select(i => i.Arguments[0])
                .ToList();
            Assert.Empty(activationMessages);

            Assert.True(manager.CompleteMissionBySoldierTowerDefenseId(2u));

            Assert.True(manager.IsMissionComplete(156u));
        }
    }

    [Theory]
    [InlineData(650u, 46u, 11u, 4u)]
    [InlineData(652u, 48u, 14u, 4u)]
    [InlineData(651u, 47u, 10u, 1u)]
    public void NorthernWildsSettlerMission_WithRuntimeZoneBridgeAndHubRows_CompletesAfterVideoBackedBuildCount(
        uint pathMissionId,
        uint pathSettlerHubId,
        uint pathSettlerImprovementGroupId,
        uint requiredBuildCount)
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Settler, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            settlerImprovementGroups:
            [
                new PathSettlerImprovementGroupEntry { Id = 10u, PathSettlerHubId = 47u },
                new PathSettlerImprovementGroupEntry { Id = 11u, PathSettlerHubId = 46u },
                new PathSettlerImprovementGroupEntry { Id = 14u, PathSettlerHubId = 48u }
            ],
            settlerHubs:
            [
                new PathSettlerHubEntry { Id = 46u, MissionCount = 4u },
                new PathSettlerHubEntry { Id = 47u, MissionCount = 1u },
                new PathSettlerHubEntry { Id = 48u, MissionCount = 4u }
            ],
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Settler,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out _,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.Contains(configuredGameTableManager.PathSettlerHub.Entries, hub => hub.Id == pathSettlerHubId);
            Assert.True(manager.TryActivateCurrentZoneEpisode());
            for (uint buildCount = 1u; buildCount <= requiredBuildCount; buildCount++)
            {
                Assert.True(manager.CompleteMissionBySettlerImprovementGroupId(pathSettlerImprovementGroupId));

                Assert.Equal(buildCount == requiredBuildCount, manager.IsMissionComplete(pathMissionId));
            }
        }
    }

    [Theory]
    [InlineData(42u, 34u, 3u)]
    [InlineData(160u, 130u, 3u)]
    [InlineData(648u, 128u, 15u)]
    public void NorthernWildsScientistMission_WithRuntimeZoneBridge_CompletesAfterChecklistScans(
        uint pathMissionId,
        uint pathScientistCreatureInfoId,
        uint requiredScans)
    {
        BuildGameTableProvider(
            [new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }],
            [],
            CreateNorthernWildsPathMissionEntries(),
            null,
            null,
            null,
            null,
            null,
            null,
            CreateNorthernWildsPathEpisodeEntries(),
            CreateNorthernWildsScientistCreatureInfoEntries(),
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Scientist,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out _);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            for (uint scan = 1u; scan < requiredScans; scan++)
            {
                Assert.True(manager.ProgressScientistCreatureScanMission(pathScientistCreatureInfoId));
                Assert.False(manager.IsMissionComplete(pathMissionId));
            }
            Assert.True(manager.ProgressScientistCreatureScanMission(pathScientistCreatureInfoId));

            Assert.True(manager.IsMissionComplete(pathMissionId));
            Assert.True(manager.HasScannedScientistCreature(pathScientistCreatureInfoId));

            ServerPathMissionUpdate completeUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Last();
            Assert.Equal(pathMissionId, completeUpdate.Mission.PathMissionId);
            Assert.True(completeUpdate.Mission.Completed);
            Assert.Equal(requiredScans, completeUpdate.Mission.ProgressCount);
            Assert.Equal(0u, completeUpdate.Mission.ProgressData);
        }
    }

    [Fact]
    public void NorthernWildsMissionRewardGrant_GrantsMissionRewardAndCompletedEpisodeReward()
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
                    PathRewardTypeEnum = PathRewardGrant.MissionRewardType,
                    ObjectId = 33u,
                    Item2Id = 9001u,
                    Count = 2u
                },
                new PathRewardEntry
                {
                    Id = 319u,
                    PathRewardTypeEnum = PathRewardGrant.EpisodeRewardType,
                    ObjectId = 8u,
                    Item2Id = 15486u,
                    Count = 1u
                }
            ],
            CreateNorthernWildsPathMissionEntries(),
            pathEpisodes: CreateNorthernWildsPathEpisodeEntries(),
            worldZones: CreateNorthernWildsMissionZoneEntries(),
            worldLocations: CreateNorthernWildsSoldierMissionWorldLocations(),
            gameFormulas:
            [
                new GameFormulaEntry { Id = 0x017Au, Dataint0 = 50u }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out _,
                out _,
                out var inventoryProxy);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 604u, ParentZoneId = 596u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.True(manager.TryActivateSoldierMissionByEventId(12u));
            Assert.True(manager.CompleteActiveMission(33));

            IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemCreates =
                inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
            Assert.Equal(2, itemCreates.Count);
            Assert.Equal(InventoryLocation.Inventory, itemCreates[0].Arguments[0]);
            Assert.Equal(9001u, itemCreates[0].Arguments[1]);
            Assert.Equal(2u, itemCreates[0].Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreates[0].Arguments[3]);
            Assert.Equal(InventoryLocation.Inventory, itemCreates[1].Arguments[0]);
            Assert.Equal(15486u, itemCreates[1].Arguments[1]);
            Assert.Equal(1u, itemCreates[1].Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreates[1].Arguments[3]);
        }
    }

    [Fact]
    public void NorthernWildsSoldierMission_DoesNotGrantEpisodeRewardUntilAllActiveEpisodeMissionsComplete()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 2u, PathXP = 100u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 319u,
                    PathRewardTypeEnum = PathRewardGrant.EpisodeRewardType,
                    ObjectId = 8u,
                    Item2Id = 15486u,
                    Count = 1u
                }
            ],
            [
                new PathMissionEntry
                {
                    Id = 33u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    ObjectId = 12u,
                    PathMissionFactionEnum = 1u
                },
                new PathMissionEntry
                {
                    Id = 34u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    ObjectId = 13u,
                    PathMissionFactionEnum = 1u
                },
                new PathMissionEntry
                {
                    Id = 156u,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)Path.Soldier,
                    ObjectId = 2u,
                    PathMissionFactionEnum = 1u
                }
            ],
            pathEpisodes:
            [
                new PathEpisodeEntry { Id = 8u, WorldId = 426u, WorldZoneId = 35u, PathTypeEnum = (uint)Path.Soldier }
            ],
            worldZones:
            [
                new WorldZoneEntry { Id = 1u },
                new WorldZoneEntry { Id = 35u }
            ],
            gameFormulas:
            [
                new GameFormulaEntry { Id = 0x017Au, Dataint0 = 50u }
            ]);

        {
            PathManager manager = CreateManager(
                Path.Soldier,
                totalXp: 0u,
                levelRewarded: 1,
                out _,
                out var playerProxy,
                out var sessionProxy,
                out _,
                out var inventoryProxy);
            playerProxy.SetProperty("Faction1", Faction.Exile);
            playerProxy.SetProperty("Map", CreateMap(426u));
            playerProxy.SetProperty("Zone", new WorldZoneEntry { Id = 1u });

            Assert.True(manager.TryActivateCurrentZoneEpisode());
            Assert.True(manager.TryActivateSoldierMissionByEventId(12u));
            Assert.True(manager.TryActivateSoldierMissionByEventId(13u));
            Assert.True(manager.TryActivateSoldierMissionByEventId(2u));
            Assert.True(manager.CompleteActiveMission(33));

            IReadOnlyList<object> messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToList();

            ServerPathUpdateXP xpUpdate = messages.OfType<ServerPathUpdateXP>().Single();
            Assert.Equal(50u, xpUpdate.TotalXP);

            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));

            Assert.True(manager.CompleteActiveMission(34));
            Assert.True(manager.CompleteActiveMission(156));

            RecordingDispatchProxy<IInventory>.Invocation itemCreate =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreate.Arguments[0]);
            Assert.Equal(15486u, itemCreate.Arguments[1]);
            Assert.Equal(1u, itemCreate.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, itemCreate.Arguments[3]);
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
    public void CompleteExplorerProgressMission_WithActiveNodeMissionAndNode_CompletesMission()
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
    public void CompleteCurrentExplorerExploreZoneMission_WithFullyExploredParentMappedZone_CompletesMission()
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
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(100));
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
    public void CompleteCurrentExplorerExploreZoneMission_WithIncompleteMapZone_DoesNotCompleteMission()
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
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(99));
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
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(100));
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
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(100));
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
            playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), CreateZoneMapManagerWithExploredPercent(100));
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

            Assert.True(manager.IsMissionActiveByObjectId(1u));
            Assert.False(manager.IsMissionComplete(1254u));
            Assert.True(manager.CompleteMissionByObjectId(1u));

            Assert.True(manager.IsMissionComplete(1254u));
            Assert.True(manager.IsMissionCompleteByObjectId(1u));
            Assert.False(manager.IsMissionActiveByObjectId(1u));
        }
    }

    [Fact]
    public void IsMissionCompleteByObjectId_WithCompletedMatchingMission_ReturnsTrue()
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
                out _,
                pathMissionModels:
                [
                    new CharacterPathMissionModel
                    {
                        Id = 1,
                        PathMissionId = 1254,
                        PathEpisodeId = 9,
                        Completed = 1,
                        State = (byte)PathMissionState.Complete
                    }
                ]);

            Assert.True(manager.IsMissionCompleteByObjectId(1u));
            Assert.False(manager.IsMissionActiveByObjectId(1u));
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

            Assert.False(manager.IsMissionActiveByObjectId(1u));
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

            Assert.False(manager.IsMissionActiveByObjectId(1u));
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
    public void ProgressSoldierSwatMission_WithActiveMission_UsesSwatCountAndCompletes()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 2424u,
                    PathEpisodeId = 294u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x0007u,
                    ObjectId = 93u
                }
            ],
            soldierSwat:
            [
                new PathSoldierSWATEntry { Id = 93u, Creature2Id = 41146u, Count = 5u }
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
            manager.ActivateMissions(294, new Dictionary<ushort, uint>
            {
                [2424] = 0
            });

            Assert.True(manager.ProgressSoldierSwatMission(2424, 2u));

            Assert.False(manager.IsMissionComplete(2424u));
            ServerPathMissionUpdate partialUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Single();
            Assert.False(partialUpdate.Mission.Completed);
            Assert.Equal(2u, partialUpdate.Mission.ProgressCount);
            Assert.Equal(1u, partialUpdate.Mission.ProgressData);

            Assert.True(manager.ProgressSoldierSwatMission(2424, 10u));

            Assert.True(manager.IsMissionComplete(2424u));
            ServerPathMissionUpdate completeUpdate = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>()
                .Last();
            Assert.True(completeUpdate.Mission.Completed);
            Assert.Equal(5u, completeUpdate.Mission.ProgressCount);
            Assert.Equal(0u, completeUpdate.Mission.ProgressData);
        }
    }

    [Theory]
    [InlineData("path-mission")]
    [InlineData("soldier-swat")]
    public void ProgressSoldierSwatMission_WithMissingStaticTable_DoesNotProgressMission(string missingTable)
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = 2424u,
                    PathEpisodeId = 294u,
                    PathTypeEnum = (uint)Path.Soldier,
                    PathMissionTypeEnum = 0x0007u,
                    ObjectId = 93u
                }
            ],
            soldierSwat:
            [
                new PathSoldierSWATEntry { Id = 93u, Count = 5u }
            ]);

        {
            GameTableManager gameTableManager = configuredGameTableManager;
            SetAutoProperty(
                gameTableManager,
                missingTable == "path-mission" ? nameof(GameTableManager.PathMission) : nameof(GameTableManager.PathSoldierSWAT),
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
            manager.ActivateMissions(294, new Dictionary<ushort, uint>
            {
                [2424] = 0
            });

            Assert.False(manager.ProgressSoldierSwatMission(2424, 1u));
            Assert.False(manager.IsMissionComplete(2424u));
            Assert.Empty(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerPathMissionUpdate>());
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
        IGameTableManager gameTableManager = null,
        IEntityFactory entityFactory = null)
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
            Path = Enumerable
                .Range((int)Path.Soldier, (int)Path.Explorer - (int)Path.Soldier + 1)
                .Select(i =>
                {
                    var modelPath = (Path)i;
                    return new CharacterPathModel
                    {
                        Id = 42ul,
                        Path = (byte)modelPath,
                        Unlocked = (byte)(modelPath == path ? 1 : 0),
                        TotalXp = modelPath == path ? totalXp : 0u,
                        LevelRewarded = modelPath == path ? levelRewarded : (byte)0
                    };
                })
                .ToList(),
            PathMission = pathMissionModels?.ToList() ?? [],
            Datacube    = datacubeModels?.ToList() ?? []
        };

        gameTableManager ??= configuredGameTableManager;
        IPrerequisiteManager prerequisiteManager = configuredPrerequisiteManager;

        var datacubeManager = new DatacubeManager(player, characterModel, gameTableManager);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        var manager = new PathManager(player, characterModel, prerequisiteManager, gameTableManager, entityFactory);
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

    private void BuildScanbotGameTableProvider()
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Scientist, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            scanBotProfiles:
            [
                new PathScientistScanBotProfileEntry { Id = 77u, Creature2Id = 12664u }
            ],
            creature2Entries:
            [
                new Creature2Entry { Id = 12664u }
            ]);
    }

    private static IScannerUnitEntity CreateScanbot(out RecordingDispatchProxy<IScannerUnitEntity> scanbotProxy)
    {
        return RecordingDispatchProxy<IScannerUnitEntity>.Create(out scanbotProxy);
    }

    private static IEntityFactory CreateEntityFactory(
        IScannerUnitEntity scanbot,
        out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy)
    {
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out entityFactoryProxy);
        entityFactoryProxy.SetMethodHandler(nameof(IEntityFactory.CreateEntity), _ => scanbot);
        return entityFactory;
    }

    private static void ConfigureScanbotPlayer(
        RecordingDispatchProxy<IPlayer> playerProxy,
        IScannerUnitEntity scanbot,
        bool profileUnlocked,
        out RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        ConfigureScanbotPlayer(
            playerProxy,
            scanbot,
            profileUnlocked,
            out mapProxy,
            out _);
    }

    private static void ConfigureScanbotPlayer(
        RecordingDispatchProxy<IPlayer> playerProxy,
        IScannerUnitEntity scanbot,
        bool profileUnlocked,
        out RecordingDispatchProxy<IBaseMap> mapProxy,
        out RecordingDispatchProxy<IPetCustomisationManager> petCustomisationManagerProxy,
        uint scanBotProfileId = 77u)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 426u });
        mapProxy.SetMethodHandler(nameof(IBaseMap.CanEnter), _ => true);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args =>
            args.Length == 1 && (uint)args[0] == 9001u ? scanbot : null);

        IPetCustomisationManager petCustomisationManager =
            RecordingDispatchProxy<IPetCustomisationManager>.Create(out petCustomisationManagerProxy);
        IPetCustomisation customisation = profileUnlocked
            ? RecordingDispatchProxy<IPetCustomisation>.Create(out _)
            : null;
        IPetCustomisation unlockedCustomisation = RecordingDispatchProxy<IPetCustomisation>.Create(out _);
        petCustomisationManagerProxy.SetMethodHandler(nameof(IPetCustomisationManager.GetCustomisation), args =>
            (PetType)args[0] == PetType.ScanBot && (uint)args[1] == scanBotProfileId ? customisation : null);
        petCustomisationManagerProxy.SetMethodHandler(nameof(IPetCustomisationManager.UnlockScanBotProfile), args =>
        {
            if ((uint)args[0] == scanBotProfileId)
                customisation = unlockedCustomisation;

            return null;
        });

        playerProxy.SetProperty(nameof(IGridEntity.Guid), 500u);
        playerProxy.SetProperty("Map", map);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
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
        IEnumerable<PathScientistCreatureInfoEntry> pathScientistCreatureInfos = null,
        IEnumerable<WorldZoneEntry> worldZones = null,
        IEnumerable<WorldLocation2Entry> worldLocations = null,
        IEnumerable<MapZoneEntry> mapZones = null,
        IEnumerable<MapZoneWorldJoinEntry> mapZoneWorldJoins = null,
        IEnumerable<Spell4Entry> spell4Entries = null,
        IEnumerable<CharacterTitleEntry> characterTitles = null,
        IEnumerable<PathScientistScanBotProfileEntry> scanBotProfiles = null,
        IEnumerable<PrerequisiteEntry> prerequisites = null,
        IEnumerable<GameFormulaEntry> gameFormulas = null,
        IEnumerable<PathScientistDatacubeDiscoveryEntry> pathScientistDatacubeDiscoveries = null,
        bool includeSpell4Table = true,
        bool includeScanBotProfileTable = true,
        IEnumerable<Creature2Entry> creature2Entries = null,
        IEnumerable<PathSoldierSWATEntry> soldierSwat = null)
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
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSoldierSWAT), CreateGameTable((soldierSwat ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSettlerImprovementGroup), CreateGameTable((settlerImprovementGroups ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathSettlerHub), CreateGameTable((settlerHubs ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathExplorerNode), CreateGameTable((pathExplorerNodes ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathExplorerPowerMap), CreateGameTable((pathExplorerPowerMaps ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathEpisode), CreateGameTable((pathEpisodes ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathScientistCreatureInfo), CreateGameTable((pathScientistCreatureInfos ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathScientistDatacubeDiscovery), CreateGameTable((pathScientistDatacubeDiscoveries ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), CreateGameTable((worldZones ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldLocation2), CreateGameTable((worldLocations ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZone), CreateGameTable((mapZones ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.MapZoneWorldJoin), CreateGameTable((mapZoneWorldJoins ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable((creature2Entries ?? []).ToArray()));
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

    private static PathEpisodeEntry[] CreateNorthernWildsPathEpisodeEntries()
    {
        return
        [
            new PathEpisodeEntry { Id = 8u, WorldId = 426u, WorldZoneId = 35u, PathTypeEnum = (uint)Path.Soldier },
            new PathEpisodeEntry { Id = 9u, WorldId = 426u, WorldZoneId = 35u, PathTypeEnum = (uint)Path.Explorer },
            new PathEpisodeEntry { Id = 28u, WorldId = 426u, WorldZoneId = 35u, PathTypeEnum = (uint)Path.Scientist },
            new PathEpisodeEntry { Id = 82u, WorldId = 426u, WorldZoneId = 35u, PathTypeEnum = (uint)Path.Settler }
        ];
    }

    private static PathMissionEntry[] CreateNorthernWildsPathMissionEntries()
    {
        return
        [
            new PathMissionEntry
            {
                Id = 33u,
                PathEpisodeId = 8u,
                PathTypeEnum = (uint)Path.Soldier,
                PathMissionTypeEnum = 0x0000u,
                ObjectId = 12u,
                WorldLocation2Id00 = 7760u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 34u,
                PathEpisodeId = 8u,
                PathTypeEnum = (uint)Path.Soldier,
                PathMissionTypeEnum = 0x0000u,
                ObjectId = 13u,
                WorldLocation2Id00 = 9285u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 156u,
                PathEpisodeId = 8u,
                PathTypeEnum = (uint)Path.Soldier,
                PathMissionTypeEnum = 0x0000u,
                ObjectId = 2u,
                WorldLocation2Id00 = 8866u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 35u,
                PathEpisodeId = 9u,
                PathTypeEnum = (uint)Path.Explorer,
                PathMissionTypeEnum = 0x000Fu,
                PathMissionDisplayTypeEnum = 31u,
                ObjectId = 37u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 36u,
                PathEpisodeId = 9u,
                PathTypeEnum = (uint)Path.Explorer,
                PathMissionTypeEnum = 0x000Fu,
                PathMissionDisplayTypeEnum = 29u,
                ObjectId = 38u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 158u,
                PathEpisodeId = 9u,
                PathTypeEnum = (uint)Path.Explorer,
                PathMissionTypeEnum = 0x000Fu,
                PathMissionDisplayTypeEnum = 31u,
                ObjectId = 26u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 1254u,
                PathEpisodeId = 9u,
                PathTypeEnum = (uint)Path.Explorer,
                PathMissionTypeEnum = 0x0010u,
                PathMissionDisplayTypeEnum = 24u,
                ObjectId = 1u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 42u,
                PathEpisodeId = 28u,
                PathTypeEnum = (uint)Path.Scientist,
                PathMissionTypeEnum = 0x0002u,
                ObjectId = 34u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 160u,
                PathEpisodeId = 28u,
                PathTypeEnum = (uint)Path.Scientist,
                PathMissionTypeEnum = 0x0002u,
                ObjectId = 130u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 648u,
                PathEpisodeId = 28u,
                PathTypeEnum = (uint)Path.Scientist,
                PathMissionTypeEnum = 0x0002u,
                ObjectId = 128u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 650u,
                PathEpisodeId = 82u,
                PathTypeEnum = (uint)Path.Settler,
                PathMissionTypeEnum = 0x0013u,
                ObjectId = 46u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 651u,
                PathEpisodeId = 82u,
                PathTypeEnum = (uint)Path.Settler,
                PathMissionTypeEnum = 0x0013u,
                ObjectId = 47u,
                PathMissionFactionEnum = 1u
            },
            new PathMissionEntry
            {
                Id = 652u,
                PathEpisodeId = 82u,
                PathTypeEnum = (uint)Path.Settler,
                PathMissionTypeEnum = 0x0013u,
                ObjectId = 48u,
                PathMissionFactionEnum = 1u
            }
        ];
    }

    private static WorldZoneEntry[] CreateNorthernWildsMissionZoneEntries()
    {
        return
        [
            new WorldZoneEntry { Id = 35u },
            new WorldZoneEntry { Id = 590u, ParentZoneId = 35u },
            new WorldZoneEntry { Id = 596u, ParentZoneId = 590u },
            new WorldZoneEntry { Id = 602u, ParentZoneId = 596u },
            new WorldZoneEntry { Id = 604u, ParentZoneId = 596u },
            new WorldZoneEntry { Id = 647u, ParentZoneId = 596u },
            new WorldZoneEntry { Id = 651u, ParentZoneId = 596u }
        ];
    }

    private static WorldLocation2Entry[] CreateNorthernWildsSoldierMissionWorldLocations()
    {
        return
        [
            new WorldLocation2Entry { Id = 7760u, WorldId = 426u, WorldZoneId = 604u },
            new WorldLocation2Entry { Id = 8866u, WorldId = 426u, WorldZoneId = 651u },
            new WorldLocation2Entry { Id = 9285u, WorldId = 426u, WorldZoneId = 602u }
        ];
    }

    private static PathScientistCreatureInfoEntry[] CreateNorthernWildsScientistCreatureInfoEntries()
    {
        return
        [
            new PathScientistCreatureInfoEntry { Id = 34u, ChecklistCount = 3u },
            new PathScientistCreatureInfoEntry { Id = 128u, ChecklistCount = 15u },
            new PathScientistCreatureInfoEntry { Id = 130u, ChecklistCount = 3u }
        ];
    }

    private static IBaseMap CreateMap(uint worldId)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = worldId });
        return map;
    }

    private static IZoneMapManager CreateZoneMapManagerWithExploredPercent(byte exploredPercent)
    {
        IZoneMapManager zoneMapManager = RecordingDispatchProxy<IZoneMapManager>.Create(out RecordingDispatchProxy<IZoneMapManager> zoneMapProxy);
        zoneMapProxy.SetMethodReturn(nameof(IZoneMapManager.GetMapZoneExploredPercent), exploredPercent);
        return zoneMapManager;
    }
}
