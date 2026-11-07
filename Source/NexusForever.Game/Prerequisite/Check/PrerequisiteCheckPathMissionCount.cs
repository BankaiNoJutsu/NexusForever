using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 61: native test <c>FUN_14049fef0</c> (count helper <c>FUN_1404a1aa0</c>) counts the completed path missions of the PathEpisode with id <c>ObjectId</c>
    /// (22 of 25 retail slots), skipping missions whose <c>PathMissionFlags</c> has 0x40, and compares that count with <c>Value</c> (count first, Value second). An unknown episode counts 0.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathMissionCount)]
    public class PrerequisiteCheckPathMissionCount : IPrerequisiteCheck
    {
        private const uint ExcludedMissionFlag = 0x40u;

        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPathMissionCount(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IPathManager pathManager = player.PathManager;
            if (pathManager == null || gameTableManager.PathMission == null)
                return false;

            uint count = (uint)gameTableManager.PathMission.Entries
                .Count(mission => mission.PathEpisodeId == objectId
                    && (mission.PathMissionFlags & ExcludedMissionFlag) == 0u
                    && pathManager.IsMissionComplete(mission.Id));

            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
