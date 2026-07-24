using NexusForever.Database.Character;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IPathManager : IDatabaseCharacter, IEnumerable<IPathEntry>
    {
        /// <summary>
        /// Checks to see if supplied <see cref="Static.PlayerPath.Path"/> is active.
        /// </summary>
        bool IsPathActive(Static.PlayerPath.Path pathToCheck);

        /// <summary>
        /// Attempts to activate supplied <see cref="Static.PlayerPath.Path"/>.
        /// </summary>
        void ActivatePath(Static.PlayerPath.Path pathToActivate);

        /// <summary>
        /// Checks if supplied <see cref="Static.PlayerPath.Path"/> is unlocked.
        /// </summary>
        bool IsPathUnlocked(Static.PlayerPath.Path pathToUnlock);

        /// <summary>
        /// Attemps to unlock supplied <see cref="Static.PlayerPath.Path"/>.
        /// </summary>
        void UnlockPath(Static.PlayerPath.Path pathToUnlock);

        /// <summary>
        /// Add XP to the current <see cref="Static.PlayerPath.Path"/>.
        /// </summary>
        void AddXp(uint xp);

        /// <summary>
        /// Add path levels to the current <see cref="Static.PlayerPath.Path"/>.
        /// </summary>
        void AddLevels(uint levels);

        /// <summary>
        /// Activates all supplied path missions for the current zone episode.
        /// </summary>
        void ActivateMissions(ushort episodeId, IReadOnlyDictionary<ushort, uint> missionXp);

        /// <summary>
        /// Activates table-backed path missions for the player's current world/zone episode.
        /// </summary>
        bool TryActivateCurrentZoneEpisode();

        /// <summary>
        /// Activates table-backed path missions for a specific entered world zone.
        /// </summary>
        bool TryActivateCurrentZoneEpisode(uint worldZoneId);

        /// <summary>
        /// Completes a path mission if it is active or known.
        /// </summary>
        bool CompleteMission(ushort pathMissionId);

        /// <summary>
        /// Completes a path mission only if it is already active.
        /// </summary>
        bool CompleteActiveMission(ushort pathMissionId);

        /// <summary>
        /// Completes an active explorer node progress mission when the mission and node tables match the client report.
        /// </summary>
        bool CompleteExplorerProgressMission(ushort pathMissionId, uint explorerNodeIndex);

        /// <summary>
        /// Completes an active explorer power-map mission when the mission and power-map tables match the client report.
        /// </summary>
        bool CompleteExplorerPowerMapMission(uint pathExplorerPowerMapId);

        /// <summary>
        /// Completes active explorer explore-zone missions that match the player's fully explored current map zone.
        /// </summary>
        bool CompleteCurrentExplorerExploreZoneMission();

        /// <summary>
        /// Completes active path missions whose PathMission objectId matches the supplied object id.
        /// </summary>
        bool CompleteMissionByObjectId(uint objectId);

        /// <summary>
        /// Returns whether an active path mission has a PathMission objectId matching the supplied object id.
        /// </summary>
        bool IsMissionActiveByObjectId(uint objectId);

        /// <summary>
        /// Returns whether a completed path mission has a PathMission objectId matching the supplied object id.
        /// </summary>
        bool IsMissionCompleteByObjectId(uint objectId);

        /// <summary>
        /// Activates an eligible Soldier holdout mission for the supplied PathSoldierEvent id.
        /// </summary>
        bool TryActivateSoldierMissionByEventId(uint pathSoldierEventId);

        /// <summary>
        /// Completes active soldier path missions associated with the supplied tower defense row.
        /// </summary>
        bool CompleteMissionBySoldierTowerDefenseId(uint pathSoldierTowerDefenseId);

        /// <summary>
        /// Progresses active Scientist creature-info scan missions for the supplied creature-info row.
        /// </summary>
        bool ProgressScientistCreatureScanMission(uint pathScientistCreatureInfoId);

        /// <summary>
        /// Completes active Scientist datacube-discovery missions for the player's current world zone.
        /// </summary>
        bool CompleteCurrentScientistDatacubeDiscoveryMission();

        /// <summary>
        /// Progresses active Soldier assassinate missions for a killed creature or target group.
        /// </summary>
        bool ProgressSoldierAssassinateMissionForCreatureKill(uint creature2Id, IReadOnlyCollection<uint> targetGroupIds);

        /// <summary>
        /// Increments an active Soldier SWAT mission by its PathMission id.
        /// </summary>
        bool ProgressSoldierSwatMission(ushort pathMissionId, uint amount);

        /// <summary>
        /// Progresses active Settler hub missions associated with the supplied improvement group row.
        /// </summary>
        bool CompleteMissionBySettlerImprovementGroupId(uint pathSettlerImprovementGroupId);

        /// <summary>
        /// Returns if a path mission has been completed in the current session.
        /// </summary>
        bool IsMissionComplete(uint pathMissionId);

        /// <summary>
        /// Returns whether the player has scan credit for <see cref="GameTable.Model.PathScientistCreatureInfoEntry.Id"/>.
        /// </summary>
        bool HasScannedScientistCreature(uint pathScientistCreatureInfoId);

        /// <summary>
        /// Records scientist scan credit for <see cref="GameTable.Model.PathScientistCreatureInfoEntry.Id"/>.
        /// </summary>
        void MarkScientistCreatureScanned(uint pathScientistCreatureInfoId);

        /// <summary>
        /// Attempts to deploy the supplied Scientist scanbot profile.
        /// </summary>
        bool TryDeployScientistScanbot(uint pathScientistScanBotProfileId);

        /// <summary>
        /// Dismisses the currently deployed Scientist scanbot, if present.
        /// </summary>
        bool DismissScientistScanbot();

        /// <summary>
        /// Confirms a pending Scientist scanbot summon once the map has assigned a real unit id.
        /// </summary>
        bool OnScientistScanbotSummoned(IWorldEntity entity);

        /// <summary>
        /// Clears Scientist scanbot state when the owned scanner unit leaves the map.
        /// </summary>
        bool OnScientistScanbotUnsummoned(IWorldEntity entity);

        /// <summary>
        /// Returns settler infrastructure progress for <see cref="GameTable.Model.PathSettlerInfrastructureEntry.Id"/>.
        /// </summary>
        SettlerInfrastructureState GetSettlerInfrastructureState(uint pathSettlerInfrastructureId);

        /// <summary>
        /// Records settler improvement-group build status from server build packets.
        /// </summary>
        void ApplySettlerImprovementGroupStatus(uint pathSettlerImprovementGroupId, int tier, uint bundleCount);

        /// <summary>
        /// Returns build progress percent for a <see cref="GameTable.Model.PathSettlerHubEntry.Id"/> or
        /// <see cref="GameTable.Model.PathSettlerImprovementGroupEntry.Id"/> (client record+0x60 proxy).
        /// </summary>
        uint GetSettlerHubBuildProgressPercent(uint pathSettlerHubOrImprovementGroupId);

        /// <summary>
        /// Returns contribution progress percent for a hub or improvement group (client record+0x68 proxy).
        /// </summary>
        uint GetSettlerHubContributionProgressPercent(uint pathSettlerHubOrImprovementGroupId);

        /// <summary>
        /// Returns overall settler hub progress percent when prerequisite rows use objectId0=0 (client record+0x64 proxy).
        /// </summary>
        uint GetSettlerHubOverallProgressPercent();

        /// <summary>
        /// Returns whether a path mission checklist/clue item is complete.
        /// </summary>
        /// <returns>
        /// False when the mission is unknown or inactive; otherwise sets <paramref name="isComplete"/>.
        /// </returns>
        bool TryIsPathMissionChecklistItemComplete(ushort pathMissionId, uint checklistIndex, out bool isComplete);

        void SendInitialPackets();
        void SendSetUnitPathTypePacket();
        void SendServerPathActivateResult(GenericError result = GenericError.Ok);
        void SendServerPathUnlockResult(GenericError result = GenericError.Ok);
    }
}
