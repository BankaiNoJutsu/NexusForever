using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemMicrochip)]
    public class PrerequisiteCheckItemMicrochip : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            if (objectId == 0)
            {
                // Retail rows 10610/10685 keep objectId0=0 with value0=1/2; treat as microchip count gate.
                uint count = (uint)parameters.Item.MicrochipIds.Count;
                return comparison switch
                {
                    PrerequisiteComparison.Equal              => count == value,
                    PrerequisiteComparison.NotEqual           => count != value,
                    PrerequisiteComparison.GreaterThanOrEqual => count >= value,
                    PrerequisiteComparison.GreaterThan        => count > value,
                    PrerequisiteComparison.LessThanOrEqual    => count <= value,
                    PrerequisiteComparison.LessThan           => count < value,
                    _                                         => false
                };
            }

            uint mask = MapMicrochipIdToBit(objectId);
            if (mask == 0)
                return false;

            uint slotMask = BuildMicrochipSlotMask(parameters.Item.MicrochipIds);
            bool present = (slotMask & mask) != 0;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => present,
                PrerequisiteComparison.NotEqual => !present,
                _                               => false
            };
        }

        private static uint BuildMicrochipSlotMask(IList<uint> microchipIds)
        {
            uint slotMask = 0;
            foreach (uint microchipId in microchipIds)
                slotMask |= MapMicrochipIdToBit(microchipId);

            return slotMask;
        }

        private static uint MapMicrochipIdToBit(uint microchipTypeId)
        {
            return microchipTypeId switch
            {
                7  => 1,
                8  => 2,
                9  => 4,
                10 => 8,
                11 => 0x10,
                12 => 0x20,
                13 => 0x40,
                _  => 0
            };
        }
    }
}
