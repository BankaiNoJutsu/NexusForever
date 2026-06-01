using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Item2Id)]
    public class PrerequisiteCheckItem2Id : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item?.Info == null)
                return false;

            return PrerequisiteCompare.Compare(comparison, parameters.Item.Info.Id, objectId);
        }
    }
}
