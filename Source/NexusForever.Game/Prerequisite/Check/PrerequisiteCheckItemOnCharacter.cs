using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 14: handler table <c>14049d7b0</c> inventory ownership check.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ItemOnCharacter)]
    public class PrerequisiteCheckItemOnCharacter : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint count = player.Inventory.GetItemCount(objectId);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
