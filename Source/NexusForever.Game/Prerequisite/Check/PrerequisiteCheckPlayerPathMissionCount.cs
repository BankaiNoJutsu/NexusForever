using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 110: native case <c>0x6e</c> (<c>FUN_1404a00e0</c>) finds the player's path mission <c>ObjectId</c> and compares its progress value with <c>Value</c> (integer helper, progress first). The mission
    /// class's virtual <c>+0x30</c> selects the value: the population count of the progress word for definition types 3, 0xd, 0xe, 0x11, 0x14, 0x17, 0x19, 0x1a and 0x1b, the word itself otherwise. The progress word is
    /// the <c>ProgressCount</c> of <c>ServerPathMissionUpdate</c>. Types 5 and 6 depend on a table row that was not read and are treated as plain counts; a mission the server does not have counts as 0.
    /// Status: native body read; server mapping inferred. See Decomp/Analysis/PREREQUISITE_PATH_MISSION_COUNT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PlayerPathMissionCount)]
    public class PrerequisiteCheckPlayerPathMissionCount : IPrerequisiteCheck
    {
        private static readonly HashSet<uint> BitCountedMissionTypes = [0x03, 0x0d, 0x0e, 0x11, 0x14, 0x17, 0x19, 0x1a, 0x1b];

        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPlayerPathMissionCount(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint progress = 0u;
            if (player.PathManager != null && player.PathManager.TryGetMissionProgress(objectId, out uint word))
            {
                uint missionType = gameTableManager.PathMission?.GetEntry(objectId)?.PathMissionTypeEnum ?? uint.MaxValue;
                progress = BitCountedMissionTypes.Contains(missionType) ? (uint)BitOperations.PopCount(word) : word;
            }

            return PrerequisiteCompare.Compare(comparison, progress, value);
        }
    }
}
