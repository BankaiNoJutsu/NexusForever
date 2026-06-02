using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Prerequisite
{
    internal static class PathPrerequisiteHelper
    {
        public static uint GetPathLevel(IPlayer player, PlayerPath path, IGameTableManager gameTableManager)
        {
            uint totalXp = 0u;
            foreach (IPathEntry entry in player.PathManager)
            {
                if (entry.Path == path)
                {
                    totalXp = entry.TotalXp;
                    break;
                }
            }

            PathLevelEntry pathLevel = gameTableManager.PathLevel.Entries
                .LastOrDefault(x => x.PathXP <= totalXp && x.PathTypeEnum == (uint)path);
            return pathLevel?.PathLevel ?? 0u;
        }
    }
}
