using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemStatId)]
    public class PrerequisiteCheckItemStatId : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item?.Info == null)
                return false;

            // Client Prerequisite_CheckItemStatId 1404a0d30 compares item-eval +0x148 to value0.
            uint itemStatId = parameters.Item.Info.Entry.ItemStatId;
            return PrerequisiteCompare.Compare(comparison, itemStatId, value);
        }
    }
}
