using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 219: journal/datacube-volume progress on the local player; NF uses
    /// <see cref="IDatacubeManager.GetDatacube"/> with <see cref="DatacubeType.Journal"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Volume)]
    public class PrerequisiteCheckVolume : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return DatacubePrerequisiteHelper.MeetsProgress(
                player,
                comparison,
                value,
                (ushort)objectId,
                DatacubeType.Journal);
        }
    }
}
