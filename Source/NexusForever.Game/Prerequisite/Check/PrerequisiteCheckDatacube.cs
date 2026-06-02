using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 218: handler table[218-221] share stub <c>1407db510</c>; client evaluates datacube
    /// progress bitmasks on the local player (see <see cref="SimpleEntity.OnActivateCast"/>). NF uses
    /// <see cref="IDatacubeManager.GetDatacube"/> with <see cref="DatacubeType.Datacube"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Datacube)]
    public class PrerequisiteCheckDatacube : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return DatacubePrerequisiteHelper.MeetsProgress(
                player,
                comparison,
                value,
                (ushort)objectId,
                DatacubeType.Datacube);
        }
    }
}
