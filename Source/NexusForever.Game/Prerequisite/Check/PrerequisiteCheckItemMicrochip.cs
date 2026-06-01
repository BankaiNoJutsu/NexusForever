using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemMicrochip)]
    public class PrerequisiteCheckItemMicrochip : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemMicrochip(IGameTableManager gameTableManager = null)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            if (objectId == 0)
            {
                // Retail rows 10610/10685 keep objectId0=0 with value0=1/2.
                uint count = GetCountForObjectIdZero(parameters.Item);
                return PrerequisiteCompare.Compare(comparison, count, value);
            }

            uint mask = ItemRuneSocketTypes.MapSocketTypeIdToBit(objectId);
            if (mask == 0)
                return false;

            uint slotMask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(parameters.Item, gameTableManager);
            bool present = (slotMask & mask) != 0;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => present,
                PrerequisiteComparison.NotEqual => !present,
                _                               => false
            };
        }

        private static uint GetCountForObjectIdZero(IItem item)
        {
            if (item.MicrochipIds.Count > 0)
                return (uint)item.MicrochipIds.Count;

            uint installedRunes = 0;
            foreach (ItemRuneSlot slot in item.RuneSlots)
            {
                if (slot.RuneItem2Id != 0u)
                    installedRunes++;
            }

            return installedRunes;
        }
    }
}
