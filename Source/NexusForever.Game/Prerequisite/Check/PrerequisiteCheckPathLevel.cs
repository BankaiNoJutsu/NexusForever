using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 91: handler table <c>14049f4d0</c>; compares active path level to <c>value0</c>
    /// (path type in <c>objectId0</c> when set, otherwise the player's active path).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathLevel)]
    public class PrerequisiteCheckPathLevel : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPathLevel(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native (evaluator case 0x5b, vtable +0x4b0, 14049ffb0) receives the unit, the comparison and Value only and reads the level from the path global (re-read 2026-11-10,
            // Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md). ObjectId is never read; treating it as a path selector made retail prerequisite slots with a non-zero ObjectId (one slot, ObjectId 36189) unmeetable.

            foreach (IPathEntry entry in player.PathManager)
            {
                if (!player.PathManager.IsPathActive(entry.Path))
                    continue;

                uint pathLevel = PathPrerequisiteHelper.GetPathLevel(player, entry.Path, gameTableManager);
                if (PrerequisiteCompare.Compare(comparison, pathLevel, value))
                    return true;
            }

            return false;
        }
    }
}
