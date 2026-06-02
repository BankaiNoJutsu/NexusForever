using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 13: handler table <c>14049d760</c> equipped-slot / item presence check.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ItemEquipped)]
    public class PrerequisiteCheckItemEquipped : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint equipped = 0u;
            if (objectId != 0u)
            {
                IItem item = player.Inventory.GetItem(InventoryLocation.Equipped, objectId);
                if (item != null)
                    equipped = 1u;
            }

            return PrerequisiteCompare.Compare(comparison, equipped, value);
        }
    }
}
