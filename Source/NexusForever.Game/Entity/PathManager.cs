using System.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Map;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Entity
{
    public class PathManager : IPathManager
    {
        private const uint MaxPathCount = 4u;
        private const uint MaxPathLevel = PathRewardGrant.MaxPathLevel;
        private const uint SoldierHoldoutMissionType = 0x0000;
        private const uint SoldierAssassinateMissionType = 0x0004;
        private const uint SoldierSwatMissionType = 0x0007;
        private const uint ScientistCreatureInfoMissionType = 0x0002;
        private const uint ScientistDatacubeDiscoveryMissionType = 0x0018;
        private const uint ExplorerNodeMissionType = 0x000F;
        private const uint ExplorerExploreZoneMissionType = 0x0010;
        private const uint ExplorerPowerMapMissionType = 0x0012;
        private const byte ExplorerExploreZoneCompletePercent = 100;
        private const uint SettlerHubMissionType = 0x0013;
        private const uint SettlerInfrastructureMissionType = 0x0015;
        private const uint SettlerImprovementMaxTier = 4u;
        private const uint PathMissionCompletionXpGameFormulaId = 0x017Au;
        private const uint DefaultMissionCompletionXp = 50u;
        private const uint DefaultScientistScanBotProfileId = 1u;
        private const string PathLevelTableName = "PathLevel.tbl";
        private const string PathRewardTableName = "PathReward.tbl";
        private const string Spell4TableName = "Spell4.tbl";
        private const string CharacterTitleTableName = "CharacterTitle.tbl";
        private const string PathScientistScanBotProfileTableName = "PathScientistScanBotProfile.tbl";

        private static readonly IReadOnlyDictionary<(uint WorldId, uint RuntimeWorldZoneId, Path Path), ushort> ReviewedRuntimePathEpisodeZoneBridges =
            new Dictionary<(uint WorldId, uint RuntimeWorldZoneId, Path Path), ushort>
            {
                // DataMapping path_episode_zone_map.csv maps these build-16042 Northern Wilds
                // path episodes to runtime/source zone 1, while PathEpisode.tbl keeps client
                // WorldZoneId 35. Keep this as a reviewed runtime-owned bridge instead of
                // querying authoring/staging tables at runtime.
                [(426u, 1u, Path.Soldier)]   = 8,
                [(426u, 1u, Path.Settler)]   = 82,
                [(426u, 1u, Path.Scientist)] = 28,
                [(426u, 1u, Path.Explorer)]  = 9
            };

        private static readonly IReadOnlyDictionary<(uint WorldId, uint RuntimeWorldZoneId, Path Path), IReadOnlySet<ushort>> ReviewedRuntimePathMissionZoneBridges =
            new Dictionary<(uint WorldId, uint RuntimeWorldZoneId, Path Path), IReadOnlySet<ushort>>
            {
                // PathMission 156 / WorldLocation2 8866 is the Yeti holdout near the
                // Landing Site creature/quest indicator area. The table row is in zone 651,
                // while the player-visible area can report Landing Site zone 647.
                [(426u, 647u, Path.Soldier)] = new HashSet<ushort> { 156 },
                [(426u, 651u, Path.Soldier)] = new HashSet<ushort> { 156 }
            };

        private readonly IPlayer player;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IEntityFactory entityFactory;
        private readonly Dictionary<Path, IPathEntry> paths = new();
        private readonly Dictionary<ushort, PathMissionRuntimeState> pathMissions = [];
        private readonly HashSet<ushort> activatedEpisodes = [];
        private readonly HashSet<ushort> activatedMissions = [];
        private readonly Dictionary<uint, SettlerImprovementGroupRuntimeStatus> settlerImprovementGroupStatus = new();
        private IWorldEntity pendingScientistScanbot;
        private uint activeScientistScanbotGuid;
        private bool scientistScanbotDismissPending;
        private bool initialPacketsSent;

        /// <summary>
        /// Create a new <see cref="IPathManager"/> from <see cref="IPlayer"/> database model.
        /// </summary>
        public PathManager(
            IPlayer owner,
            CharacterModel model,
            IPrerequisiteManager prerequisiteManager = null,
            IGameTableManager gameTableManager = null,
            IEntityFactory entityFactory = null)
        {
            player = owner;
            this.prerequisiteManager = prerequisiteManager;
            this.gameTableManager = gameTableManager;
            this.entityFactory = entityFactory;
            foreach (CharacterPathModel pathModel in model.Path)
                paths.Add((Path)pathModel.Path, new PathEntry(pathModel));

            foreach (CharacterPathMissionModel pathMissionModel in model.PathMission)
            {
                if (pathMissions.ContainsKey(pathMissionModel.PathMissionId))
                    continue;

                var state = new PathMissionRuntimeState(pathMissionModel);
                pathMissions.Add(state.MissionId, state);
            }

            HydrateScientistScanStateFromCompletedMissions();
            Validate();
        }

        private void HydrateScientistScanStateFromCompletedMissions()
        {
            foreach (PathMissionRuntimeState state in pathMissions.Values)
                if (state.Completed)
                    TryMarkScientistScanFromMission(state.MissionId);
        }

        private void TryMarkScientistScanFromMission(ushort pathMissionId)
        {
            if (TryGetScientistCreatureInfoId(pathMissionId, out uint creatureInfoId))
                MarkScientistCreatureScanned(creatureInfoId);
        }

        private bool TryGetScientistCreatureInfoId(ushort pathMissionId, out uint creatureInfoId)
        {
            creatureInfoId = 0u;

            PathMissionEntry mission = gameTableManager?.PathMission?.GetEntry(pathMissionId);
            if (mission == null
                || mission.PathTypeEnum != (uint)Path.Scientist
                || mission.PathMissionTypeEnum != ScientistCreatureInfoMissionType
                || mission.ObjectId == 0u
                || mission.ObjectId > ushort.MaxValue)
            {
                return false;
            }

            if (gameTableManager.PathScientistCreatureInfo?.GetEntry(mission.ObjectId) == null)
                return false;

            creatureInfoId = mission.ObjectId;
            return true;
        }

        public bool HasScannedScientistCreature(uint pathScientistCreatureInfoId)
        {
            if (pathScientistCreatureInfoId == 0u || pathScientistCreatureInfoId > ushort.MaxValue)
                return false;

            return player.DatacubeManager.HasScientistCreatureScan((ushort)pathScientistCreatureInfoId);
        }

        public void MarkScientistCreatureScanned(uint pathScientistCreatureInfoId)
        {
            if (pathScientistCreatureInfoId == 0u || pathScientistCreatureInfoId > ushort.MaxValue)
                return;

            player.DatacubeManager.AddScientistCreatureScan((ushort)pathScientistCreatureInfoId);
        }

        public bool TryDeployScientistScanbot(uint pathScientistScanBotProfileId)
        {
            if (pendingScientistScanbot != null
                || activeScientistScanbotGuid != 0u
                || scientistScanbotDismissPending)
            {
                return false;
            }

            if (!TryGetDeployableScientistScanbotProfile(pathScientistScanBotProfileId, out PathScientistScanBotProfileEntry scanBotProfile))
                return false;

            IScannerUnitEntity scanbot = entityFactory.CreateEntity<IScannerUnitEntity>();
            scanbot.Initialise(scanBotProfile.Creature2Id);
            scanbot.SummonerGuid = player.Guid;
            scanbot.Rotation     = player.Rotation;
            scanbot.Faction1     = player.Faction1;
            scanbot.Faction2     = player.Faction2;

            var mapPosition = new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = player.Map.Entry,
                    MapLock = (player.Map as IMapInstance)?.MapLock
                },
                Position = player.Position
            };

            if (!player.Map.CanEnter(scanbot, mapPosition))
                return false;

            pendingScientistScanbot = scanbot;

            player.Map.EnqueueAdd(scanbot, mapPosition);
            if (scanbot.Guid != 0u)
                OnScientistScanbotSummoned(scanbot);

            return true;
        }

        private bool TryGetDeployableScientistScanbotProfile(uint pathScientistScanBotProfileId, out PathScientistScanBotProfileEntry scanBotProfile)
        {
            scanBotProfile = null;

            if (player.Path != Path.Scientist
                || player.Guid == 0u
                || player.Map == null
                || entityFactory == null)
            {
                return false;
            }

            foreach (uint candidateProfileId in GetScientistScanbotProfileCandidates(pathScientistScanBotProfileId))
            {
                scanBotProfile = gameTableManager?.PathScientistScanBotProfile?.GetEntry(candidateProfileId);
                if (scanBotProfile == null || scanBotProfile.Creature2Id == 0u)
                    continue;

                if (gameTableManager.Creature2?.GetEntry(scanBotProfile.Creature2Id) == null)
                    continue;

                if (player.PetCustomisationManager?.GetCustomisation(PetType.ScanBot, scanBotProfile.Id) != null)
                    return true;

                if (TryRecoverScientistScanbotProfileUnlock(scanBotProfile.Id))
                    return true;
            }

            scanBotProfile = null;
            return false;
        }

        private IEnumerable<uint> GetScientistScanbotProfileCandidates(uint pathScientistScanBotProfileId)
        {
            if (pathScientistScanBotProfileId != 0u)
            {
                yield return pathScientistScanBotProfileId;
                yield break;
            }

            yield return DefaultScientistScanBotProfileId;

            foreach (PathScientistScanBotProfileEntry entry in gameTableManager?.PathScientistScanBotProfile?.Entries ?? [])
            {
                if (entry.Id != 0u && entry.Id != DefaultScientistScanBotProfileId)
                    yield return entry.Id;
            }
        }

        private bool TryRecoverScientistScanbotProfileUnlock(uint pathScientistScanBotProfileId)
        {
            if (pathScientistScanBotProfileId == 0u
                || player.PetCustomisationManager == null
                || !IsEarnedScientistScanbotLevelReward(pathScientistScanBotProfileId))
            {
                return false;
            }

            player.PetCustomisationManager.UnlockScanBotProfile(pathScientistScanBotProfileId);
            return player.PetCustomisationManager.GetCustomisation(PetType.ScanBot, pathScientistScanBotProfileId) != null;
        }

        private bool IsEarnedScientistScanbotLevelReward(uint pathScientistScanBotProfileId)
        {
            IPathEntry scientistEntry = GetPathEntry(Path.Scientist);
            if (scientistEntry == null || !scientistEntry.Unlocked)
                return false;

            if (player.Path == Path.Scientist && pathScientistScanBotProfileId == DefaultScientistScanBotProfileId)
                return true;

            uint rewardedLevel = scientistEntry.LevelRewarded;
            if (player.Path == Path.Scientist && rewardedLevel == 0u)
                rewardedLevel = 1u;

            if (rewardedLevel == 0u || gameTableManager?.PathReward?.Entries == null)
                return false;

            for (uint level = 1u; level <= rewardedLevel; level++)
            {
                uint rewardObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Scientist, level);
                if (gameTableManager.PathReward.Entries.Any(entry =>
                    entry.ObjectId == rewardObjectId
                    && entry.PathScientistScanBotProfileId == pathScientistScanBotProfileId
                    && PathRewardGrant.IsGrantableLevelReward(entry)
                    && (entry.PrerequisiteId == 0u || GetPrerequisiteManager().Meets(player, entry.PrerequisiteId))))
                    return true;
            }

            return false;
        }

        public bool DismissScientistScanbot()
        {
            if (pendingScientistScanbot == null && activeScientistScanbotGuid == 0u)
                return false;

            scientistScanbotDismissPending = true;
            SendScientistScanbotState(0u, GetScientistScanbotDismissCooldownMS());

            bool removeQueued = TryRemoveScientistScanbot();
            if (!removeQueued && pendingScientistScanbot == null)
                ClearScientistScanbotState();

            return true;
        }

        private bool TryRemoveScientistScanbot()
        {
            if (activeScientistScanbotGuid != 0u)
            {
                IWorldEntity activeScanbot = player.Map?.GetEntity<IWorldEntity>(activeScientistScanbotGuid);
                if (activeScanbot?.SummonerGuid == player.Guid)
                {
                    activeScanbot.RemoveFromMap();
                    return true;
                }
            }

            if (pendingScientistScanbot?.InWorld == true)
            {
                pendingScientistScanbot.RemoveFromMap();
                return true;
            }

            return false;
        }

        public bool OnScientistScanbotSummoned(IWorldEntity entity)
        {
            if (!ReferenceEquals(entity, pendingScientistScanbot)
                || entity.Guid == 0u
                || entity.SummonerGuid != player.Guid)
            {
                return false;
            }

            if (scientistScanbotDismissPending)
            {
                entity.RemoveFromMap();
                return true;
            }

            activeScientistScanbotGuid = entity.Guid;
            pendingScientistScanbot = null;
            SendScientistScanbotState(activeScientistScanbotGuid, 0u);
            return true;
        }

        public bool OnScientistScanbotUnsummoned(IWorldEntity entity)
        {
            bool matchesPending = ReferenceEquals(entity, pendingScientistScanbot);
            bool matchesActive = entity?.Guid != 0u && entity.Guid == activeScientistScanbotGuid;
            if (!matchesPending && !matchesActive)
                return false;

            bool sendDespawn = !scientistScanbotDismissPending && activeScientistScanbotGuid != 0u;
            ClearScientistScanbotState();

            if (sendDespawn)
                SendScientistScanbotState(0u, 0u);

            return true;
        }

        private void ClearScientistScanbotState()
        {
            pendingScientistScanbot = null;
            activeScientistScanbotGuid = 0u;
            scientistScanbotDismissPending = false;
        }

        private uint GetScientistScanbotDismissCooldownMS()
        {
            // Cooldown timing is still blocked on retail evidence; avoid imposing a guessed client lockout.
            return 0u;
        }

        private void SendScientistScanbotState(uint scanbotUnitId, uint scanbotCooldownMS)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathScientistScanbotState
            {
                ScanbotUnitId     = scanbotUnitId,
                ScanbotCooldownMS = scanbotCooldownMS
            });
        }

        private void Validate()
        {
            if (paths.Count != MaxPathCount)
            {
                // sanity checks to make sure a player always has entries for all paths
                if (paths.Count == 0)
                    SetPathEntry(player.Path, PathCreate(player.Path, true));

                for (Path path = Path.Soldier; path <= Path.Explorer; path++)
                    if (GetPathEntry(path) == null)
                        SetPathEntry(path, PathCreate(path));
            }

        }

        /// <summary>
        /// Create a new <see cref="IPathEntry"/>.
        /// </summary>
        private IPathEntry PathCreate(Path path, bool unlocked = false)
        {
            if (path > Path.Explorer)
                return null;

            if (GetPathEntry(path) != null)
                throw new ArgumentException($"{path} is already added to the player!");

            var pathEntry = new PathEntry(
                player.CharacterId,
                path,
                unlocked
            );
            SetPathEntry(path, pathEntry);
            return pathEntry;
        }

        /// <summary>
        /// Checks to see if a <see cref="IPlayer"/>'s <see cref="Path"/> is active.
        /// </summary>
        public bool IsPathActive(Path pathToCheck)
        {
            return player.Path == pathToCheck;
        }

        /// <summary>
        /// Attempts to activate a <see cref="IPlayer"/>'s <see cref="Path"/>.
        /// </summary>
        public void ActivatePath(Path pathToActivate)
        {
            if (pathToActivate > Path.Explorer)
                throw new ArgumentException("Path is not recognised.");

            if (!IsPathUnlocked(pathToActivate))
                throw new ArgumentException("Path is not unlocked.");

            if (IsPathActive(pathToActivate))
                throw new ArgumentException("Path is already active.");

            if (player.Path == Path.Scientist)
                DismissScientistScanbot();

            player.Path = pathToActivate;

            SendServerPathActivateResult(GenericError.Ok);
            SendSetUnitPathTypePacket();
            SendPathLogPacket();
            ReplayCurrentZoneEpisode(allowWhileLoading: false);
        }

        /// <summary>
        /// Checks to see if a <see cref="IPlayer"/>'s <see cref="Path"/> is mathced by a corresponding <see cref="PathUnlockedMask"/> flag.
        /// </summary>
        public bool IsPathUnlocked(Path pathToUnlock)
        {
            return GetPathEntry(pathToUnlock).Unlocked;
        }

        /// <summary>
        /// Attemps to adjust the <see cref="IPlayer"/>'s <see cref="PathUnlockedMask"/> status.
        /// </summary>
        public void UnlockPath(Path pathToUnlock)
        {
            if (pathToUnlock > Path.Explorer)
                throw new ArgumentException("Path is not recognised.");

            if (IsPathUnlocked(pathToUnlock))
                throw new ArgumentException("Path is already unlocked.");

            GetPathEntry(pathToUnlock).Unlocked = true;

            SendServerPathUnlockResult();
            SendPathLogPacket();
            ReplayCurrentZoneEpisode(allowWhileLoading: false);
        }

        /// <summary>
        /// Add XP to the current <see cref="Path"/>.
        /// </summary>
        public void AddXp(uint xp)
        {
            if (xp == 0)
                throw new ArgumentException("XP must be greater than 0.");

            Path path = player.Path;
            IPathEntry entry = GetPathEntry(path);
            if (!TryGetCurrentLevel(path, out uint currentLevel))
                return;

            bool xpChanged = false;

            if (currentLevel < MaxPathLevel)
            {
                checked
                {
                    entry.TotalXp += xp;
                }
                xpChanged = true;
            }

            GrantOutstandingLevelRewards(path, entry.TotalXp);
            if (xpChanged)
                SendServerPathUpdateXp(entry.TotalXp);
        }

        /// <summary>
        /// Add path levels to the current <see cref="Path"/>.
        /// </summary>
        public void AddLevels(uint levels)
        {
            if (levels == 0u)
                throw new ArgumentException("Levels must be greater than 0.");

            Path path = player.Path;
            if (!TryGetCurrentLevel(path, out uint currentLevel))
                return;

            IPathEntry entry = GetPathEntry(path);
            if (currentLevel >= MaxPathLevel)
            {
                GrantOutstandingLevelRewards(path, entry.TotalXp);
                return;
            }

            uint targetLevel = Math.Min(currentLevel + levels, MaxPathLevel);
            if (!TryGetPathXpForLevel(path, targetLevel, out uint targetXp))
            {
                GrantOutstandingLevelRewards(path, entry.TotalXp);
                return;
            }

            if (targetXp <= entry.TotalXp)
            {
                GrantOutstandingLevelRewards(path, entry.TotalXp);
                return;
            }

            AddXp(targetXp - entry.TotalXp);
        }

        public void ActivateMissions(ushort episodeId, IReadOnlyDictionary<ushort, uint> missionXp)
        {
            ActivateMissions(episodeId, missionXp, sendMissionActivate: true);
        }

        private void ActivateMissions(ushort episodeId, IReadOnlyDictionary<ushort, uint> missionXp, bool sendMissionActivate)
        {
            if (episodeId == 0 || missionXp == null || missionXp.Count == 0)
                return;

            HashSet<ushort> activatedMissionIds = [];
            foreach ((ushort missionId, uint xp) in missionXp)
            {
                if (!pathMissions.TryGetValue(missionId, out PathMissionRuntimeState state))
                {
                    state = new PathMissionRuntimeState(player.CharacterId, missionId, episodeId);
                    pathMissions.Add(missionId, state);
                }

                state.EpisodeId = episodeId;
                state.Xp = xp;
                if (!state.Completed)
                {
                    state.State = PathMissionState.Started;
                    if (activatedMissions.Add(missionId))
                        activatedMissionIds.Add(missionId);
                }
            }

            bool sendCurrentEpisode = activatedEpisodes.Add(episodeId);
            if (activatedMissionIds.Count == 0)
            {
                if (sendCurrentEpisode)
                    SendPathCurrentEpisode(episodeId);
                return;
            }

            if (sendCurrentEpisode)
                SendPathCurrentEpisode(episodeId);
            SendPathEpisodeProgress(episodeId, missionIds: activatedMissionIds);
            if (sendMissionActivate)
                SendPathMissionActivate(episodeId, missionIds: activatedMissionIds);
        }

        private void DiscoverMissions(ushort episodeId, IReadOnlyDictionary<ushort, uint> missionXp)
        {
            if (episodeId == 0 || missionXp == null || missionXp.Count == 0)
                return;

            HashSet<ushort> discoveredMissionIds = [];
            foreach ((ushort missionId, uint xp) in missionXp)
            {
                if (!pathMissions.TryGetValue(missionId, out PathMissionRuntimeState state))
                {
                    state = new PathMissionRuntimeState(player.CharacterId, missionId, episodeId);
                    pathMissions.Add(missionId, state);
                }

                state.EpisodeId = episodeId;
                state.Xp = xp;
                if (state.Completed || state.State == PathMissionState.Complete)
                    continue;

                state.State = PathMissionState.Started;

                if (activatedMissions.Add(missionId))
                    discoveredMissionIds.Add(missionId);
            }

            bool sendCurrentEpisode = activatedEpisodes.Add(episodeId);
            if (discoveredMissionIds.Count == 0)
            {
                if (sendCurrentEpisode)
                    SendPathCurrentEpisode(episodeId);
                return;
            }

            if (sendCurrentEpisode)
                SendPathCurrentEpisode(episodeId);
            SendPathMissionActivate(episodeId, missionIds: discoveredMissionIds);
        }

        public bool TryActivateCurrentZoneEpisode()
        {
            WorldZoneEntry zone = player.Zone;
            if (zone == null)
                return false;

            return TryActivateCurrentZoneEpisode(zone.Id, allowWhileLoading: false);
        }

        public bool TryActivateCurrentZoneEpisode(uint worldZoneId)
        {
            return TryActivateCurrentZoneEpisode(worldZoneId, allowWhileLoading: false);
        }

        private bool TryActivateCurrentZoneEpisode(uint worldZoneId, bool allowWhileLoading)
        {
            uint worldId = player.Map?.Entry?.Id ?? 0u;
            if (worldId == 0u || worldZoneId == 0u)
                return false;

            if (!allowWhileLoading && player.IsLoading)
                return false;

            if (gameTableManager.PathEpisode?.Entries == null
                || gameTableManager.PathMission?.Entries == null)
                return false;

            IReadOnlyList<uint> zoneIds = GetCurrentAndAncestorZoneIds(worldZoneId);
            if (zoneIds.Count == 0)
                return false;

            PathEpisodeEntry pathEpisode = GetCurrentZonePathEpisode(worldId, zoneIds);
            if (pathEpisode == null || pathEpisode.Id > 0x3FFFu)
                return false;

            List<PathMissionEntry> eligibleMissions = gameTableManager.PathMission.Entries
                .Where(m => m.PathEpisodeId == pathEpisode.Id
                    && m.PathTypeEnum == (uint)player.Path
                    && IsMissionFactionAllowed(m)
                    && IsMissionPrerequisiteAllowed(m)
                    && m.Id <= 0x7FFFu)
                .OrderBy(m => m.Id)
                .ToList();

            IReadOnlySet<ushort> reviewedZoneMissionIds = GetReviewedRuntimePathMissionIds(worldId, zoneIds);
            bool hasLocationScopedMissions = eligibleMissions.Any(HasMissionWorldLocation);
            List<PathMissionEntry> zoneMissions = eligibleMissions
                .Where(m => reviewedZoneMissionIds.Contains((ushort)m.Id)
                    || IsMissionLocationInZone(worldId, m, worldZoneId, zoneIds))
                .ToList();
            if (hasLocationScopedMissions)
            {
                eligibleMissions = zoneMissions
                    .Concat(eligibleMissions.Where(m => !HasMissionWorldLocation(m)))
                    .DistinctBy(m => m.Id)
                    .OrderBy(m => m.Id)
                    .ToList();
            }
            else if (zoneMissions.Count != 0)
            {
                eligibleMissions = zoneMissions;
            }

            Dictionary<ushort, uint> missionsToActivate = eligibleMissions
                .Where(IsZoneEntryActivationAllowed)
                .ToDictionary(m => (ushort)m.Id, _ => 0u);
            Dictionary<ushort, uint> missionsToDiscover = eligibleMissions
                .Where(m => !IsZoneEntryActivationAllowed(m))
                .ToDictionary(m => (ushort)m.Id, _ => 0u);
            if (missionsToActivate.Count == 0 && missionsToDiscover.Count == 0)
            {
                SendPathCurrentEpisodeIfNeeded((ushort)pathEpisode.Id);
                return true;
            }

            // WIP/GUESSED: LaughingWS activated the current PathEpisode on zone changes.
            // This keeps the safe table-backed episode/mission surface, but leaves durable
            // path persistence, exact per-mission reward precision, and broader
            // unlock sequencing blocked. Regular zone missions send both progress and
            // activation state because ZoneMap expects an activated mission entry when
            // rendering current-zone path missions. Soldier holdouts remain discovered-only
            // until the player interacts with the holdout beacon.
            if (missionsToActivate.Count != 0)
                ActivateMissions((ushort)pathEpisode.Id, missionsToActivate, sendMissionActivate: true);
            if (missionsToDiscover.Count != 0)
                DiscoverMissions((ushort)pathEpisode.Id, missionsToDiscover);
            return true;
        }

        private PathEpisodeEntry GetCurrentZonePathEpisode(uint worldId, IReadOnlyList<uint> zoneIds)
        {
            Path activePath = player.Path;
            foreach (uint zoneId in zoneIds)
            {
                PathEpisodeEntry pathEpisode = gameTableManager.PathEpisode.Entries
                    .FirstOrDefault(e => e.WorldId == worldId
                        && e.WorldZoneId == zoneId
                        && e.PathTypeEnum == (uint)activePath);
                if (pathEpisode != null)
                    return pathEpisode;

                if (!ReviewedRuntimePathEpisodeZoneBridges.TryGetValue((worldId, zoneId, activePath), out ushort bridgedEpisodeId))
                    continue;

                pathEpisode = gameTableManager.PathEpisode.GetEntry(bridgedEpisodeId);
                if (pathEpisode == null)
                    continue;

                if (pathEpisode.WorldId != worldId || pathEpisode.PathTypeEnum != (uint)activePath)
                    continue;

                return pathEpisode;
            }

            return null;
        }

        public bool CompleteMission(ushort pathMissionId)
        {
            PathMissionRuntimeState state = GetOrCreateMissionState(pathMissionId);
            if (state.Completed)
                return false;

            state.Completed = true;
            state.ProgressCount = Math.Max(state.ProgressCount, 1u);
            state.ProgressData = 0u;
            state.State = PathMissionState.Complete;

            player.Session.EnqueueMessageEncrypted(new ServerPathMissionAdvanced
            {
                PathMissionId = pathMissionId
            });
            player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
            {
                Mission = BuildMission(state)
            });

            PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(pathMissionId);
            if (mission != null)
            {
                // WIP/GUESSED: LaughingWS exposes AchievementType.PathMission (62) and
                // PathMissionType (63) for path mission completion credit. Total/count-style
                // path mission achievement triggers remain blocked until their retail call
                // pattern is mapped.
                player.AchievementManager.CheckAchievements(player, AchievementType.PathMission, mission.Id);
                player.AchievementManager.CheckAchievements(player, AchievementType.PathMissionType, mission.PathMissionTypeEnum);
            }

            uint xp = GetMissionCompletionXp(state, mission, player.Path);
            if (xp > 0u)
                AddXp(xp);

            GrantMissionRewards(pathMissionId);
            GrantEpisodeRewardsIfComplete(state.EpisodeId);
            TryMarkScientistScanFromMission(pathMissionId);

            return true;
        }

        private uint GetMissionCompletionXp(PathMissionRuntimeState state, PathMissionEntry mission, Path activePath)
        {
            if (state.Xp > 0u)
                return state.Xp;

            if (mission == null)
                return 0u;

            if (mission.PathTypeEnum != (uint)activePath)
                return 0u;

            // Client Game.PathMission.GetRewardXp reads GameFormula 0x017a Dataint0,
            // and falls back to 50 if the table row is unavailable. Per-mission reward
            // precision remains blocked pending stronger PathReward evidence.
            GameFormulaEntry formula = gameTableManager.GameFormula?.GetEntry(PathMissionCompletionXpGameFormulaId);
            return formula?.Dataint0 > 0u ? formula.Dataint0 : DefaultMissionCompletionXp;
        }

        public bool CompleteActiveMission(ushort pathMissionId)
        {
            if (!pathMissions.ContainsKey(pathMissionId))
                return false;

            return CompleteMission(pathMissionId);
        }

        public bool CompleteExplorerProgressMission(ushort pathMissionId, uint explorerNodeIndex)
        {
            // Current evidence proves mission/node table validation; exact node-index semantics are still unmapped.
            _ = explorerNodeIndex;

            if (!pathMissions.ContainsKey(pathMissionId))
                return false;

            PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(pathMissionId);
            if (mission == null
                || mission.PathTypeEnum != (uint)Path.Explorer
                || mission.PathMissionTypeEnum != ExplorerNodeMissionType)
                return false;

            bool hasExplorerNode = gameTableManager.PathExplorerNode?.Entries
                .Any(n => n.PathExplorerAreaId == mission.ObjectId) ?? false;
            if (!hasExplorerNode)
                return false;

            return CompleteMission(pathMissionId);
        }

        public bool CompleteExplorerPowerMapMission(uint pathExplorerPowerMapId)
        {
            if (pathExplorerPowerMapId == 0u)
                return false;

            if (gameTableManager.PathExplorerPowerMap?.GetEntry(pathExplorerPowerMapId) == null)
                return false;

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Explorer
                    || mission.PathMissionTypeEnum != ExplorerPowerMapMissionType
                    || mission.ObjectId != pathExplorerPowerMapId)
                    continue;

                // WIP/GUESSED: client evidence proves 0x00F9 carries the PathMission.ObjectId
                // for type 0x12 Explorer power-map progress. Exact server-owned progress,
                // ready/active timers, and failure state packets remain blocked.
                completedAny |= CompleteMission(state.MissionId);
            }

            return completedAny;
        }

        public bool CompleteCurrentExplorerExploreZoneMission()
        {
            if (player.Path != Path.Explorer)
                return false;

            uint mapZoneId = ResolveCurrentMapZoneId();
            if (mapZoneId == 0u)
                return false;

            if (mapZoneId > ushort.MaxValue)
                return false;

            byte exploredPercent = player.ZoneMapManager?.GetMapZoneExploredPercent((ushort)mapZoneId) ?? 0;
            if (exploredPercent < ExplorerExploreZoneCompletePercent)
                return false;

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Explorer
                    || mission.PathMissionTypeEnum != ExplorerExploreZoneMissionType
                    || mission.ObjectId != mapZoneId)
                    continue;

                // LaughingWS marks Explorer_ExploreZone as ObjectId == MapZone.Id. Local
                // Explorer video/transcript evidence for Northern Wilds cartography says the
                // mission completes only after every visible map hex is filled in, so require
                // the server-owned ZoneMap exploration percent to be complete before advancing.
                completedAny |= CompleteMission(state.MissionId);
            }

            return completedAny;
        }

        public bool CompleteMissionByObjectId(uint objectId)
        {
            if (objectId == 0u)
                return false;

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                PathMissionEntry entry = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (entry?.ObjectId != objectId)
                    continue;

                if (entry.PathTypeEnum != (uint)player.Path)
                    continue;

                if (state.State != PathMissionState.Started)
                    continue;

                // WIP/GUESSED: current path runtime is session-local, so object-id completion
                // is limited to active-path rows. This mirrors the client visibility path gate
                // while durable path episode/mission ownership remains blocked.
                completedAny |= CompleteMission(state.MissionId);
            }

            return completedAny;
        }

        public bool IsMissionActiveByObjectId(uint objectId)
        {
            if (objectId == 0u)
                return false;

            foreach (PathMissionRuntimeState state in pathMissions.Values)
            {
                if (state.Completed || state.State == PathMissionState.Complete)
                    continue;

                if (state.State != PathMissionState.Started)
                    continue;

                PathMissionEntry entry = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (entry?.ObjectId != objectId)
                    continue;

                if (entry.PathTypeEnum != (uint)player.Path)
                    continue;

                return true;
            }

            return false;
        }

        public bool IsMissionCompleteByObjectId(uint objectId)
        {
            if (objectId == 0u)
                return false;

            foreach (PathMissionRuntimeState state in pathMissions.Values)
            {
                if (!state.Completed && state.State != PathMissionState.Complete)
                    continue;

                PathMissionEntry entry = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (entry?.ObjectId != objectId)
                    continue;

                if (entry.PathTypeEnum != (uint)player.Path)
                    continue;

                return true;
            }

            return false;
        }

        public bool TryActivateSoldierMissionByEventId(uint pathSoldierEventId)
        {
            if (player.Path != Path.Soldier
                || pathSoldierEventId == 0u
                || gameTableManager?.PathMission?.Entries == null)
            {
                return false;
            }

            PathMissionEntry mission = gameTableManager.PathMission.Entries
                .Where(m => m.PathTypeEnum == (uint)Path.Soldier
                    && m.PathMissionTypeEnum == SoldierHoldoutMissionType
                    && m.ObjectId == pathSoldierEventId
                    && m.Id <= ushort.MaxValue
                    && m.PathEpisodeId <= ushort.MaxValue
                    && IsMissionFactionAllowed(m)
                    && IsMissionPrerequisiteAllowed(m))
                .OrderBy(m => m.Id)
                .FirstOrDefault();
            if (mission == null)
                return false;

            StartMission((ushort)mission.PathEpisodeId, (ushort)mission.Id, 0u);

            return IsMissionActiveByObjectId(pathSoldierEventId)
                || IsMissionCompleteByObjectId(pathSoldierEventId);
        }

        private void StartMission(ushort episodeId, ushort missionId, uint xp)
        {
            if (!pathMissions.TryGetValue(missionId, out PathMissionRuntimeState state))
            {
                state = new PathMissionRuntimeState(player.CharacterId, missionId, episodeId);
                pathMissions.Add(missionId, state);
            }

            state.EpisodeId = episodeId;
            state.Xp = xp;
            if (state.Completed || state.State == PathMissionState.Complete)
                return;

            state.State = PathMissionState.Started;

            bool sendCurrentEpisode = activatedEpisodes.Add(episodeId);
            if (sendCurrentEpisode)
                SendPathCurrentEpisode(episodeId);

            if (activatedMissions.Add(missionId))
                SendPathMissionActivate(episodeId, new HashSet<ushort> { missionId });
        }

        public bool CompleteMissionBySoldierTowerDefenseId(uint pathSoldierTowerDefenseId)
        {
            if (pathSoldierTowerDefenseId == 0u)
                return false;

            PathSoldierTowerDefenseEntry entry = gameTableManager.PathSoldierTowerDefense?.GetEntry(pathSoldierTowerDefenseId);
            return entry != null && CompleteMissionByObjectId(entry.PathSoldierEventId);
        }

        public bool ProgressScientistCreatureScanMission(uint pathScientistCreatureInfoId)
        {
            if (player.Path != Path.Scientist)
                return false;

            if (pathScientistCreatureInfoId == 0u || pathScientistCreatureInfoId > ushort.MaxValue)
                return false;

            PathScientistCreatureInfoEntry creatureInfo = gameTableManager?.PathScientistCreatureInfo?.GetEntry(pathScientistCreatureInfoId);
            if (creatureInfo == null)
                return false;

            MarkScientistCreatureScanned(pathScientistCreatureInfoId);

            bool progressedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                if (state.Completed)
                    continue;

                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Scientist
                    || mission.PathMissionTypeEnum != ScientistCreatureInfoMissionType
                    || mission.ObjectId != pathScientistCreatureInfoId)
                    continue;

                uint requiredCount = Math.Max(creatureInfo.ChecklistCount, 1u);
                state.ProgressCount = Math.Min(state.ProgressCount + 1u, requiredCount);
                state.ProgressData = state.ProgressCount >= requiredCount ? 0u : 1u;
                progressedAny = true;

                if (state.ProgressCount >= requiredCount)
                {
                    CompleteMission(state.MissionId);
                    continue;
                }

                // Client PathMission.GetNumCompleted reads the first progress payload for
                // Scientist creature-info missions. ProgressData stays an in-progress marker
                // until per-scan checklist-bit producer semantics are fully mapped.
                player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
                {
                    Mission = BuildMission(state)
                });
            }

            return progressedAny;
        }

        public bool CompleteCurrentScientistDatacubeDiscoveryMission()
        {
            if (player.Path != Path.Scientist)
                return false;

            if (player.Zone == null
                || gameTableManager?.PathMission == null
                || gameTableManager.PathScientistDatacubeDiscovery == null)
            {
                return false;
            }

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                if (state.Completed)
                    continue;

                PathMissionEntry mission = gameTableManager.PathMission.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Scientist
                    || mission.PathMissionTypeEnum != ScientistDatacubeDiscoveryMissionType
                    || mission.ObjectId == 0u)
                {
                    continue;
                }

                PathScientistDatacubeDiscoveryEntry discovery = gameTableManager.PathScientistDatacubeDiscovery.GetEntry(mission.ObjectId);
                if (discovery == null || !IsCurrentOrAncestorZone(discovery.WorldZoneId))
                    continue;

                completedAny |= CompleteMission(state.MissionId);
            }

            return completedAny;
        }

        public bool ProgressSoldierAssassinateMissionForCreatureKill(uint creature2Id, IReadOnlyCollection<uint> targetGroupIds)
        {
            if (player.Path != Path.Soldier)
                return false;

            targetGroupIds ??= Array.Empty<uint>();
            if (creature2Id == 0u && targetGroupIds.Count == 0)
                return false;

            bool progressedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                if (state.Completed)
                    continue;

                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Soldier
                    || mission.PathMissionTypeEnum != SoldierAssassinateMissionType)
                    continue;

                PathSoldierAssassinateEntry assassinate = gameTableManager.PathSoldierAssassinate?.GetEntry(mission.ObjectId);
                if (!MatchesSoldierAssassinateKill(assassinate, creature2Id, targetGroupIds))
                    continue;

                uint requiredCount = Math.Max(assassinate.Count, 1u);
                state.ProgressCount = Math.Min(state.ProgressCount + 1u, requiredCount);
                state.ProgressData = state.ProgressCount >= requiredCount ? 0u : 1u;
                progressedAny = true;

                if (state.ProgressCount >= requiredCount)
                {
                    CompleteMission(state.MissionId);
                    continue;
                }

                // Client PathMission.GetNumCompleted reads the first progress payload for
                // Soldier_Assassinate (0x04). ProgressData remains a conservative in-progress
                // marker until per-type producer semantics are fully mapped.
                player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
                {
                    Mission = BuildMission(state)
                });
            }

            return progressedAny;
        }

        public bool ProgressSoldierSwatMission(ushort pathMissionId, uint amount)
        {
            if (player.Path != Path.Soldier || amount == 0u)
                return false;

            if (!pathMissions.TryGetValue(pathMissionId, out PathMissionRuntimeState state)
                || state.Completed
                || state.State != PathMissionState.Started)
            {
                return false;
            }

            PathMissionEntry mission = gameTableManager?.PathMission?.GetEntry(pathMissionId);
            if (mission == null
                || mission.PathTypeEnum != (uint)Path.Soldier
                || mission.PathMissionTypeEnum != SoldierSwatMissionType)
            {
                return false;
            }

            PathSoldierSWATEntry swat = gameTableManager.PathSoldierSWAT?.GetEntry(mission.ObjectId);
            if (swat == null)
                return false;

            uint requiredCount = Math.Max(swat.Count, 1u);
            uint remaining = state.ProgressCount >= requiredCount ? 0u : requiredCount - state.ProgressCount;
            state.ProgressCount += Math.Min(amount, remaining);
            state.ProgressData = state.ProgressCount >= requiredCount ? 0u : 1u;

            if (state.ProgressCount >= requiredCount)
                return CompleteMission(state.MissionId);

            player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
            {
                Mission = BuildMission(state)
            });
            return true;
        }

        public bool CompleteMissionBySettlerImprovementGroupId(uint pathSettlerImprovementGroupId)
        {
            if (player.Path != Path.Settler)
                return false;

            if (pathSettlerImprovementGroupId == 0u)
                return false;

            PathSettlerImprovementGroupEntry entry = gameTableManager.PathSettlerImprovementGroup?.GetEntry(pathSettlerImprovementGroupId);
            if (entry == null || entry.PathSettlerHubId == 0u)
                return false;

            PathSettlerHubEntry hub = gameTableManager.PathSettlerHub?.GetEntry(entry.PathSettlerHubId);
            if (hub == null)
                return false;

            bool progressedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                if (state.Completed)
                    continue;

                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Settler
                    || mission.PathMissionTypeEnum != SettlerHubMissionType
                    || mission.ObjectId != entry.PathSettlerHubId)
                    continue;

                uint requiredCount = Math.Max(hub.MissionCount, 1u);
                state.ProgressCount = Math.Min(state.ProgressCount + 1u, requiredCount);
                state.ProgressData = state.ProgressCount >= requiredCount ? 0u : 1u;
                progressedAny = true;

                if (state.ProgressCount >= requiredCount)
                {
                    CompleteMission(state.MissionId);
                    continue;
                }

                // Client PathMission.GetNumCompleted reads the first progress payload for
                // Settler_Hub (0x13). We count each accepted build-tier request as one hub
                // contribution because durable built-group state, resource costs, avenue totals,
                // and unique-build contribution semantics are still blocked.
                player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
                {
                    Mission = BuildMission(state)
                });
            }

            return progressedAny;
        }

        public bool IsMissionComplete(uint pathMissionId)
        {
            return pathMissionId <= ushort.MaxValue
                && pathMissions.TryGetValue((ushort)pathMissionId, out PathMissionRuntimeState state)
                && state.Completed;
        }

        public bool TryIsPathMissionChecklistItemComplete(ushort pathMissionId, uint checklistIndex, out bool isComplete)
        {
            isComplete = false;
            if (!pathMissions.TryGetValue(pathMissionId, out PathMissionRuntimeState state))
                return false;

            if (state.Completed || state.State == PathMissionState.Complete)
            {
                isComplete = true;
                return true;
            }

            if (checklistIndex >= 32)
                return true;

            isComplete = (state.ProgressData & (1u << (int)checklistIndex)) != 0;
            return true;
        }

        public SettlerInfrastructureState GetSettlerInfrastructureState(uint pathSettlerInfrastructureId)
        {
            if (gameTableManager.PathSettlerInfrastructure?.GetEntry(pathSettlerInfrastructureId) == null
                || gameTableManager.PathMission?.Entries == null)
            {
                return SettlerInfrastructureState.Inactive;
            }

            PathMissionEntry mission = gameTableManager.PathMission.Entries
                .FirstOrDefault(entry => entry.PathMissionTypeEnum == SettlerInfrastructureMissionType
                    && entry.ObjectId == pathSettlerInfrastructureId);
            if (mission == null)
                return SettlerInfrastructureState.Inactive;

            if (!pathMissions.TryGetValue((ushort)mission.Id, out PathMissionRuntimeState state))
                return SettlerInfrastructureState.Inactive;

            if (state.Completed || state.State == PathMissionState.Complete)
                return SettlerInfrastructureState.Built;

            if (state.State == PathMissionState.Started || state.State == PathMissionState.Unlocked)
                return SettlerInfrastructureState.Building;

            return SettlerInfrastructureState.Inactive;
        }

        public void ApplySettlerImprovementGroupStatus(uint pathSettlerImprovementGroupId, int tier, uint bundleCount)
        {
            if (pathSettlerImprovementGroupId == 0u)
                return;

            settlerImprovementGroupStatus[pathSettlerImprovementGroupId] = new SettlerImprovementGroupRuntimeStatus(tier, bundleCount);
        }

        public uint GetSettlerHubBuildProgressPercent(uint pathSettlerHubOrImprovementGroupId)
        {
            uint hubId = ResolveSettlerHubId(pathSettlerHubOrImprovementGroupId);
            if (hubId == 0u)
                return 0u;

            uint missionPercent = GetSettlerHubMissionProgressPercent(hubId);
            if (pathSettlerHubOrImprovementGroupId != hubId
                && settlerImprovementGroupStatus.TryGetValue(pathSettlerHubOrImprovementGroupId, out SettlerImprovementGroupRuntimeStatus status))
            {
                uint tierPercent = PercentFromTier(status.Tier);
                return Math.Max(missionPercent, tierPercent);
            }

            return missionPercent;
        }

        public uint GetSettlerHubContributionProgressPercent(uint pathSettlerHubOrImprovementGroupId)
        {
            uint improvementGroupId = ResolveSettlerImprovementGroupId(pathSettlerHubOrImprovementGroupId);
            if (improvementGroupId != 0u
                && settlerImprovementGroupStatus.TryGetValue(improvementGroupId, out SettlerImprovementGroupRuntimeStatus status))
            {
                PathSettlerImprovementGroupEntry group = gameTableManager.PathSettlerImprovementGroup?.GetEntry(improvementGroupId);
                if (group != null && group.MaxBundleCount > 0u)
                    return Math.Min(100u, status.BundleCount * 100u / group.MaxBundleCount);
            }

            uint hubId = ResolveSettlerHubId(pathSettlerHubOrImprovementGroupId);
            return hubId != 0u ? GetSettlerHubMissionProgressPercent(hubId) : 0u;
        }

        public uint GetSettlerHubOverallProgressPercent()
        {
            if (settlerImprovementGroupStatus.Count > 0)
            {
                uint total = 0u;
                foreach (KeyValuePair<uint, SettlerImprovementGroupRuntimeStatus> entry in settlerImprovementGroupStatus)
                {
                    total += GetSettlerHubBuildProgressPercent(entry.Key);
                }

                return total / (uint)settlerImprovementGroupStatus.Count;
            }

            uint maxPercent = 0u;
            HashSet<uint> hubIds = [];
            if (gameTableManager.PathSettlerHub?.Entries != null)
            {
                foreach (PathSettlerHubEntry hub in gameTableManager.PathSettlerHub.Entries)
                    hubIds.Add(hub.Id);
            }

            foreach (uint hubId in hubIds)
                maxPercent = Math.Max(maxPercent, GetSettlerHubMissionProgressPercent(hubId));

            return maxPercent;
        }

        private uint GetSettlerHubMissionProgressPercent(uint pathSettlerHubId)
        {
            PathSettlerHubEntry hub = gameTableManager.PathSettlerHub?.GetEntry(pathSettlerHubId);
            if (hub == null)
                return 0u;

            uint requiredCount = Math.Max(hub.MissionCount, 1u);
            foreach (PathMissionRuntimeState state in pathMissions.Values)
            {
                PathMissionEntry mission = gameTableManager.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Settler
                    || mission.PathMissionTypeEnum != SettlerHubMissionType
                    || mission.ObjectId != pathSettlerHubId)
                {
                    continue;
                }

                return Math.Min(100u, state.ProgressCount * 100u / requiredCount);
            }

            return 0u;
        }

        private uint ResolveSettlerHubId(uint objectId)
        {
            if (objectId == 0u)
                return 0u;

            if (gameTableManager.PathSettlerHub?.GetEntry(objectId) != null)
                return objectId;

            PathSettlerImprovementGroupEntry group = gameTableManager.PathSettlerImprovementGroup?.GetEntry(objectId);
            return group?.PathSettlerHubId ?? 0u;
        }

        private uint ResolveSettlerImprovementGroupId(uint objectId)
        {
            if (objectId == 0u)
                return 0u;

            return gameTableManager.PathSettlerImprovementGroup?.GetEntry(objectId) != null
                ? objectId
                : 0u;
        }

        private static uint PercentFromTier(int tier)
        {
            if (tier <= 0)
                return 0u;

            return Math.Min(100u, (uint)tier * 100u / SettlerImprovementMaxTier);
        }

        private readonly record struct SettlerImprovementGroupRuntimeStatus(int Tier, uint BundleCount);

        /// <summary>
        /// Get the current <see cref="Path"/> level for the <see cref="IPlayer"/>.
        /// </summary>
        private bool TryGetCurrentLevel(Path path, out uint level)
        {
            level = 0u;
            if (gameTableManager.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(TryGetCurrentLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve current path level.");
                return false;
            }

            PathLevelEntry entry = gameTableManager.PathLevel.Entries
                .LastOrDefault(x => x.PathXP <= paths[path].TotalXp && x.PathTypeEnum == (uint)path);
            if (entry == null)
            {
                MissingGameDataDiagnostics.ReportMissingRow(
                    PathLevelTableName,
                    path,
                    nameof(PathManager) + "." + nameof(TryGetCurrentLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve current path level.");
                return false;
            }

            level = entry.PathLevel;
            return true;
        }

        private bool TryGetPathXpForLevel(Path path, uint level, out uint xp)
        {
            xp = 0u;
            if (gameTableManager.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(TryGetPathXpForLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve path XP target.");
                return false;
            }

            PathLevelEntry entry = gameTableManager.PathLevel.Entries
                .LastOrDefault(x => x.PathLevel == level && x.PathTypeEnum == (uint)path);
            if (entry == null)
            {
                MissingGameDataDiagnostics.ReportMissingRow(
                    PathLevelTableName,
                    $"{path}:{level}",
                    nameof(PathManager) + "." + nameof(TryGetPathXpForLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve path XP target.");
                return false;
            }

            xp = entry.PathXP;
            return true;
        }

        private void GrantOutstandingLevelRewards(Path path, uint totalXp)
        {
            IPathEntry entry = GetPathEntry(path);
            if (entry.LevelRewarded >= MaxPathLevel)
                return;

            byte rewardedLevel = entry.LevelRewarded;
            if (gameTableManager.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(GrantOutstandingLevelRewards),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot grant outstanding path level rewards.");
                return;
            }

            IEnumerable<PathLevelEntry> pathLevelEntries = gameTableManager.PathLevel.Entries;
            foreach (uint level in pathLevelEntries
                .Where(x => x.PathTypeEnum == (uint)path
                    && x.PathLevel > rewardedLevel
                    && x.PathXP <= totalXp)
                .OrderBy(x => x.PathLevel)
                .Select(e => e.PathLevel))
                GrantLevelUpReward(path, level);
        }

        /// <summary>
        /// Grants a player a level up reward for a <see cref="Path"/> and level
        /// </summary>
        /// <param name="path">The path to grant the reward for</param>
        /// <param name="level">The level to grant the reward for</param>
        private void GrantLevelUpReward(Path path, uint level)
        {
            uint pathRewardObjectId = PathRewardGrant.GetLevelRewardObjectId(path, level);

            IEnumerable<PathRewardEntry> pathRewardEntries;
            if (gameTableManager.PathReward?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathRewardTableName,
                    nameof(PathManager) + "." + nameof(GrantLevelUpReward),
                    MissingGameDataSeverity.PlayerImpacting,
                    $"Cannot grant level reward for path={path} level={level}.");
                pathRewardEntries = [];
            }
            else
            {
                pathRewardEntries = gameTableManager.PathReward.Entries
                    .Where(x => x.ObjectId == pathRewardObjectId);
            }
            foreach (PathRewardEntry pathRewardEntry in pathRewardEntries)
            {
                if (!PathRewardGrant.IsGrantableLevelReward(pathRewardEntry))
                    continue;

                if (pathRewardEntry.PrerequisiteId > 0 && !GetPrerequisiteManager().Meets(player, pathRewardEntry.PrerequisiteId))
                    continue;

                GrantPathReward(pathRewardEntry);
            }

            GetPathEntry(path).LevelRewarded = (byte)level;
            player.AchievementManager.SetAchievementProgress(player, AchievementType.PathLevel, (uint)path, 0u, level);
            if (level >= MaxPathLevel)
                player.AchievementManager.CheckAchievements(player, AchievementType.GuildMaxPathLevel, 0u);
            player.CastSpell(53234, new Spell.SpellParameters());
        }

        private void GrantMissionRewards(ushort pathMissionId)
        {
            if (gameTableManager.PathReward?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathRewardTableName,
                    nameof(PathManager) + "." + nameof(GrantMissionRewards),
                    MissingGameDataSeverity.PlayerImpacting,
                    $"Cannot grant mission reward for pathMissionId={pathMissionId}.");
                return;
            }

            foreach (PathRewardEntry pathRewardEntry in gameTableManager.PathReward.Entries
                .Where(x => x.ObjectId == pathMissionId))
            {
                if (!PathRewardGrant.IsGrantableMissionReward(pathRewardEntry, pathMissionId))
                    continue;

                if (pathRewardEntry.PrerequisiteId > 0 && !GetPrerequisiteManager().Meets(player, pathRewardEntry.PrerequisiteId))
                    continue;

                // WIP/GUESSED: LaughingWS grants PathRewardType.Mission rows on path mission
                // completion. Exact reward presentation, item-count handling, and flagged reward
                // semantics remain blocked, so this only grants currently-supported unflagged rows.
                GrantPathReward(pathRewardEntry);
            }
        }

        private void GrantEpisodeRewardsIfComplete(ushort pathEpisodeId)
        {
            if (pathEpisodeId == 0u)
                return;

            List<PathMissionRuntimeState> episodeMissions = pathMissions.Values
                .Where(m => m.EpisodeId == pathEpisodeId)
                .ToList();
            if (episodeMissions.Count == 0 || episodeMissions.Any(m => !m.Completed))
                return;

            if (gameTableManager.PathReward?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathRewardTableName,
                    nameof(PathManager) + "." + nameof(GrantEpisodeRewardsIfComplete),
                    MissingGameDataSeverity.PlayerImpacting,
                    $"Cannot grant episode reward for pathEpisodeId={pathEpisodeId}.");
                return;
            }

            foreach (PathRewardEntry pathRewardEntry in gameTableManager.PathReward.Entries
                .Where(x => x.ObjectId == pathEpisodeId))
            {
                if (!PathRewardGrant.IsGrantableEpisodeReward(pathRewardEntry, pathEpisodeId))
                    continue;

                if (pathRewardEntry.PrerequisiteId > 0 && !GetPrerequisiteManager().Meets(player, pathRewardEntry.PrerequisiteId))
                    continue;

                GrantPathReward(pathRewardEntry);
            }
        }

        /// <summary>
        /// Grant the <see cref="IPlayer"/> rewards from the <see cref="PathRewardEntry"/>
        /// </summary>
        /// <param name="pathRewardEntry">The entry containing items, spells, or titles, to be rewarded"/></param>
        private void GrantPathReward(PathRewardEntry pathRewardEntry)
        {
            if (pathRewardEntry == null)
                throw new ArgumentNullException();

            if (pathRewardEntry.Item2Id > 0)
            {
                // WIP/GUESSED: PathReward.Count is the only current table field that can
                // carry an item quantity. Treat zero as the legacy single-item fallback
                // until retail reward presentation and overflow semantics are mapped.
                uint itemCount = pathRewardEntry.Count > 0u ? pathRewardEntry.Count : 1u;
                player.Inventory.ItemCreate(InventoryLocation.Inventory, pathRewardEntry.Item2Id, itemCount, ItemUpdateReason.PathReward);
            }

            if (pathRewardEntry.Spell4Id > 0)
            {
                Spell4Entry spell4Entry = gameTableManager.Spell4?.GetEntry(pathRewardEntry.Spell4Id);
                if (spell4Entry != null)
                    player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);
                else
                    MissingGameDataDiagnostics.ReportSkippedGrant(
                        "Path spell reward",
                        Spell4TableName,
                        pathRewardEntry.Spell4Id,
                        nameof(PathManager) + "." + nameof(GrantPathReward),
                        $"pathRewardId={pathRewardEntry.Id}");
            }

            if (pathRewardEntry.CharacterTitleId > 0)
            {
                CharacterTitleEntry titleEntry = gameTableManager.CharacterTitle?.GetEntry(pathRewardEntry.CharacterTitleId);
                if (titleEntry != null)
                    player.TitleManager.AddTitle((ushort)titleEntry.Id);
                else
                    MissingGameDataDiagnostics.ReportSkippedGrant(
                        "Path title reward",
                        CharacterTitleTableName,
                        pathRewardEntry.CharacterTitleId,
                        nameof(PathManager) + "." + nameof(GrantPathReward),
                        $"pathRewardId={pathRewardEntry.Id}");
            }

            if (pathRewardEntry.PathScientistScanBotProfileId > 0)
            {
                PathScientistScanBotProfileEntry scanBotProfileEntry = gameTableManager.PathScientistScanBotProfile?.GetEntry(pathRewardEntry.PathScientistScanBotProfileId);
                if (scanBotProfileEntry != null)
                    player.PetCustomisationManager.UnlockScanBotProfile(scanBotProfileEntry.Id);
                else
                    MissingGameDataDiagnostics.ReportSkippedGrant(
                        "Path scanbot profile reward",
                        PathScientistScanBotProfileTableName,
                        pathRewardEntry.PathScientistScanBotProfileId,
                        nameof(PathManager) + "." + nameof(GrantPathReward),
                        $"pathRewardId={pathRewardEntry.Id}");
            }
        }

        private PathUnlockedMask GetPathUnlockedMask()
        {
            PathUnlockedMask mask = PathUnlockedMask.None;
            foreach (IPathEntry entry in paths.Values)
                if (entry.Unlocked)
                    mask |= (PathUnlockedMask)(1 << (int)entry.Path);

            return mask;
        }

        /// <summary>
        /// Execute a DB Save of the <see cref="CharacterContext"/>
        /// </summary>
        public void Save(CharacterContext context)
        {
            foreach (IPathEntry pathEntry in paths.Values)
                pathEntry.Save(context);

            PathEntry.UpsertTrackedCreates(context);

            foreach (PathMissionRuntimeState state in pathMissions.Values)
                state.Save(context);
        }

        public void SendInitialPackets()
        {
            SendPathLogPacket();
            if (initialPacketsSent)
                return;

            initialPacketsSent = true;
            ReplayCurrentZoneEpisode(allowWhileLoading: true);
        }

        private bool ReplayCurrentZoneEpisode(bool allowWhileLoading)
        {
            activatedEpisodes.Clear();
            activatedMissions.Clear();
            WorldZoneEntry zone = player.Zone;
            return zone != null && TryActivateCurrentZoneEpisode(zone.Id, allowWhileLoading);
        }

        /// <summary>
        /// Used to update the Player's Path Log.
        /// </summary>
        private void SendPathLogPacket()
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathInitialise
            {
                ActivePath                  = player.Path,
                PathProgress                = paths.Values.Select(p => p.TotalXp).ToArray(),
                PathUnlockedMask            = GetPathUnlockedMask(),
                TimeSinceLastActivateInDays = GetCooldownTime()
            });
        }

        private float GetCooldownTime()
        {
            return (float)DateTime.UtcNow.Subtract(player.PathActivatedTime).TotalDays * -1;
        }

        private WorldZoneEntry GetMostParentZone(WorldZoneEntry zone)
        {
            WorldZoneEntry currentZone = zone;
            for (int i = 0; i < 32 && currentZone?.ParentZoneId > 0u; i++)
            {
                WorldZoneEntry parentZone = gameTableManager.WorldZone.GetEntry(currentZone.ParentZoneId);
                if (parentZone == null)
                    break;

                currentZone = parentZone;
            }

            return currentZone;
        }

        private IReadOnlyList<uint> GetCurrentAndAncestorZoneIds(uint worldZoneId)
        {
            if (worldZoneId == 0u)
                return [];

            List<uint> zoneIds = [];
            uint currentZoneId = worldZoneId;
            for (int i = 0; i < 32 && currentZoneId != 0u; i++)
            {
                if (zoneIds.Contains(currentZoneId))
                    break;

                zoneIds.Add(currentZoneId);
                WorldZoneEntry currentZone = gameTableManager.WorldZone?.GetEntry(currentZoneId);
                if (currentZone == null || currentZone.ParentZoneId == 0u)
                    break;

                currentZoneId = currentZone.ParentZoneId;
            }

            return zoneIds;
        }

        private IReadOnlySet<ushort> GetReviewedRuntimePathMissionIds(uint worldId, IReadOnlyList<uint> zoneIds)
        {
            HashSet<ushort> missionIds = [];
            foreach (uint zoneId in zoneIds)
                if (ReviewedRuntimePathMissionZoneBridges.TryGetValue((worldId, zoneId, player.Path), out IReadOnlySet<ushort> reviewedMissionIds))
                    missionIds.UnionWith(reviewedMissionIds);

            return missionIds;
        }

        private bool IsMissionLocationInZone(uint worldId, PathMissionEntry mission, uint currentZoneId, IReadOnlyList<uint> currentZoneIds)
        {
            if (gameTableManager.WorldLocation2 == null)
                return false;

            foreach (uint worldLocationId in GetMissionWorldLocationIds(mission))
            {
                WorldLocation2Entry location = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
                if (location == null || location.WorldId != worldId || location.WorldZoneId == 0u)
                    continue;

                if (currentZoneIds.Contains(location.WorldZoneId))
                    return true;

                IReadOnlyList<uint> locationZoneIds = GetCurrentAndAncestorZoneIds(location.WorldZoneId);
                if (locationZoneIds.Contains(currentZoneId))
                    return true;
            }

            return false;
        }

        private static IEnumerable<uint> GetMissionWorldLocationIds(PathMissionEntry mission)
        {
            if (mission.WorldLocation2Id00 != 0u)
                yield return mission.WorldLocation2Id00;
            if (mission.WorldLocation2Id01 != 0u)
                yield return mission.WorldLocation2Id01;
            if (mission.WorldLocation2Id02 != 0u)
                yield return mission.WorldLocation2Id02;
            if (mission.WorldLocation2Id03 != 0u)
                yield return mission.WorldLocation2Id03;
        }

        private static bool HasMissionWorldLocation(PathMissionEntry mission)
        {
            return mission.WorldLocation2Id00 != 0u
                || mission.WorldLocation2Id01 != 0u
                || mission.WorldLocation2Id02 != 0u
                || mission.WorldLocation2Id03 != 0u;
        }

        private bool IsCurrentOrAncestorZone(uint worldZoneId)
        {
            if (worldZoneId == 0u)
                return false;

            WorldZoneEntry currentZone = player.Zone;
            for (int i = 0; i < 32 && currentZone != null; i++)
            {
                if (currentZone.Id == worldZoneId)
                    return true;

                if (currentZone.ParentZoneId == 0u)
                    break;

                currentZone = gameTableManager.WorldZone?.GetEntry(currentZone.ParentZoneId);
            }

            return false;
        }

        private uint ResolveCurrentMapZoneId()
        {
            WorldZoneEntry worldZoneEntry = player.Zone;
            for (int i = 0; i < 32 && worldZoneEntry != null; i++)
            {
                MapZoneEntry zoneMap = gameTableManager.MapZone?.Entries?
                    .FirstOrDefault(m => m.WorldZoneId == worldZoneEntry.Id);
                if (zoneMap != null)
                    return zoneMap.Id;

                if (worldZoneEntry.ParentZoneId == 0u)
                    break;

                worldZoneEntry = gameTableManager.WorldZone?.GetEntry(worldZoneEntry.ParentZoneId);
            }

            uint worldId = player.Map?.Entry?.Id ?? 0u;
            if (worldId == 0u)
                return 0u;

            return gameTableManager.MapZoneWorldJoin?.Entries?
                .FirstOrDefault(m => m.WorldId == worldId)?.MapZoneId ?? 0u;
        }

        private bool IsMissionFactionAllowed(PathMissionEntry mission)
        {
            return mission.PathMissionFactionEnum switch
            {
                0u => true,
                1u => player.Faction1 == Faction.Exile,
                2u => player.Faction1 == Faction.Dominion,
                _  => false
            };
        }

        private static bool MatchesSoldierAssassinateKill(PathSoldierAssassinateEntry assassinate, uint creature2Id, IReadOnlyCollection<uint> targetGroupIds)
        {
            if (assassinate == null)
                return false;

            if (assassinate.Creature2Id != 0u && assassinate.Creature2Id == creature2Id)
                return true;

            return assassinate.TargetGroupId != 0u && targetGroupIds.Contains(assassinate.TargetGroupId);
        }

        private bool IsMissionPrerequisiteAllowed(PathMissionEntry mission)
        {
            if (mission.PrerequisiteId == 0u)
                return true;

            if (gameTableManager.Prerequisite?.GetEntry(mission.PrerequisiteId) == null)
                return false;

            // WIP/GUESSED: client labels map path mission visibility to path, faction,
            // and optional prerequisite checks. This applies the table prerequisite gate
            // during current-zone activation, while exact unlock sequencing remains blocked.
            if (TryEvaluateSimplePathPrerequisite(mission.PrerequisiteId, out bool allowed))
                return allowed;

            return GetPrerequisiteManager().Meets(player, mission.PrerequisiteId);
        }

        private bool IsZoneEntryActivationAllowed(PathMissionEntry mission)
        {
            // Soldier holdout missions are advertised by the current episode, but the
            // mission runtime starts when the player interacts with the holdout beacon.
            return player.Path != Path.Soldier
                || mission.PathMissionTypeEnum != SoldierHoldoutMissionType;
        }

        private IPrerequisiteManager GetPrerequisiteManager()
        {
            return prerequisiteManager ?? throw new InvalidOperationException($"{nameof(PathManager)} requires an {nameof(IPrerequisiteManager)}.");
        }

        private bool TryEvaluateSimplePathPrerequisite(uint prerequisiteId, out bool allowed)
        {
            allowed = false;

            PrerequisiteEntry entry = gameTableManager.Prerequisite?.GetEntry(prerequisiteId);
            if (entry == null || entry.Flags != EvaluationMode.EvaluateAND)
                return false;

            bool evaluated = false;
            for (int i = 0; i < entry.PrerequisiteTypeId.Length; i++)
            {
                PrerequisiteType type = entry.PrerequisiteTypeId[i];
                if (type == PrerequisiteType.None)
                    continue;

                if (type != PrerequisiteType.Path || entry.PrerequisiteComparisonId[i] != PrerequisiteComparison.Equal)
                    return false;

                evaluated = true;
                if (player.Path != (Path)entry.Value[i])
                    return true;
            }

            allowed = evaluated;
            return evaluated;
        }

        /// <summary>
        /// Used to tell the world (and the player) which Path Type this Player is.
        /// </summary>
        public void SendSetUnitPathTypePacket()
        {
            player.EnqueueToVisible(new ServerSetUnitPathType
            {
                UnitId = player.Guid,
                Path = player.Path,
            }, true);
        }

        /// <summary>
        /// Sends a response to the player's <see cref="Path"/> activate request
        /// </summary>
        /// <param name="result">Used for success or error values</param>
        public void SendServerPathActivateResult(GenericError result = GenericError.Ok)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathChangeResult
            {
                Result = result
            });
        }

        /// <summary>
        /// Sends a response to the player's request for unlocking a <see cref="Path"/>
        /// </summary>
        /// <param name="result">Used for success or error values</param>
        public void SendServerPathUnlockResult(GenericError result = GenericError.Ok)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathUnlockResult
            {
                Result           = result,
                UnlockedPathMask = GetPathUnlockedMask()
            });
        }

        /// <summary>
        /// Sends total XP for the activate path to the player
        /// </summary>
        /// <param name="totalXp">Total Path XP to be sent</param>
        private void SendServerPathUpdateXp(uint totalXp)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathUpdateXP
            {
                TotalXP = totalXp
            });
        }

        private PathMissionRuntimeState GetOrCreateMissionState(ushort pathMissionId)
        {
            if (pathMissions.TryGetValue(pathMissionId, out PathMissionRuntimeState state))
                return state;

            PathMissionEntry entry = gameTableManager.PathMission?.GetEntry(pathMissionId);
            state = new PathMissionRuntimeState(player.CharacterId, pathMissionId, (ushort)(entry?.PathEpisodeId ?? 0u));
            state.State = PathMissionState.Started;
            pathMissions.Add(pathMissionId, state);
            return state;
        }

        private void SendPathCurrentEpisode(ushort episodeId)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathSetCurrentEpisode
            {
                PathEpisodeId = episodeId
            });
        }

        private void SendPathCurrentEpisodeIfNeeded(ushort episodeId)
        {
            if (!activatedEpisodes.Add(episodeId))
                return;

            SendPathCurrentEpisode(episodeId);
        }

        private void SendPathEpisodeProgress(ushort episodeId, IReadOnlySet<ushort> missionIds = null)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathEpisodeProgress
            {
                EpisodeId = episodeId,
                Missions = pathMissions.Values
                    .Where(m => m.EpisodeId == episodeId)
                    .Where(m => missionIds == null || missionIds.Contains(m.MissionId))
                    .OrderBy(m => m.MissionId)
                    .Select(BuildMission)
                    .ToList()
            });
        }

        private void SendPathMissionActivate(ushort episodeId, IReadOnlySet<ushort> missionIds = null)
        {
            List<Mission> missions = pathMissions.Values
                .Where(m => m.EpisodeId == episodeId && !m.Completed)
                .Where(m => missionIds == null || missionIds.Contains(m.MissionId))
                .OrderBy(m => m.MissionId)
                .Select(BuildMission)
                .ToList();
            if (missions.Count == 0)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerPathMissionActivate
            {
                Missions = missions
            });
        }

        private static Mission BuildMission(PathMissionRuntimeState state)
        {
            return new Mission
            {
                PathMissionId = state.MissionId,
                Completed = state.Completed,
                ProgressCount = state.ProgressCount,
                ProgressData = state.ProgressData,
                State = state.State,
                GiverUnitId = state.GiverUnitId
            };
        }

        private IPathEntry GetPathEntry(Path path)
        {
            paths.TryGetValue(path, out IPathEntry pathEntry);
            return pathEntry;
        }

        private void SetPathEntry(Path path, IPathEntry entry)
        {
            paths[path] = entry;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IPathEntry> GetEnumerator()
        {
            return paths.Values.GetEnumerator();
        }

        private sealed class PathMissionRuntimeState
        {
            [Flags]
            private enum SaveMask
            {
                None          = 0x0000,
                Create        = 0x0001,
                Episode       = 0x0002,
                Xp            = 0x0004,
                Completed     = 0x0008,
                ProgressCount = 0x0010,
                ProgressData  = 0x0020,
                State         = 0x0040
            }

            public ulong CharacterId { get; }
            public ushort EpisodeId
            {
                get => episodeId;
                set => SetField(ref episodeId, value, SaveMask.Episode);
            }

            public ushort MissionId { get; }

            public uint Xp
            {
                get => xp;
                set => SetField(ref xp, value, SaveMask.Xp);
            }

            public bool Completed
            {
                get => completed;
                set => SetField(ref completed, value, SaveMask.Completed);
            }

            public uint ProgressCount
            {
                get => progressCount;
                set => SetField(ref progressCount, value, SaveMask.ProgressCount);
            }

            public uint ProgressData
            {
                get => progressData;
                set => SetField(ref progressData, value, SaveMask.ProgressData);
            }

            public PathMissionState State
            {
                get => state;
                set => SetField(ref state, value, SaveMask.State);
            }

            public uint GiverUnitId { get; init; }

            private ushort episodeId;
            private uint xp;
            private bool completed;
            private uint progressCount;
            private uint progressData;
            private PathMissionState state = PathMissionState.Started;
            private SaveMask saveMask;

            public PathMissionRuntimeState(ulong characterId, ushort missionId, ushort episodeId)
            {
                CharacterId = characterId;
                MissionId   = missionId;

                this.episodeId = episodeId;
                saveMask       = SaveMask.Create;
            }

            public PathMissionRuntimeState(CharacterPathMissionModel model)
            {
                CharacterId   = model.Id;
                MissionId     = model.PathMissionId;
                episodeId     = model.PathEpisodeId;
                state         = (PathMissionState)model.State;
                completed     = Convert.ToBoolean(model.Completed);
                progressCount = model.ProgressCount;
                progressData  = model.ProgressData;
                xp            = model.Xp;

                saveMask = SaveMask.None;
            }

            public void Save(CharacterContext context)
            {
                if (saveMask == SaveMask.None)
                    return;

                CharacterPathMissionModel model = BuildModel();
                if ((saveMask & SaveMask.Create) != 0)
                {
                    context.Add(model);
                }
                else
                {
                    EntityEntry<CharacterPathMissionModel> entity = context.Attach(model);
                    if ((saveMask & SaveMask.Episode) != 0)
                        entity.Property(p => p.PathEpisodeId).IsModified = true;

                    if ((saveMask & SaveMask.Xp) != 0)
                        entity.Property(p => p.Xp).IsModified = true;

                    if ((saveMask & SaveMask.Completed) != 0)
                        entity.Property(p => p.Completed).IsModified = true;

                    if ((saveMask & SaveMask.ProgressCount) != 0)
                        entity.Property(p => p.ProgressCount).IsModified = true;

                    if ((saveMask & SaveMask.ProgressData) != 0)
                        entity.Property(p => p.ProgressData).IsModified = true;

                    if ((saveMask & SaveMask.State) != 0)
                        entity.Property(p => p.State).IsModified = true;
                }

                saveMask = SaveMask.None;
            }

            private CharacterPathMissionModel BuildModel()
            {
                return new CharacterPathMissionModel
                {
                    Id             = CharacterId,
                    PathMissionId  = MissionId,
                    PathEpisodeId  = EpisodeId,
                    State          = (byte)State,
                    Completed      = Convert.ToByte(Completed),
                    ProgressCount  = ProgressCount,
                    ProgressData   = ProgressData,
                    Xp             = Xp
                };
            }

            private void SetField<T>(ref T field, T value, SaveMask mask)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;

                field = value;
                saveMask |= mask;
            }
        }
    }
}
