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
            if (objectId != 0u)
            {
                uint pathLevel = PathPrerequisiteHelper.GetPathLevel(player, (PlayerPath)objectId, gameTableManager);
                return PrerequisiteCompare.Compare(comparison, pathLevel, value);
            }

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
