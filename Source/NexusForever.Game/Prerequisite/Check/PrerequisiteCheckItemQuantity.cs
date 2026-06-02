using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 51: handler table <c>14049e780</c> compares inventory item count via manager vtable.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ItemQuantity)]
    public class PrerequisiteCheckItemQuantity : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint count = player.Inventory.GetItemCount(objectId);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
