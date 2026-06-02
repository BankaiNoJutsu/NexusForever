using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    internal static class DatacubePrerequisiteHelper
    {
        public static bool MeetsProgress(
            IPlayer player,
            PrerequisiteComparison comparison,
            uint value,
            ushort datacubeId,
            DatacubeType type)
        {
            IDatacube datacube = player.DatacubeManager.GetDatacube(datacubeId, type);
            if (datacube == null)
            {
                return comparison switch
                {
                    PrerequisiteComparison.NotEqual => true,
                    _                             => false
                };
            }

            if (value == 0u)
            {
                return comparison switch
                {
                    PrerequisiteComparison.Equal    => true,
                    PrerequisiteComparison.NotEqual => false,
                    _                               => false
                };
            }

            bool maskMet = (datacube.Progress & value) == value;
            return comparison switch
            {
                PrerequisiteComparison.Equal              => maskMet,
                PrerequisiteComparison.NotEqual           => !maskMet,
                PrerequisiteComparison.GreaterThanOrEqual => datacube.Progress >= value,
                PrerequisiteComparison.GreaterThan        => datacube.Progress > value,
                PrerequisiteComparison.LessThanOrEqual    => datacube.Progress <= value,
                PrerequisiteComparison.LessThan           => datacube.Progress < value,
                _                                         => false
            };
        }
    }
}
