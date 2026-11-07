using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 54: native case <c>0x36</c> (<c>FUN_14049fdc0</c> -> <c>FUN_1404a1950</c>) finds the player's path mission with id <c>Value</c> and returns the mission class's virtual <c>+0x28</c>, the field at
    /// <c>+0x18</c> that the class's Activate (<c>+0x18</c>) and Deactivate (<c>+0x20</c>) virtuals set and clear: the mission is the currently active one. Equal returns it, NotEqual its negation, other comparisons are
    /// false; an absent service or mission gives false. <c>ObjectId</c> is not read. The Soldier path additionally requires a holdout-table membership for mission definition type 0 (not modelled).
    /// Status: native body read; the server mapping (active = the Started state) is inferred.
    /// See Decomp/Analysis/PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PlayerPathMission)]
    public class PrerequisiteCheckPlayerPathMission : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool active = player.PathManager?.GetMissionState(value) == PathMissionState.Started;
            return PrerequisiteCompare.CompareFlag(comparison, active);
        }
    }
}
