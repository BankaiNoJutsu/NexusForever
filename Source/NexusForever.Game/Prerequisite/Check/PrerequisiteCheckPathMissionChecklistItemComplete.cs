using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 220: live vtable <c>+0x498</c> ->
    /// <c>Prerequisite_CheckPathMissionChecklistItemComplete_Table220</c> (<c>14049fe80</c>)
    /// resolves <see cref="PathMissionRuntime_FindById"/> with <c>objectId</c> (PathMission id)
    /// and tests checklist/clue completion via runtime vtable <c>+0x50(value)</c>.
    /// NF proxy: completed missions return all checklist items complete; otherwise uses persisted
    /// path mission <c>ProgressData</c> bit <c>value</c> when <c>value &lt; 32</c>.
    /// Per-type explorer/scientist/settler clue semantics remain blocked.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathMissionChecklistItemComplete)]
    public class PrerequisiteCheckPathMissionChecklistItemComplete : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IPathManager pathManager = player?.PathManager;
            if (pathManager == null || objectId > ushort.MaxValue)
            {
                return comparison switch
                {
                    PrerequisiteComparison.NotEqual => true,
                    _                               => false
                };
            }

            if (!pathManager.TryIsPathMissionChecklistItemComplete((ushort)objectId, value, out bool isComplete))
            {
                return comparison switch
                {
                    PrerequisiteComparison.NotEqual => true,
                    _                               => false
                };
            }

            return comparison switch
            {
                PrerequisiteComparison.Equal    => isComplete,
                PrerequisiteComparison.NotEqual => !isComplete,
                _                               => false
            };
        }
    }
}
