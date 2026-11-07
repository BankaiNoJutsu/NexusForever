using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 155: native test <c>FUN_1404a0360</c> looks up the player's path mission with id <c>ObjectId</c> (a PathMission id in 155 of 174 retail slots), reads its
    /// state (mission virtual <c>+0xd0</c>, 0 when absent) and compares it with <c>Value</c> (state first, Value second). Retail <c>Value</c>s 0 to 4 are the
    /// <see cref="NexusForever.Game.Static.PlayerPath.PathMissionState"/> values, with Equal and NotEqual only.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathMission155)]
    public class PrerequisiteCheckPathMissionState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IPathManager pathManager = player.PathManager;
            if (pathManager == null)
                return false;

            return PrerequisiteCompare.Compare(comparison, (uint)pathManager.GetMissionState(objectId), value);
        }
    }
}
