using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Retail;

namespace NexusForever.Game.Housing
{
    /// <summary>
    /// Owner/neighbor harvest yield split from property settings (build 16042).
    /// </summary>
    public static class RetailHousingHarvestSplit
    {
        public static int GetNeighborSharePercent(byte shareIndex)
        {
            int index = Math.Clamp(shareIndex, (byte)0, (byte)(RetailCertainRules.HousingNeighborHarvestSharePercents.Length - 1));
            return RetailCertainRules.HousingNeighborHarvestSharePercents[index];
        }

        public static int GetOwnerSharePercent(byte shareIndex)
        {
            return 100 - GetNeighborSharePercent(shareIndex);
        }

        /// <summary>
        /// Split a harvest yield between residence owner and harvesting neighbor.
        /// </summary>
        public static (uint ownerAmount, uint harvesterAmount) SplitYield(
            IResidence residence,
            ulong ownerCharacterId,
            ulong harvesterCharacterId,
            uint totalAmount,
            bool garden)
        {
            if (totalAmount == 0 || ownerCharacterId == harvesterCharacterId)
                return (totalAmount, 0);

            byte shareIndex = garden ? residence.GardenSharing : residence.ResourceSharing;
            int neighborPercent = GetNeighborSharePercent(shareIndex);
            uint harvesterAmount = (uint)((totalAmount * (ulong)neighborPercent) / 100ul);
            return (totalAmount - harvesterAmount, harvesterAmount);
        }
    }
}
