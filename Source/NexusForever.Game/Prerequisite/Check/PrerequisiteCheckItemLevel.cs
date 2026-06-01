using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemLevel)]
    public class PrerequisiteCheckItemLevel : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item?.Info == null)
                return false;

            // Client Prerequisite_CheckItemLevel 1404a0db0 compares item-eval +0x4 (item level field).
            uint itemLevel = parameters.Item.Info.Entry.RequiredLevel;
            return PrerequisiteCompare.Compare(comparison, itemLevel, value);
        }
    }
}
