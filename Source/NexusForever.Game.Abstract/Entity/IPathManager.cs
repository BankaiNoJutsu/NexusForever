using NexusForever.Database.Character;
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
        /// Completes a path mission if it is active or known.
        /// </summary>
        bool CompleteMission(ushort pathMissionId);

        /// <summary>
        /// Completes active path missions whose PathMission objectId matches the supplied object id.
        /// </summary>
        bool CompleteMissionByObjectId(uint objectId);

        /// <summary>
        /// Completes active soldier path missions associated with the supplied tower defense row.
        /// </summary>
        bool CompleteMissionBySoldierTowerDefenseId(uint pathSoldierTowerDefenseId);

        /// <summary>
        /// Completes active settler path missions associated with the supplied improvement group row.
        /// </summary>
        bool CompleteMissionBySettlerImprovementGroupId(uint pathSettlerImprovementGroupId);

        /// <summary>
        /// Returns if a path mission has been completed in the current session.
        /// </summary>
        bool IsMissionComplete(uint pathMissionId);

        void SendInitialPackets();
        void SendSetUnitPathTypePacket();
        void SendServerPathActivateResult(GenericError result = GenericError.Ok);
        void SendServerPathUnlockResult(GenericError result = GenericError.Ok);
    }
}
