namespace NexusForever.Game.Static.Crafting
{
    /// <summary>
    /// Client rune socket type conversions (ItemRuneSlotType_ToRuneType 140514660, Prerequisite_MapMicrochipIdToBit 14049bdc0).
    /// </summary>
    public static class ItemRuneSocketTypes
    {
        public static bool TryToRuneType(uint socketType, out RuneType runeType)
        {
            if (socketType >= (uint)RuneType.Air && socketType <= (uint)RuneType.Fusion)
            {
                runeType = (RuneType)socketType;
                return true;
            }

            // Compact slot bytes 1..7 => Air..Fusion (7..13).
            if (socketType >= 1u && socketType <= 7u)
            {
                runeType = (RuneType)(socketType + 6u);
                return true;
            }

            runeType = default;
            return false;
        }

        public static uint MapSocketTypeIdToBit(uint socketTypeId)
        {
            if (!TryToRuneType(socketTypeId, out RuneType runeType))
                return 0;

            return MapRuneTypeToBit(runeType);
        }

        public static uint MapRuneTypeToBit(RuneType runeType)
        {
            return runeType switch
            {
                RuneType.Air    => 1,
                RuneType.Water  => 2,
                RuneType.Earth  => 4,
                RuneType.Fire   => 8,
                RuneType.Logic  => 0x10,
                RuneType.Life   => 0x20,
                RuneType.Fusion => 0x40,
                _               => 0
            };
        }
    }
}
