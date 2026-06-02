using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 64: path type in <c>objectId0</c>, minimum path level in <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathTypeLevel)]
    public class PrerequisiteCheckPathTypeLevel : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPathTypeLevel(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            var path = (PlayerPath)objectId;
            uint pathLevel = PathPrerequisiteHelper.GetPathLevel(player, path, gameTableManager);
            return PrerequisiteCompare.Compare(comparison, pathLevel, value);
        }
    }
}
