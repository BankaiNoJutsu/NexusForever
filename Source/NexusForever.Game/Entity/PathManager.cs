using System.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
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
        private const uint SoldierAssassinateMissionType = 0x0004;
        private const uint ExplorerVistaMissionType = 0x000F;
        private const uint ExplorerExploreZoneMissionType = 0x0010;
        private const uint ExplorerPowerMapMissionType = 0x0012;
        private const uint SettlerHubMissionType = 0x0013;
        private const uint SettlerInfrastructureMissionType = 0x0015;
        private const uint SettlerImprovementMaxTier = 4u;
        private const uint PathMissionCompletionXpGameFormulaId = 0x017Au;
        private const uint DefaultMissionCompletionXp = 50u;
        private const string PathLevelTableName = "PathLevel.tbl";
        private const string PathRewardTableName = "PathReward.tbl";
        private const string Spell4TableName = "Spell4.tbl";
        private const string CharacterTitleTableName = "CharacterTitle.tbl";
        private const string PathScientistScanBotProfileTableName = "PathScientistScanBotProfile.tbl";

        private readonly IPlayer player;
        private readonly Dictionary<Path, IPathEntry> paths = new();
        private readonly Dictionary<ushort, PathMissionRuntimeState> pathMissions = [];
        private readonly HashSet<ushort> activatedEpisodes = [];
        private readonly Dictionary<uint, SettlerImprovementGroupRuntimeStatus> settlerImprovementGroupStatus = new();

        /// <summary>
        /// Create a new <see cref="IPathManager"/> from <see cref="IPlayer"/> database model.
        /// </summary>
        public PathManager(IPlayer owner, CharacterModel model)
        {
            player = owner;
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
            foreach (KeyValuePair<ushort, uint> mapping in PathScientistPrerequisiteHelper.MissionToCreatureInfoId)
            {
                if (IsMissionComplete(mapping.Key))
                    MarkScientistCreatureScanned(mapping.Value);
            }
        }

        private void TryMarkScientistScanFromMission(ushort pathMissionId)
        {
            if (PathScientistPrerequisiteHelper.MissionToCreatureInfoId.TryGetValue(pathMissionId, out uint creatureInfoId))
                MarkScientistCreatureScanned(creatureInfoId);
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

            player.Path = pathToActivate;

            SendServerPathActivateResult(GenericError.Ok);
            SendSetUnitPathTypePacket();
            SendPathLogPacket();
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
            if (episodeId == 0 || missionXp == null || missionXp.Count == 0)
                return;

            HashSet<ushort> activatedMissionIds = [];
            foreach ((ushort missionId, uint xp) in missionXp)
            {
                if (!pathMissions.TryGetValue(missionId, out PathMissionRuntimeState state))
                {
                    state = new PathMissionRuntimeState(player.CharacterId, missionId, episodeId);
                    pathMissions.Add(missionId, state);
                    activatedMissionIds.Add(missionId);
                }

                state.EpisodeId = episodeId;
                state.Xp = xp;
                if (!state.Completed)
                    state.State = PathMissionState.Started;
            }

            if (activatedMissionIds.Count == 0)
                return;

            if (!activatedEpisodes.Add(episodeId))
                return;

            SendPathCurrentEpisode(episodeId);
            SendPathEpisodeProgress(episodeId, missionIds: activatedMissionIds);
            SendPathMissionActivate(episodeId, missionIds: activatedMissionIds);
        }

        public bool TryActivateCurrentZoneEpisode()
        {
            uint worldId = player.Map?.Entry?.Id ?? 0u;
            WorldZoneEntry zone = player.Zone;
            if (worldId == 0u || zone == null)
                return false;

            WorldZoneEntry rootZone = GetMostParentZone(zone);
            if (rootZone == null)
                return false;

            if (GameTableManager.Instance.PathEpisode?.Entries == null
                || GameTableManager.Instance.PathMission?.Entries == null)
                return false;

            PathEpisodeEntry pathEpisode = GameTableManager.Instance.PathEpisode.Entries
                .FirstOrDefault(e => e.WorldId == worldId
                    && e.WorldZoneId == rootZone.Id
                    && e.PathTypeEnum == (uint)player.Path);
            if (pathEpisode == null || pathEpisode.Id > 0x3FFFu)
                return false;

            Dictionary<ushort, uint> missions = GameTableManager.Instance.PathMission.Entries
                .Where(m => m.PathEpisodeId == pathEpisode.Id
                    && m.PathTypeEnum == (uint)player.Path
                    && IsMissionFactionAllowed(m)
                    && IsMissionPrerequisiteAllowed(m)
                    && m.Id <= 0x7FFFu)
                .OrderBy(m => m.Id)
                .ToDictionary(m => (ushort)m.Id, _ => 0u);
            if (missions.Count == 0)
                return false;

            // WIP/GUESSED: LaughingWS activated the current PathEpisode on zone changes.
            // This keeps the safe table-backed episode/mission surface, but leaves durable
            // path persistence, exact per-mission reward precision, and broader
            // unlock sequencing blocked.
            ActivateMissions((ushort)pathEpisode.Id, missions);
            return true;
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

            PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(pathMissionId);
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
            TryMarkScientistScanFromMission(pathMissionId);

            return true;
        }

        private static uint GetMissionCompletionXp(PathMissionRuntimeState state, PathMissionEntry mission, Path activePath)
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
            GameFormulaEntry formula = GameTableManager.Instance.GameFormula?.GetEntry(PathMissionCompletionXpGameFormulaId);
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

            PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(pathMissionId);
            if (mission == null
                || mission.PathTypeEnum != (uint)Path.Explorer
                || mission.PathMissionTypeEnum != ExplorerVistaMissionType)
                return false;

            bool hasExplorerNode = GameTableManager.Instance.PathExplorerNode?.Entries
                .Any(n => n.PathExplorerAreaId == mission.ObjectId) ?? false;
            if (!hasExplorerNode)
                return false;

            return CompleteMission(pathMissionId);
        }

        public bool CompleteExplorerPowerMapMission(uint pathExplorerPowerMapId)
        {
            if (pathExplorerPowerMapId == 0u)
                return false;

            if (GameTableManager.Instance.PathExplorerPowerMap?.GetEntry(pathExplorerPowerMapId) == null)
                return false;

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
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

            bool completedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Explorer
                    || mission.PathMissionTypeEnum != ExplorerExploreZoneMissionType
                    || mission.ObjectId != mapZoneId)
                    continue;

                // WIP/GUESSED: LaughingWS marks Explorer_ExploreZone as ObjectId == MapZone.Id.
                // Current server evidence is enough for active-only zone-entry completion, but
                // durable path progress, exact reward timing, and non-map-zone edge cases remain blocked.
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
                PathMissionEntry entry = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
                if (entry?.ObjectId != objectId)
                    continue;

                if (entry.PathTypeEnum != (uint)player.Path)
                    continue;

                // WIP/GUESSED: current path runtime is session-local, so object-id completion
                // is limited to active-path rows. This mirrors the client visibility path gate
                // while durable path episode/mission ownership remains blocked.
                completedAny |= CompleteMission(state.MissionId);
            }

            return completedAny;
        }

        public bool CompleteMissionBySoldierTowerDefenseId(uint pathSoldierTowerDefenseId)
        {
            if (pathSoldierTowerDefenseId == 0u)
                return false;

            PathSoldierTowerDefenseEntry entry = GameTableManager.Instance.PathSoldierTowerDefense?.GetEntry(pathSoldierTowerDefenseId);
            return entry != null && CompleteMissionByObjectId(entry.PathSoldierEventId);
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

                PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
                if (mission == null
                    || mission.PathTypeEnum != (uint)Path.Soldier
                    || mission.PathMissionTypeEnum != SoldierAssassinateMissionType)
                    continue;

                PathSoldierAssassinateEntry assassinate = GameTableManager.Instance.PathSoldierAssassinate?.GetEntry(mission.ObjectId);
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

        public bool CompleteMissionBySettlerImprovementGroupId(uint pathSettlerImprovementGroupId)
        {
            if (player.Path != Path.Settler)
                return false;

            if (pathSettlerImprovementGroupId == 0u)
                return false;

            PathSettlerImprovementGroupEntry entry = GameTableManager.Instance.PathSettlerImprovementGroup?.GetEntry(pathSettlerImprovementGroupId);
            if (entry == null || entry.PathSettlerHubId == 0u)
                return false;

            PathSettlerHubEntry hub = GameTableManager.Instance.PathSettlerHub?.GetEntry(entry.PathSettlerHubId);
            if (hub == null)
                return false;

            bool progressedAny = false;
            foreach (PathMissionRuntimeState state in pathMissions.Values.ToList())
            {
                if (state.Completed)
                    continue;

                PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
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
            if (GameTableManager.Instance.PathSettlerInfrastructure?.GetEntry(pathSettlerInfrastructureId) == null
                || GameTableManager.Instance.PathMission?.Entries == null)
            {
                return SettlerInfrastructureState.Inactive;
            }

            PathMissionEntry mission = GameTableManager.Instance.PathMission.Entries
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
                PathSettlerImprovementGroupEntry group = GameTableManager.Instance.PathSettlerImprovementGroup?.GetEntry(improvementGroupId);
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
            if (GameTableManager.Instance.PathSettlerHub?.Entries != null)
            {
                foreach (PathSettlerHubEntry hub in GameTableManager.Instance.PathSettlerHub.Entries)
                    hubIds.Add(hub.Id);
            }

            foreach (uint hubId in hubIds)
                maxPercent = Math.Max(maxPercent, GetSettlerHubMissionProgressPercent(hubId));

            return maxPercent;
        }

        private uint GetSettlerHubMissionProgressPercent(uint pathSettlerHubId)
        {
            PathSettlerHubEntry hub = GameTableManager.Instance.PathSettlerHub?.GetEntry(pathSettlerHubId);
            if (hub == null)
                return 0u;

            uint requiredCount = Math.Max(hub.MissionCount, 1u);
            foreach (PathMissionRuntimeState state in pathMissions.Values)
            {
                PathMissionEntry mission = GameTableManager.Instance.PathMission?.GetEntry(state.MissionId);
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

        private static uint ResolveSettlerHubId(uint objectId)
        {
            if (objectId == 0u)
                return 0u;

            if (GameTableManager.Instance.PathSettlerHub?.GetEntry(objectId) != null)
                return objectId;

            PathSettlerImprovementGroupEntry group = GameTableManager.Instance.PathSettlerImprovementGroup?.GetEntry(objectId);
            return group?.PathSettlerHubId ?? 0u;
        }

        private static uint ResolveSettlerImprovementGroupId(uint objectId)
        {
            if (objectId == 0u)
                return 0u;

            return GameTableManager.Instance.PathSettlerImprovementGroup?.GetEntry(objectId) != null
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
            if (GameTableManager.Instance.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(TryGetCurrentLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve current path level.");
                return false;
            }

            PathLevelEntry entry = GameTableManager.Instance.PathLevel.Entries
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
            if (GameTableManager.Instance.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(TryGetPathXpForLevel),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot resolve path XP target.");
                return false;
            }

            PathLevelEntry entry = GameTableManager.Instance.PathLevel.Entries
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
            if (GameTableManager.Instance.PathLevel?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathLevelTableName,
                    nameof(PathManager) + "." + nameof(GrantOutstandingLevelRewards),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot grant outstanding path level rewards.");
                return;
            }

            IEnumerable<PathLevelEntry> pathLevelEntries = GameTableManager.Instance.PathLevel.Entries;
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
            if (GameTableManager.Instance.PathReward?.Entries == null)
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
                pathRewardEntries = GameTableManager.Instance.PathReward.Entries
                    .Where(x => x.ObjectId == pathRewardObjectId);
            }
            foreach (PathRewardEntry pathRewardEntry in pathRewardEntries)
            {
                if (!PathRewardGrant.IsGrantableLevelReward(pathRewardEntry))
                    continue;

                if (pathRewardEntry.PrerequisiteId > 0 && !PrerequisiteManager.Instance.Meets(player, pathRewardEntry.PrerequisiteId))
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
            if (GameTableManager.Instance.PathReward?.Entries == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    PathRewardTableName,
                    nameof(PathManager) + "." + nameof(GrantMissionRewards),
                    MissingGameDataSeverity.PlayerImpacting,
                    $"Cannot grant mission reward for pathMissionId={pathMissionId}.");
                return;
            }

            foreach (PathRewardEntry pathRewardEntry in GameTableManager.Instance.PathReward.Entries
                .Where(x => x.ObjectId == pathMissionId))
            {
                if (!PathRewardGrant.IsGrantableMissionReward(pathRewardEntry, pathMissionId))
                    continue;

                if (pathRewardEntry.PrerequisiteId > 0 && !PrerequisiteManager.Instance.Meets(player, pathRewardEntry.PrerequisiteId))
                    continue;

                // WIP/GUESSED: LaughingWS grants PathRewardType.Mission rows on path mission
                // completion. Exact reward presentation, item-count handling, and flagged reward
                // semantics remain blocked, so this only grants currently-supported unflagged rows.
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
                Spell4Entry spell4Entry = GameTableManager.Instance.Spell4?.GetEntry(pathRewardEntry.Spell4Id);
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
                CharacterTitleEntry titleEntry = GameTableManager.Instance.CharacterTitle?.GetEntry(pathRewardEntry.CharacterTitleId);
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
                PathScientistScanBotProfileEntry scanBotProfileEntry = GameTableManager.Instance.PathScientistScanBotProfile?.GetEntry(pathRewardEntry.PathScientistScanBotProfileId);
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

            foreach (PathMissionRuntimeState state in pathMissions.Values)
                state.Save(context);
        }

        public void SendInitialPackets()
        {
            SendPathLogPacket();
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
                WorldZoneEntry parentZone = GameTableManager.Instance.WorldZone.GetEntry(currentZone.ParentZoneId);
                if (parentZone == null)
                    break;

                currentZone = parentZone;
            }

            return currentZone;
        }

        private uint ResolveCurrentMapZoneId()
        {
            WorldZoneEntry worldZoneEntry = player.Zone;
            for (int i = 0; i < 32 && worldZoneEntry != null; i++)
            {
                MapZoneEntry zoneMap = GameTableManager.Instance.MapZone?.Entries?
                    .FirstOrDefault(m => m.WorldZoneId == worldZoneEntry.Id);
                if (zoneMap != null)
                    return zoneMap.Id;

                if (worldZoneEntry.ParentZoneId == 0u)
                    break;

                worldZoneEntry = GameTableManager.Instance.WorldZone?.GetEntry(worldZoneEntry.ParentZoneId);
            }

            uint worldId = player.Map?.Entry?.Id ?? 0u;
            if (worldId == 0u)
                return 0u;

            return GameTableManager.Instance.MapZoneWorldJoin?.Entries?
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

            if (GameTableManager.Instance.Prerequisite?.GetEntry(mission.PrerequisiteId) == null)
                return false;

            // WIP/GUESSED: client labels map path mission visibility to path, faction,
            // and optional prerequisite checks. This applies the table prerequisite gate
            // during current-zone activation, while exact unlock sequencing remains blocked.
            if (TryEvaluateSimplePathPrerequisite(mission.PrerequisiteId, out bool allowed))
                return allowed;

            return PrerequisiteManager.Instance.Meets(player, mission.PrerequisiteId);
        }

        private bool TryEvaluateSimplePathPrerequisite(uint prerequisiteId, out bool allowed)
        {
            allowed = false;

            PrerequisiteEntry entry = GameTableManager.Instance.Prerequisite?.GetEntry(prerequisiteId);
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

            PathMissionEntry entry = GameTableManager.Instance.PathMission?.GetEntry(pathMissionId);
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
            player.Session.EnqueueMessageEncrypted(new ServerPathMissionActivate
            {
                Missions = pathMissions.Values
                    .Where(m => m.EpisodeId == episodeId && !m.Completed)
                    .Where(m => missionIds == null || missionIds.Contains(m.MissionId))
                    .OrderBy(m => m.MissionId)
                    .Select(BuildMission)
                    .ToList()
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
