using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.AppliedItemStatId)]
    public class PrerequisiteCheckAppliedItemStatId : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item?.Info == null)
                return false;

            // Client Prerequisite_CheckAppliedItemStatId 1404a0d60 compares item-eval +0x144 to objectId0 (ItemStat.Id).
            uint appliedItemStatId = parameters.Item.Info.StatEntry?.Id ?? 0u;
            return PrerequisiteCompare.Compare(comparison, appliedItemStatId, objectId);
        }
    }
}
