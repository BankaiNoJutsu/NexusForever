using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 41: handler table <c>Prerequisite_CheckZoneExplored</c> (<c>14049e490</c>) resolves
    /// zone-map exploration percent (float path through <c>PrerequisiteManager_ApplyComparison</c> at
    /// <c>1404a2010</c>). NF uses <see cref="IZoneMapManager.GetMapZoneExploredPercent"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ZoneExplored)]
    public class PrerequisiteCheckZoneExplored : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint exploredPercent = player.ZoneMapManager.GetMapZoneExploredPercent((ushort)objectId);
            return PrerequisiteCompare.Compare(comparison, exploredPercent, value);
        }
    }
}
