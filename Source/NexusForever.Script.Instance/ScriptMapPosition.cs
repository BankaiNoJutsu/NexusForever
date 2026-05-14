using System.Numerics;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.GameTable.Model;

namespace NexusForever.Script.Instance
{
    internal sealed class ScriptMapPosition : IMapPosition
    {
        public IMapInfo Info { get; init; }
        public Vector3 Position { get; set; }
    }

    internal sealed class ScriptMapInfo : IMapInfo
    {
        public WorldEntry Entry { get; init; }
        public IMapLock MapLock { get; init; }
    }
}
